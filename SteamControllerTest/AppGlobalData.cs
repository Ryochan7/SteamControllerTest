using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest
{
    public class AppGlobalData
    {
        public const string APP_SETTINGS_FILENAME = "settings.json";
        public const string APP_FOLDER_NAME = "Test";

        public static string exelocation = Process.GetCurrentProcess().MainModule.FileName;
        public static string exedirpath = Directory.GetParent(exelocation).FullName;
        public static string exeFileName = Path.GetFileName(exelocation);

        public static FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(exelocation);
        public static string exeversion = fileVersion.ProductVersion;
        public static ulong exeversionLong = (ulong)fileVersion.ProductMajorPart << 48 |
            (ulong)fileVersion.ProductMinorPart << 32 | (ulong)fileVersion.ProductBuildPart << 16;
        public static ulong fullExeVersionLong = exeversionLong | (ushort)fileVersion.ProductPrivatePart;

        public string appdatapath;
        public string userAppDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_FOLDER_NAME);

        public const string BLANK_VIGEMBUS_VERSION = "0.0.0.0";
        public const string MIN_SUPPORTED_VIGEMBUS_VERSION = "1.17.113.0";

        public static bool vigemInstalled = false;
        public static string vigembusVersion = BLANK_VIGEMBUS_VERSION;
        public static Version vigemBusVersionInfo =
            new Version(!string.IsNullOrEmpty(vigembusVersion) ? vigembusVersion :
                BLANK_VIGEMBUS_VERSION);
        public static Version minSupportedViGEmBusVersionInfo = new Version(MIN_SUPPORTED_VIGEMBUS_VERSION);

        public const int CONFIG_VERSION = 0;
        public const int APP_CONFIG_VERSION = 0;
        public const string ASSEMBLY_RESOURCE_PREFIX = "pack://application:,,,/SteamControllerTest;";
        public const string RESOURCES_PREFIX = "/SteamControllertest;component/Resources";

        public AppSettingsStore appSettings;

        public AppGlobalData()
        {
            // Default to using remote AppData folder
            appdatapath = userAppDataPath;
        }

        public void LoadAppSettings()
        {
            string configPath = Path.Combine(appdatapath, APP_SETTINGS_FILENAME);
            appSettings = new AppSettingsStore(configPath);
            appSettings.LoadConfig();
        }

        public void SaveAppSettings()
        {
            if (appSettings != null)
            {
                appSettings.SaveConfig();
            }
        }
    }

    public static class AppGlobalDataSingleton
    {
        private static AppGlobalData instance;
        public static AppGlobalData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppGlobalData();
                }

                return instance;
            }
        }
    }
}
