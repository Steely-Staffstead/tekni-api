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

        string? companyId = NullIfWhiteSpace(queryParams["companyId"] ?? queryParams["company_id"]);

        int? periodFrom = TryParseInt(queryParams["periodFrom"] ?? queryParams["periodfrom"]);
        int? periodTo = TryParseInt(queryParams["periodTo"] ?? queryParams["periodto"]);
        int? periodFromPy = TryParseInt(queryParams["periodFromPy"] ?? queryParams["periodfrom_py"] ?? queryParams["periodFrom_PY"]);
        int? periodToPy = TryParseInt(queryParams["periodToPy"] ?? queryParams["periodto_py"] ?? queryParams["periodTo_PY"]);

        int top = TryParseInt(queryParams["top"]) ?? 20000;
        if (top < 1) top = 1;
        if (top > 20000) top = 20000;

        int? ebitda = TryParseBit(queryParams["ebitda"] ?? queryParams["EBITDA"]);
        int? icIncluded = TryParseBit(queryParams["IC_included"] ?? queryParams["ic_included"] ?? queryParams["icIncluded"]);

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
    f.source,
    f.type,
    f.intercompany as IC_included,
    c.company_legal_name,
    c.groupname,
    c.bl_name,
    c.cg_order,
    a.category_name,
    a.class_name,
    a.class_key,
    a.account_name, 
    case when f.period between @periodFrompy and @periodTopy then 1 else 0 end  as comparison,
    a.EBITDA,
    a.Gross_profit,
    a.category_key,
    a.cat_order
FROM pbi.financials f
INNER JOIN pbi.vicompany c
    ON c.company_id = f.company_id
INNER JOIN pbi.viaccount a
    ON a.account_code = f.account_code
WHERE (@companyId IS NULL OR f.company_id = @companyId)
  AND (
        (
            @periodFrom IS NULL
            AND @periodTo IS NULL
            AND @periodFromPy IS NULL
            AND @periodToPy IS NULL
        )
        OR
        (
            (@periodFrom IS NOT NULL OR @periodTo IS NOT NULL)
            AND (@periodFrom IS NULL OR f.period >= @periodFrom)
            AND (@periodTo IS NULL OR f.period <= @periodTo)
        )
        OR
        (
            (@periodFromPy IS NOT NULL OR @periodToPy IS NOT NULL)
            AND (@periodFromPy IS NULL OR f.period >= @periodFromPy)
            AND (@periodToPy IS NULL OR f.period <= @periodToPy)
        )
      )
  AND (@ebitda IS NULL OR ISNULL(a.EBITDA, 0) = @ebitda)
  AND (@icIncluded IS NULL OR ISNULL(f.intercompany, 0) = @icIncluded)
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
            cmd.Parameters.Add(new SqlParameter("@periodFromPy", SqlDbType.Int)
            {
                Value = (object?)periodFromPy ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter("@periodToPy", SqlDbType.Int)
            {
                Value = (object?)periodToPy ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter("@ebitda", SqlDbType.Bit)
            {
                Value = (object?)ebitda ?? DBNull.Value
            });
            cmd.Parameters.Add(new SqlParameter("@icIncluded", SqlDbType.Bit)
            {
                Value = (object?)icIncluded ?? DBNull.Value
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
                top,
                filters = new
                {
                    companyId,
                    periodFrom,
                    periodTo,
                    periodFromPy,
                    periodToPy,
                    ebitda,
                    icIncluded
                },
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

    private static int? TryParseBit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        value = value.Trim();
        if (value == "1") return 1;
        if (value == "0") return 0;

        if (bool.TryParse(value, out bool result))
            return result ? 1 : 0;

        return null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
