namespace Classic.Core.Exceptions;

public class ClassicException : Exception
{
    public ClassicException(string message) : base(message) { }

    public ClassicException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class CrashLogParsingException : ClassicException
{
    public CrashLogParsingException(string message) : base(message) { }

    public CrashLogParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class ConfigurationException : ClassicException
{
    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class ScanningException : ClassicException
{
    public ScanningException(string message) : base(message) { }

    public ScanningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
