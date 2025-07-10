namespace Classic.Core.Interfaces;

/// <summary>
/// Defines the contract for report templates used in generating crash log analysis reports.
/// </summary>
public interface IReportTemplate
{
    /// <summary>
    /// Gets the name of the template.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of the template.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Formats the header section of the report.
    /// </summary>
    string FormatHeader(string fileName, string version);
    
    /// <summary>
    /// Formats an error section entry.
    /// </summary>
    string FormatError(string errorType, string errorMessage, object severity);
    
    /// <summary>
    /// Formats a suspect entry.
    /// </summary>
    string FormatSuspect(string name, string description, object severity, string? evidence = null, string? recommendation = null);
    
    /// <summary>
    /// Formats a plugin entry.
    /// </summary>
    string FormatPlugin(int loadOrder, string name, string? status = null, string? flags = null);
    
    /// <summary>
    /// Formats a FormID entry.
    /// </summary>
    string FormatFormId(uint formId, byte pluginIndex, uint localFormId, string pluginName, string? formType = null);
    
    /// <summary>
    /// Formats a section separator.
    /// </summary>
    string FormatSeparator();
    
    /// <summary>
    /// Formats a section header.
    /// </summary>
    string FormatSectionHeader(string title);
    
    /// <summary>
    /// Formats the footer section of the report.
    /// </summary>
    string FormatFooter(string version, DateTime date);
    
    /// <summary>
    /// Gets a severity icon/label for the given severity score.
    /// </summary>
    string GetSeverityIcon(int severityScore);
}