using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Lerp2Web
{
    #region "API: Languages & Config"

    public class API
    {
        internal static Action _loadConfigCallback;

        public static LanguageManager lang;
        public static Configuration config;

        public const string _InitConfig = "InitializatedConfig",
                            usernameConfig = "loginUsername",
                            passwordConfig = "loginPassword",
                            sessionTimeConfig = "endSessionConfig";

        public static bool RememberingAuth
        {
            get
            {
                return config != null && !config.AppSettings.Settings.IsEmpty(usernameConfig) || !config.AppSettings.Settings.IsEmpty(passwordConfig);
            }
        }

        private static void InitConfig(Configuration config)
        {
            config.AppSettings.Settings.Add(_InitConfig, "true");
            config.AppSettings.Settings.Add(usernameConfig, "");
            config.AppSettings.Settings.Add(passwordConfig, "");
            config.AppSettings.Settings.Add(sessionTimeConfig, "");
        }

        public static void LoadConfig()
        {
            //Load config
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "App.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

            if (config.AppSettings.Settings.IsEmpty(_InitConfig))
                InitConfig(config);

            //Load things that needs config
            //Load for example offline sessions
            string val = !config.AppSettings.Settings.IsEmpty(sessionTimeConfig) ? config.AppSettings.Settings[sessionTimeConfig].Value : "";
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

    #endregion "API: Languages & Config"

    #region "Languages"

    //Esto va para la API (LiteLerped-WF-API)

    public enum LerpedLanguage { ES, EN }

    public class LanguageManager
    {
        public CultureInfo culture;

        private ResourceManager _rMan;
        private string baseName;

        public Action<LerpedLanguage> Switch;

        public ResourceManager ResMan
        {
            get
            {
                if (_rMan == null)
                    _rMan = new ResourceManager(baseName, Assembly.GetExecutingAssembly());
                return _rMan;
            }
        }

        public LanguageManager(string baseName, Action<LerpedLanguage> act)
        {
            this.baseName = baseName;

            Switch = (lang) =>
            {
                culture = CultureInfo.CreateSpecificCulture(lang.ToString().ToLower());
                act(lang);
            };
        }

        public string GetString(string str)
        {
            return ResMan.GetString(str, culture);
        }
    }

    #endregion "Languages"
}