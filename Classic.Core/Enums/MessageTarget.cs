namespace Classic.Core.Enums;

[Flags]
public enum MessageTarget
{
    None = 0,
    Cli = 1,
    Gui = 2,
    Both = Cli | Gui
}
