using System.Collections.Concurrent;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Services;

/// <summary>
/// Thread-safe SQLite connection pool for efficient database access
/// </summary>
public class SqliteConnectionPool : IDisposable
{
    private readonly ILogger<SqliteConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, ConnectionPoolEntry> _connectionPools;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new();
    private bool _disposed = false;
    
    // Pool configuration
    private readonly int _maxConnectionsPerDatabase;
    private readonly TimeSpan _connectionTimeout;
    private readonly TimeSpan _cleanupInterval;
    
    public SqliteConnectionPool(
        ILogger<SqliteConnectionPool> logger,
        int maxConnectionsPerDatabase = 10,
        TimeSpan? connectionTimeout = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _maxConnectionsPerDatabase = maxConnectionsPerDatabase;
        _connectionTimeout = connectionTimeout ?? TimeSpan.FromMinutes(5);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(2);
        
        _connectionPools = new ConcurrentDictionary<string, ConnectionPoolEntry>();
        _connectionSemaphore = new SemaphoreSlim(maxConnectionsPerDatabase * 5); // Global limit
        
        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, _cleanupInterval, _cleanupInterval);
        
        _logger.LogInformation("SQLite connection pool initialized with max {MaxConnections} connections per database", 
            _maxConnectionsPerDatabase);
    }
    
    /// <summary>
    /// Gets a connection from the pool for the specified database
    /// </summary>
    public async Task<PooledConnection> GetConnectionAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqliteConnectionPool));
        
        if (!File.Exists(databasePath))
            throw new FileNotFoundException($"Database file not found: {databasePath}");
        
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var poolEntry = _connectionPools.GetOrAdd(databasePath, path => new ConnectionPoolEntry(path, _maxConnectionsPerDatabase));
            var connection = await poolEntry.GetConnectionAsync(cancellationToken);
            
            return new PooledConnection(connection, () => ReturnConnection(databasePath, connection));
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }
    
    /// <summary>
    /// Executes a query with automatic connection management
    /// </summary>
    public async Task<T?> ExecuteQueryAsync<T>(
        string databasePath, 
        string query, 
        Func<SQLiteCommand, Task<T?>> executor,
        Action<SQLiteCommand>? parameterSetter = null,
        CancellationToken cancellationToken = default)
    {
        using var pooledConnection = await GetConnectionAsync(databasePath, cancellationToken);
        using var command = new SQLiteCommand(query, pooledConnection.Connection);
        
        parameterSetter?.Invoke(command);
        
        return await executor(command);
    }
    
    /// <summary>
    /// Executes a scalar query with automatic connection management
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(
        string databasePath,
        string query,
        Action<SQLiteCommand>? parameterSetter = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(databasePath, query, async cmd =>
        {
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null && result != DBNull.Value ? (T?)result : default;
        }, parameterSetter, cancellationToken);
    }
    
    /// <summary>
    /// Returns a connection to the pool
    /// </summary>
    private void ReturnConnection(string databasePath, SQLiteConnection connection)
    {
        try
        {
            if (_connectionPools.TryGetValue(databasePath, out var poolEntry))
            {
                poolEntry.ReturnConnection(connection);
            }
            else
            {
                // Pool entry was removed, dispose the connection
                connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error returning connection to pool for database: {DatabasePath}", databasePath);
            connection.Dispose();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Cleans up expired connections
    /// </summary>
    private void CleanupExpiredConnections(object? state)
    {
        if (_disposed) return;
        
        var expiredPools = new List<string>();
        var totalCleaned = 0;
        
        foreach (var kvp in _connectionPools)
        {
            var cleanedConnections = kvp.Value.CleanupExpiredConnections(_connectionTimeout);
            totalCleaned += cleanedConnections;
            
            if (kvp.Value.IsEmpty)
            {
                expiredPools.Add(kvp.Key);
            }
        }
        
        // Remove empty pools
        foreach (var poolKey in expiredPools)
        {
            if (_connectionPools.TryRemove(poolKey, out var poolEntry))
            {
                poolEntry.Dispose();
                _logger.LogDebug("Removed empty connection pool for database: {DatabasePath}", poolKey);
            }
        }
        
        if (totalCleaned > 0)
        {
            _logger.LogDebug("Cleaned up {CleanedCount} expired connections from {PoolCount} pools", 
                totalCleaned, _connectionPools.Count);
        }
    }
    
    /// <summary>
    /// Gets connection pool statistics
    /// </summary>
    public ConnectionPoolStats GetStatistics()
    {
        var stats = new ConnectionPoolStats();
        
        foreach (var kvp in _connectionPools)
        {
            var poolEntry = kvp.Value;
            stats.TotalPools++;
            stats.TotalConnections += poolEntry.TotalConnections;
            stats.ActiveConnections += poolEntry.ActiveConnections;
            stats.IdleConnections += poolEntry.IdleConnections;
        }
        
        return stats;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _connectionSemaphore?.Dispose();
            
            foreach (var poolEntry in _connectionPools.Values)
            {
                poolEntry.Dispose();
            }
            
            _connectionPools.Clear();
            _disposed = true;
            
            _logger.LogInformation("SQLite connection pool disposed");
        }
    }
}

/// <summary>
/// Connection pool entry for a specific database
/// </summary>
internal class ConnectionPoolEntry : IDisposable
{
    private readonly string _databasePath;
    private readonly int _maxConnections;
    private readonly ConcurrentQueue<PooledConnectionEntry> _availableConnections;
    private readonly ConcurrentDictionary<SQLiteConnection, DateTime> _activeConnections;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly object _lock = new();
    private bool _disposed = false;
    
    public ConnectionPoolEntry(string databasePath, int maxConnections)
    {
        _databasePath = databasePath;
        _maxConnections = maxConnections;
        _availableConnections = new ConcurrentQueue<PooledConnectionEntry>();
        _activeConnections = new ConcurrentDictionary<SQLiteConnection, DateTime>();
        _connectionSemaphore = new SemaphoreSlim(maxConnections);
    }
    
    public int TotalConnections => _availableConnections.Count + _activeConnections.Count;
    public int ActiveConnections => _activeConnections.Count;
    public int IdleConnections => _availableConnections.Count;
    public bool IsEmpty => TotalConnections == 0;
    
    public async Task<SQLiteConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Try to get an existing connection
            while (_availableConnections.TryDequeue(out var pooledEntry))
            {
                if (IsConnectionValid(pooledEntry.Connection))
                {
                    _activeConnections.TryAdd(pooledEntry.Connection, DateTime.UtcNow);
                    return pooledEntry.Connection;
                }
                else
                {
                    pooledEntry.Connection.Dispose();
                }
            }
            
            // Create new connection
            var connection = await CreateNewConnectionAsync(cancellationToken);
            _activeConnections.TryAdd(connection, DateTime.UtcNow);
            return connection;
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }
    
    public void ReturnConnection(SQLiteConnection connection)
    {
        if (_disposed) return;
        
        if (_activeConnections.TryRemove(connection, out _))
        {
            if (IsConnectionValid(connection))
            {
                _availableConnections.Enqueue(new PooledConnectionEntry(connection, DateTime.UtcNow));
            }
            else
            {
                connection.Dispose();
            }
            
            _connectionSemaphore.Release();
        }
    }
    
    public int CleanupExpiredConnections(TimeSpan timeout)
    {
        var cleanedCount = 0;
        var cutoffTime = DateTime.UtcNow - timeout;
        
        // Clean up idle connections
        var remainingConnections = new List<PooledConnectionEntry>();
        
        while (_availableConnections.TryDequeue(out var pooledEntry))
        {
            if (pooledEntry.LastUsed < cutoffTime || !IsConnectionValid(pooledEntry.Connection))
            {
                pooledEntry.Connection.Dispose();
                cleanedCount++;
            }
            else
            {
                remainingConnections.Add(pooledEntry);
            }
        }
        
        // Re-queue remaining connections
        foreach (var entry in remainingConnections)
        {
            _availableConnections.Enqueue(entry);
        }
        
        return cleanedCount;
    }
    
    private async Task<SQLiteConnection> CreateNewConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = $"Data Source={_databasePath};Version=3;Cache Size=10000;Page Size=4096;Temp Store=Memory;";
        var connection = new SQLiteConnection(connectionString);
        
        await connection.OpenAsync(cancellationToken);
        
        // Optimize connection settings
        using var pragmaCommand = new SQLiteCommand(connection);
        pragmaCommand.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA cache_size = 10000;
            PRAGMA temp_store = MEMORY;
            PRAGMA mmap_size = 67108864;
        ";
        await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
        
        return connection;
    }
    
    private static bool IsConnectionValid(SQLiteConnection connection)
    {
        try
        {
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _connectionSemaphore?.Dispose();
            
            while (_availableConnections.TryDequeue(out var entry))
            {
                entry.Connection.Dispose();
            }
            
            foreach (var connection in _activeConnections.Keys)
            {
                connection.Dispose();
            }
            
            _activeConnections.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Wrapper for pooled connections with automatic return to pool
/// </summary>
public class PooledConnection : IDisposable
{
    private readonly Action _returnAction;
    private bool _disposed = false;
    
    public SQLiteConnection Connection { get; }
    
    internal PooledConnection(SQLiteConnection connection, Action returnAction)
    {
        Connection = connection;
        _returnAction = returnAction;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _returnAction();
            _disposed = true;
        }
    }
}

/// <summary>
/// Internal connection entry with timing information
/// </summary>
internal record PooledConnectionEntry(SQLiteConnection Connection, DateTime LastUsed);

/// <summary>
/// Connection pool statistics
/// </summary>
public class ConnectionPoolStats
{
    public int TotalPools { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public double PoolUtilization => TotalConnections > 0 ? (double)ActiveConnections / TotalConnections * 100 : 0;
}