using Microsoft.Data.SqlClient;

namespace TekniApi;

internal static class SqlEnv
{
    public static string GetConnectionString()
        => Environment.GetEnvironmentVariable("SqlConnectionString")
           ?? throw new InvalidOperationException("SqlConnectionString is missing.");

    public static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(SqlCommand cmd)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }

        return rows;
    }
}
