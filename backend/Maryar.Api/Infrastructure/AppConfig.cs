using System.Configuration;

namespace Maryar.Api.Infrastructure
{
    public static class AppConfig
    {
        public static string JwtSecret => ConfigurationManager.AppSettings["JwtSecret"];
        public static string AsaasApiKey => ConfigurationManager.AppSettings["AsaasApiKey"];
        }
                
        public static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetConnectionString(string name = "MaryarDb")
        {
            var cs = ConfigurationManager.ConnectionStrings[name];
            return cs == null ? null : cs.ConnectionString;
        }

        public static int GetInt(string key, int defaultValue)
        {
            int v;
            return int.TryParse(Get(key), out v) ? v : defaultValue;
        }
    }
}
