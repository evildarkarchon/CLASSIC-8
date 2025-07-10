namespace Classic.Core.Enums;

[Flags]
public enum MessageTarget
{
    None = 0,
    CLI = 1,
    GUI = 2,
    Both = CLI | GUI
}
