namespace Classic.Infrastructure;

public static class Constants
{
    public static class Versions
    {
        // Fallout 4 versions
        public static readonly Version F4OgVersion = new("1.5.97.0");
        public static readonly Version F4NgVersion = new("1.6.1170.0");
        public static readonly Version F4VrVersion = new("1.2.72.0");

        // Skyrim SE versions
        public static readonly Version SseVersion = new("1.6.1170.0");
        public static readonly Version SseAeVersion = new("1.6.1170.0");

        // Skyrim VR versions
        public static readonly Version VrVersion = new("1.4.15.0");

        // Legacy support
        public static readonly Version OgVersion = F4OgVersion;
        public static readonly Version NgVersion = F4NgVersion;
    }

    public static class Paths
    {
        public const string YamlFolder = "CLASSIC Data/databases";
        public const string CrashLogsFolder = "Crash Logs";
        public const string BackupFolder = "CLASSIC Backups";
        public const string TempFolder = "CLASSIC Temp";
        public const string LogsFolder = "logs";

        // Game-specific paths
        public static class GamePaths
        {
            public const string Fallout4Documents = @"Documents\My Games\Fallout4";
            public const string Fallout4VrDocuments = @"Documents\My Games\Fallout4VR";
            public const string SkyrimseDocuments = @"Documents\My Games\Skyrim Special Edition";
            public const string SkyrimvrDocuments = @"Documents\My Games\SkyrimVR";
        }
    }

    public static class FileExtensions
    {
        public const string CrashLog = ".txt";
        public const string LogFile = ".log";
        public const string ConfigFile = ".ini";
        public const string YamlFile = ".yaml";
        public const string BackupFile = ".bak";
    }

    public static class RegexPatterns
    {
        public const string FormIdPattern = @"\b[0-9A-Fa-f]{8}\b";
        public const string PluginPattern = @"^\s*\[\d+\]\s+(.+\.esp|.+\.esm|.+\.esl)\s*$";
        public const string VersionPattern = @"Game version: (.+)";
        public const string CrashgenPattern = @"Crash logger:.+v(\d+\.\d+\.\d+)";
    }

    public static class Configuration
    {
        public const int DefaultMaxConcurrency = 4;
        public const int DefaultCacheTimeoutMinutes = 30;
        public const int DefaultFileBufferSize = 8192;
        public const string DefaultLogLevel = "Information";
    }
}
