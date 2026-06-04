using MySql.Data.MySqlClient;

namespace Maryar.Api.Infrastructure
{
    public interface IConnectionFactory
    {
        MySqlConnection Create();
    }

    public class MySqlConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory()
            : this(AppConfig.GetConnectionString("MaryarDb"))
        {
        }

        public MySqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MySqlConnection Create()
        {
            var cn = new MySqlConnection(_connectionString);
            cn.Open();
            return cn;
        }
    }
}
