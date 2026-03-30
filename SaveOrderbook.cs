using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;

namespace TekniApi;

public class SaveOrderbook
{
    private readonly string _connectionString = SqlEnv.GetConnectionString();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Function("SaveOrderbook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orderbook/save")] HttpRequestData req)
    {
        SaveOrderbookRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<SaveOrderbookRequest>(req.Body, JsonOptions);
        }
        catch (Exception ex)
        {
            var badJson = req.CreateResponse(HttpStatusCode.BadRequest);
            await badJson.WriteAsJsonAsync(new { success = false, error = $"Invalid JSON body: {ex.Message}" });
            return badJson;
        }

        if (payload is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { success = false, error = "Request body is required." });
            return bad;
        }

        if (string.IsNullOrWhiteSpace(payload.UserPrincipal) || string.IsNullOrWhiteSpace(payload.CompanyId) || payload.Year is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                success = false,
                error = "Fields 'userPrincipal', 'companyId' and 'year' are required."
            });
            return bad;
        }

        var projects = payload.Projects ?? new List<OrderbookProjectDto>();
        var months = payload.Months ?? new List<OrderbookProjectMonthDto>();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("api.usp_orderbook_save", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 180
            };

            cmd.Parameters.Add(new SqlParameter("@user_principal", SqlDbType.NVarChar, 256) { Value = payload.UserPrincipal!.Trim() });
            cmd.Parameters.Add(new SqlParameter("@company_id", SqlDbType.NVarChar, 50) { Value = payload.CompanyId!.Trim() });
            cmd.Parameters.Add(new SqlParameter("@year", SqlDbType.Int) { Value = payload.Year.Value });

            var projectsTable = BuildProjectsTable(projects);
            var monthsTable = BuildMonthsTable(months);

            cmd.Parameters.Add(new SqlParameter("@projects", SqlDbType.Structured)
            {
                TypeName = "api.orderbook_project_type",
                Value = projectsTable
            });

            cmd.Parameters.Add(new SqlParameter("@months", SqlDbType.Structured)
            {
                TypeName = "api.orderbook_project_month_type",
                Value = monthsTable
            });

            await cmd.ExecuteNonQueryAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Orderbook saved successfully.",
                projectCount = projects.Count,
                monthCount = months.Count
            });
            return response;
        }
        catch (SqlException ex)
        {
            HttpStatusCode status = ex.Number switch
            {
                50002 or 50004 => HttpStatusCode.BadRequest,
                50003 => HttpStatusCode.Forbidden,
                50005 or 50006 or 50007 or 50008 => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new { success = false, error = ex.Message, sqlErrorNumber = ex.Number });
            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { success = false, error = ex.Message });
            return response;
        }
    }

    private static DataTable BuildProjectsTable(IEnumerable<OrderbookProjectDto> projects)
    {
        var dt = new DataTable();
        dt.Columns.Add("line_no", typeof(int));
        dt.Columns.Add("project_name", typeof(string));
        dt.Columns.Add("customer_name", typeof(string));
        dt.Columns.Add("contract_type", typeof(string));
        dt.Columns.Add("estimated_value", typeof(decimal));
        dt.Columns.Add("remaining_value", typeof(decimal));
        dt.Columns.Add("next_year_amount", typeof(decimal));
        dt.Columns.Add("comment_text", typeof(string));

        foreach (var p in projects)
        {
            var row = dt.NewRow();
            row["line_no"] = p.LineNo;
            row["project_name"] = p.ProjectName ?? string.Empty;
            row["customer_name"] = DbValue(p.CustomerName);
            row["contract_type"] = DbValue(p.ContractType);
            row["estimated_value"] = DbValue(p.EstimatedValue);
            row["remaining_value"] = DbValue(p.RemainingValue);
            row["next_year_amount"] = DbValue(p.NextYearAmount);
            row["comment_text"] = DbValue(p.CommentText);
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static DataTable BuildMonthsTable(IEnumerable<OrderbookProjectMonthDto> months)
    {
        var dt = new DataTable();
        dt.Columns.Add("line_no", typeof(int));
        dt.Columns.Add("period_date", typeof(DateTime));
        dt.Columns.Add("amount", typeof(decimal));

        foreach (var m in months)
        {
            var row = dt.NewRow();
            row["line_no"] = m.LineNo;
            row["period_date"] = m.PeriodDate;
            row["amount"] = m.Amount;
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static object DbValue<T>(T? value)
        => value is null ? DBNull.Value : value;
}

public class SaveOrderbookRequest
{
    public string? UserPrincipal { get; set; }
    public string? CompanyId { get; set; }
    public int? Year { get; set; }
    public List<OrderbookProjectDto>? Projects { get; set; }
    public List<OrderbookProjectMonthDto>? Months { get; set; }
}

public class OrderbookProjectDto
{
    public int LineNo { get; set; }
    public string? ProjectName { get; set; }
    public string? CustomerName { get; set; }
    public string? ContractType { get; set; }
    public decimal? EstimatedValue { get; set; }
    public decimal? RemainingValue { get; set; }
    public decimal? NextYearAmount { get; set; }
    public string? CommentText { get; set; }
}

public class OrderbookProjectMonthDto
{
    public int LineNo { get; set; }
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
}
