using System;

namespace Classic.Core.Models;

/// <summary>
/// Represents statistics from Papyrus log monitoring.
/// Contains counts of dumps, stacks, warnings, errors, and derived metrics.
/// </summary>
public class PapyrusStats : IEquatable<PapyrusStats>
{
    /// <summary>
    /// Timestamp when these statistics were recorded.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Number of dumps found in the Papyrus log.
    /// </summary>
    public int Dumps { get; init; }

    /// <summary>
    /// Number of stacks found in the Papyrus log.
    /// </summary>
    public int Stacks { get; init; }

    /// <summary>
    /// Number of warnings found in the Papyrus log.
    /// </summary>
    public int Warnings { get; init; }

    /// <summary>
    /// Number of errors found in the Papyrus log.
    /// </summary>
    public int Errors { get; init; }

    /// <summary>
    /// Ratio of dumps to stacks. Higher values indicate potential issues.
    /// </summary>
    public double Ratio { get; init; }

    /// <summary>
    /// Indicates whether the Papyrus log file exists and is readable.
    /// </summary>
    public bool LogFileExists { get; init; }

    /// <summary>
    /// Error message if log file could not be read.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public bool Equals(PapyrusStats? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Dumps == other.Dumps &&
               Stacks == other.Stacks &&
               Warnings == other.Warnings &&
               Errors == other.Errors &&
               Math.Abs(Ratio - other.Ratio) < 0.001 &&
               LogFileExists == other.LogFileExists;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PapyrusStats);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Dumps, Stacks, Warnings, Errors, Ratio.GetHashCode(), LogFileExists);
    }

    public static bool operator ==(PapyrusStats? left, PapyrusStats? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PapyrusStats? left, PapyrusStats? right)
    {
        return !Equals(left, right);
    }
}
