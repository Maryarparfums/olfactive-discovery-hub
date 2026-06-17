using System.Configuration;

namespace Maryar.Api.Infrastructure
{
    public static class AppConfig
    {
        public static int GetInt(string key, int defaultValue = 0)
        {
            var val = ConfigurationManager.AppSettings[key];
            int result;
            return int.TryParse(val, out result) ? result : defaultValue;
        }
        
        public static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static int GetInt(string key)
        {
            return int.Parse(ConfigurationManager.AppSettings[key]);
        }

        public static string GetConnectionString(string name)
        {
            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }

        public static string JwtSecret
        {
            get { return ConfigurationManager.AppSettings["JwtSecret"]; }
        }

        public static string AsaasApiKey
        {
            get { return ConfigurationManager.AppSettings["AsaasApiKey"]; }
        }
    }
}
