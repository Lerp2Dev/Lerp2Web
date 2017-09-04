namespace Lerp2Web.Properties
{
    public static class ManagedSettings
    {
        public static string CurrentLanguage
        {
            get
            {
                return Settings.Default.CurrentLanguage;
            }
            set
            {
                Settings.Default.CurrentLanguage = value;
            }
        }

        public static string InitializatedConfig
        {
            get
            {
                return Settings.Default.InitializatedConfig;
            }
            set
            {
                Settings.Default.InitializatedConfig = value;
            }
        }

        public static string LoginUsername
        {
            get
            {
                return Settings.Default.LoginUsername;
            }
            set
            {
                Settings.Default.LoginUsername = value;
            }
        }

        public static string LoginPassword
        {
            get
            {
                return Settings.Default.LoginPassword;
            }
            set
            {
                Settings.Default.LoginPassword = value;
            }
        }

        public static string EndSessionConfig
        {
            get
            {
                return Settings.Default.EndSessionConfig;
            }
            set
            {
                Settings.Default.EndSessionConfig = value;
            }
        }

        public static bool Save()
        {
            try
            {
                Settings.Default.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}