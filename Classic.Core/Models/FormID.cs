namespace Classic.Core.Models;

public class FormId
{
    public string Value { get; set; } = string.Empty;
    public uint FormIdValue { get; set; }
    public uint LocalFormId { get; set; }
    public byte PluginIndex { get; set; }
    public string PluginName { get; set; } = string.Empty;
    public string SourcePlugin { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public int FormTypeId { get; set; }
    public uint Flags { get; set; }
    public bool IsMasterRecord { get; set; }
    public string SourceSegment { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
