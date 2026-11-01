using Microsoft.Data.SqlClient;

namespace Marketplace.Api;

internal static class DatabaseSettings
{
    public static string Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Both database connection strings are required.");
        var connection = new SqlConnectionStringBuilder(value);
        if (string.IsNullOrWhiteSpace(connection.DataSource) || string.IsNullOrWhiteSpace(connection.InitialCatalog))
            throw new InvalidOperationException("A SQL Server instance and a database name are required.");
        return connection.ToString();
    }
}
