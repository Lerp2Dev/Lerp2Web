using Lerp2Web.Properties;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Lerp2Web
{
    #region "API: Languages & Config"

    public class API
    {
        internal static Action _loadConfigCallback;

        public static Configuration config;

        public static bool RememberingAuth
        {
            get
            {
                return config != null && !string.IsNullOrEmpty(Settings.Default.LoginUsername) || !string.IsNullOrEmpty(Settings.Default.LoginPassword);
            }
        }

        public static bool InitializatedConfigSession
        {
            get
            {
                return Settings.Default.InitializatedConfig == "false";
            }
        }

        private static void InitConfig(Configuration config)
        {
            /*ConfigCore.CreateSettingEntry(ConfigKeys._InitConfig, "true");
            ConfigCore.CreateSettingEntry(ConfigKeys.usernameConfig, "");
            ConfigCore.CreateSettingEntry(ConfigKeys.passwordConfig, "");
            ConfigCore.CreateSettingEntry(ConfigKeys.sessionTimeConfig, "");
            ConfigCore.CreateSettingEntry(ConfigKeys.currentLanguage, "en");*/
        }

        public static void LoadConfig(Action firstExecution)
        {
            //Load config
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "App.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

            if (InitializatedConfigSession)
            {
                InitConfig(config);
                firstExecution?.Invoke();
            }

            //Load things that needs config
            //Load for example offline sessions
            string val = !string.IsNullOrEmpty(Settings.Default.EndSessionConfig) ? Settings.Default.EndSessionConfig : "";
            if (!string.IsNullOrEmpty(val))
                OfflineSession.Load(val);

            Console.WriteLine(_loadConfigCallback == null);
            _loadConfigCallback?.Invoke();
        }

        public static void LoadConfigCallback(Action call)
        {
            Console.WriteLine("Setting callback!!");
            _loadConfigCallback = call;
        }
    }

    public class ConfigCore : API
    {
        public static SettingsManager Settings
        {
            get
            {
                //Console.WriteLine("AppSettings is null?: {0}", config.AppSettings == null);
                return config.AppSettings.Settings.ToManagedSettings();
            }
        }

        internal static KeyValueConfigurationCollection Setts
        {
            get
            {
                return config.AppSettings.Settings;
            }
        }

        public static void CreateSettingEntry(string key, string value)
        {
            try
            {
                Setts.Add(key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error ocurred trying to add value to Config. Message: {0}", ex.ToString());
            }
        }

        public static void Exiting(Action finalQuest)
        {
            Properties.Settings.Default.InitializatedConfig = "true";
            config.Save();
            finalQuest();
        }
    }

    public class ConfigKeys
    {
        /*public static string _InitConfig
        {
            get
            {
                return "InitializatedConfig";
            }
        }

        public static string usernameConfig
        {
            get
            {
                return "loginUsername";
            }
        }

        public static string passwordConfig
        {
            get
            {
                return "loginPassword";
            }
        }

        public static string sessionTimeConfig
        {
            get
            {
                return "endSessionConfig";
            }
        }

        public static string currentLanguage
        {
            get
            {
                return "currentLanguage";
            }
        }*/
    }

    public class SettingsManager : KeyValueConfigurationCollection
    {
        //private KeyValueConfigurationCollection _c;

        /*internal KeyValueConfigurationCollection col
        {
            get
            {
                return ConfigCore.Setts;
            }
        }*/

        public KeyValueConfigurationElement this[string key]
        {
            get
            {
                if (ConfigCore.Setts[key] == null)
                    ConfigCore.CreateSettingEntry(key, "");
                return ConfigCore.Setts[key];
            }
        }
    }

    public static class SettingsManagerExtensions
    {
        public static SettingsManager ToManagedSettings(this KeyValueConfigurationCollection c)
        {
            return new SettingsManager();
        }
    }

    #endregion "API: Languages & Config"
}