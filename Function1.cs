using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;

namespace TekniApi;

public class GetFinancials
{
    private readonly string _connectionString;

    public GetFinancials()
    {
        _connectionString =
            Environment.GetEnvironmentVariable("SqlConnectionString")
            ?? throw new InvalidOperationException("SqlConnectionString is missing.");
    }

    [Function("GetFinancials")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "financials")] HttpRequestData req)
    {
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        string? companyId = queryParams["companyId"];
        int? periodFrom = TryParseInt(queryParams["periodFrom"]);
        int? periodTo = TryParseInt(queryParams["periodTo"]);
        int top = TryParseInt(queryParams["top"]) ?? 1000;

        if (top < 1) top = 1;
        if (top > 20000) top = 20000;

        const string sql = @"
SELECT TOP (@top)
    f.company_id,
    f.YEAR,
    f.MONTH,
    f.period,
    f.account_code,
    f.amount_period_calc,
    f.amount_ytd_source,
    f.local_account_code,
    c.company_legal_name,
    c.groupname,
    c.bl_name,
    a.category_name,
    a.class_name,
    a.class_key,
    a.account_name,
    a.EBITDA,
    a.Gross_profit,
    a.category_key
FROM pbi.financials f
INNER JOIN pbi.vicompany c
    ON c.company_id = f.company_id
INNER JOIN pbi.viaccount a
    ON a.account_code = f.account_code
WHERE (@companyId IS NULL OR f.company_id = @companyId)
  AND (@periodFrom IS NULL OR f.period >= @periodFrom)
  AND (@periodTo IS NULL OR f.period <= @periodTo)
ORDER BY f.period, f.company_id, f.account_code;";

        try
        {
            var rows = new List<Dictionary<string, object?>>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@top", SqlDbType.Int) { Value = top });
            cmd.Parameters.Add(new SqlParameter("@companyId", SqlDbType.NVarChar, 50)
            {
                Value = (object?)companyId ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter("@periodFrom", SqlDbType.Int)
            {
                Value = (object?)periodFrom ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter("@periodTo", SqlDbType.Int)
            {
                Value = (object?)periodTo ?? DBNull.Value
            });

            cmd.CommandTimeout = 120;

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

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                success = true,
                count = rows.Count,
                data = rows
            }));

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            }));

            return response;
        }
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out int result) ? result : null;
}