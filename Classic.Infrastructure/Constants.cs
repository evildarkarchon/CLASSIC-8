namespace Classic.Infrastructure;

public static class Constants
{
    public static class Versions
    {
        // Fallout 4 versions
        public static readonly Version F4_OG_VERSION = new("1.5.97.0");
        public static readonly Version F4_NG_VERSION = new("1.6.1170.0");
        public static readonly Version F4_VR_VERSION = new("1.2.72.0");

        // Skyrim SE versions
        public static readonly Version SSE_VERSION = new("1.6.1170.0");
        public static readonly Version SSE_AE_VERSION = new("1.6.1170.0");

        // Skyrim VR versions
        public static readonly Version VR_VERSION = new("1.4.15.0");

        // Legacy support
        public static readonly Version OG_VERSION = F4_OG_VERSION;
        public static readonly Version NG_VERSION = F4_NG_VERSION;
    }

    public static class Paths
    {
        public const string YAML_FOLDER = "CLASSIC Data/databases";
        public const string CRASH_LOGS_FOLDER = "Crash Logs";
        public const string BACKUP_FOLDER = "CLASSIC Backups";
        public const string TEMP_FOLDER = "CLASSIC Temp";
        public const string LOGS_FOLDER = "logs";

        // Game-specific paths
        public static class GamePaths
        {
            public const string FALLOUT4_DOCUMENTS = @"Documents\My Games\Fallout4";
            public const string FALLOUT4VR_DOCUMENTS = @"Documents\My Games\Fallout4VR";
            public const string SKYRIMSE_DOCUMENTS = @"Documents\My Games\Skyrim Special Edition";
            public const string SKYRIMVR_DOCUMENTS = @"Documents\My Games\SkyrimVR";
        }
    }

    public static class FileExtensions
    {
        public const string CRASH_LOG = ".txt";
        public const string LOG_FILE = ".log";
        public const string CONFIG_FILE = ".ini";
        public const string YAML_FILE = ".yaml";
        public const string BACKUP_FILE = ".bak";
    }

    public static class RegexPatterns
    {
        public const string FORM_ID_PATTERN = @"\b[0-9A-Fa-f]{8}\b";
        public const string PLUGIN_PATTERN = @"^\s*\[\d+\]\s+(.+\.esp|.+\.esm|.+\.esl)\s*$";
        public const string VERSION_PATTERN = @"Game version: (.+)";
        public const string CRASHGEN_PATTERN = @"Crash logger:.+v(\d+\.\d+\.\d+)";
    }

    public static class Configuration
    {
        public const int DEFAULT_MAX_CONCURRENCY = 4;
        public const int DEFAULT_CACHE_TIMEOUT_MINUTES = 30;
        public const int DEFAULT_FILE_BUFFER_SIZE = 8192;
        public const string DEFAULT_LOG_LEVEL = "Information";
    }
}
