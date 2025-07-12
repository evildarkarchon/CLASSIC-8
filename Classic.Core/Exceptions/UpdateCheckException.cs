namespace Classic.Core.Exceptions;

/// <summary>
/// Exception thrown when update checking operations fail
/// </summary>
public class UpdateCheckException : ClassicException
{
    public UpdateCheckException(string message) : base(message)
    {
    }

    public UpdateCheckException(string message, Exception innerException) : base(message, innerException)
    {
    }
}