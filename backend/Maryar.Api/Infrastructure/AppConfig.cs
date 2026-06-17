using System.Configuration;

namespace Maryar.Api.Infrastructure
{
    public static class AppConfig
    {
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
