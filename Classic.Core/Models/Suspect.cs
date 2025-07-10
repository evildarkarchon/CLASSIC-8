namespace Classic.Core.Models;

public class Suspect
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public SuspectType Type { get; set; }
    public SeverityLevel Severity { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public List<string> RelatedFiles { get; set; } = new();
    public List<FormID> RelatedFormIDs { get; set; } = new();
}

public enum SuspectType
{
    Plugin,
    FormID,
    Setting,
    ModConflict,
    CorruptFile,
    MissingFile,
    VersionMismatch,
    Unknown
}

public enum SeverityLevel
{
    Low,
    Medium,
    High,
    Critical
}
