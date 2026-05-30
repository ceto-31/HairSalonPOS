using System.Configuration;
using Microsoft.Data.SqlClient;

namespace HairSalonPOS.Data;

public static class SqlConnectionFactory
{
    public static string ConnectionString =>
        ConfigurationManager.ConnectionStrings["HairSalonDb"]?.ConnectionString
        ?? throw new InvalidOperationException("Connection string 'HairSalonDb' not found in App.config.");

    public static SqlConnection CreateConnection() => new(ConnectionString);
}
