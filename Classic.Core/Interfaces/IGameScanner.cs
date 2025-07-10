namespace Classic.Core.Interfaces;

/// <summary>
/// Interface for game file scanning services.
/// </summary>
public interface IGameScanner
{
    /// <summary>
    /// Scans game files and configurations for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    Task<string> ScanAsync();
}

/// <summary>
/// Interface for XSE plugin checking.
/// </summary>
public interface IXsePluginChecker : IGameScanner
{
    /// <summary>
    /// Checks XSE plugins for compatibility issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous check operation. The task result contains a formatted message with check results.</returns>
    Task<string> CheckXsePluginsAsync();
}

/// <summary>
/// Interface for crash generator (Buffout4) settings checking.
/// </summary>
public interface ICrashgenChecker : IGameScanner
{
    /// <summary>
    /// Checks crash generator settings for compatibility issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous check operation. The task result contains a formatted message with check results.</returns>
    Task<string> CheckCrashgenSettingsAsync();
}

/// <summary>
/// Interface for Wrye Bash plugin checker report scanning.
/// </summary>
public interface IWryeBashChecker : IGameScanner
{
    /// <summary>
    /// Scans Wrye Bash plugin checker reports for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    Task<string> ScanWryeCheckAsync();
}

/// <summary>
/// Interface for mod INI file scanning.
/// </summary>
public interface IModIniScanner : IGameScanner
{
    /// <summary>
    /// Scans mod INI files for configuration issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    Task<string> ScanModInisAsync();
}