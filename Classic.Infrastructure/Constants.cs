namespace Classic.Infrastructure;

public static class Constants
{
    public static class Versions
    {
        public static readonly Version OG_VERSION = new("1.5.97.0");
        public static readonly Version NG_VERSION = new("1.6.1170.0");
        public static readonly Version VR_VERSION = new("1.4.15.0");
    }
    
    public static class Paths
    {
        public const string YAML_FOLDER = "CLASSIC Data/databases";
        public const string CRASH_LOGS_FOLDER = "Crash Logs";
    }
}
