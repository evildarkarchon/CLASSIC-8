using Classic.Core.Enums;

namespace Classic.Core.Interfaces;

public interface IMessageHandler
{
    void SendMessage(string message, MessageType type, MessageTarget target);
    void ReportProgress(string operation, int current, int total);
    IDisposable BeginProgressContext(string operation, int total);
}
