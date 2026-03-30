using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;

namespace TekniApi;

public class SaveTenders
{
    private readonly string _connectionString = SqlEnv.GetConnectionString();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Function("SaveTenders")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tenders/save")] HttpRequestData req)
    {
        SaveTendersRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<SaveTendersRequest>(req.Body, JsonOptions);
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

        var lines = payload.Lines ?? new List<TenderLineDto>();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("api.usp_tender_save", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 180
            };

            cmd.Parameters.Add(new SqlParameter("@user_principal", SqlDbType.NVarChar, 256) { Value = payload.UserPrincipal!.Trim() });
            cmd.Parameters.Add(new SqlParameter("@company_id", SqlDbType.NVarChar, 50) { Value = payload.CompanyId!.Trim() });
            cmd.Parameters.Add(new SqlParameter("@year", SqlDbType.Int) { Value = payload.Year.Value });

            var linesTable = BuildLinesTable(lines);

            cmd.Parameters.Add(new SqlParameter("@lines", SqlDbType.Structured)
            {
                TypeName = "api.tender_line_type",
                Value = linesTable
            });

            await cmd.ExecuteNonQueryAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Tenders saved successfully.",
                lineCount = lines.Count
            });
            return response;
        }
        catch (SqlException ex)
        {
            HttpStatusCode status = ex.Number switch
            {
                50002 or 50004 => HttpStatusCode.BadRequest,
                50003 => HttpStatusCode.Forbidden,
                50005 => HttpStatusCode.BadRequest,
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

    private static DataTable BuildLinesTable(IEnumerable<TenderLineDto> lines)
    {
        var dt = new DataTable();
        dt.Columns.Add("line_no", typeof(int));
        dt.Columns.Add("project_name", typeof(string));
        dt.Columns.Add("project_type", typeof(string));
        dt.Columns.Add("customer_name", typeof(string));
        dt.Columns.Add("contractor_name", typeof(string));
        dt.Columns.Add("priority_text", typeof(string));
        dt.Columns.Add("contract_type", typeof(string));
        dt.Columns.Add("status_text", typeof(string));
        dt.Columns.Add("comment_text", typeof(string));
        dt.Columns.Add("expected_decision_date", typeof(DateTime));
        dt.Columns.Add("expected_start_date", typeof(DateTime));
        dt.Columns.Add("estimated_margin_pct", typeof(decimal));
        dt.Columns.Add("estimated_revenue", typeof(decimal));
        dt.Columns.Add("estimated_contribution", typeof(decimal));

        foreach (var l in lines)
        {
            var row = dt.NewRow();
            row["line_no"] = l.LineNo;
            row["project_name"] = l.ProjectName ?? string.Empty;
            row["project_type"] = DbValue(l.ProjectType);
            row["customer_name"] = DbValue(l.CustomerName);
            row["contractor_name"] = DbValue(l.ContractorName);
            row["priority_text"] = DbValue(l.PriorityText);
            row["contract_type"] = DbValue(l.ContractType);
            row["status_text"] = DbValue(l.StatusText);
            row["comment_text"] = DbValue(l.CommentText);
            row["expected_decision_date"] = DbValue(l.ExpectedDecisionDate);
            row["expected_start_date"] = DbValue(l.ExpectedStartDate);
            row["estimated_margin_pct"] = DbValue(l.EstimatedMarginPct);
            row["estimated_revenue"] = DbValue(l.EstimatedRevenue);
            row["estimated_contribution"] = DbValue(l.EstimatedContribution);
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static object DbValue<T>(T? value)
        => value is null ? DBNull.Value : value;
}

public class SaveTendersRequest
{
    public string? UserPrincipal { get; set; }
    public string? CompanyId { get; set; }
    public int? Year { get; set; }
    public List<TenderLineDto>? Lines { get; set; }
}

public class TenderLineDto
{
    public int LineNo { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectType { get; set; }
    public string? CustomerName { get; set; }
    public string? ContractorName { get; set; }
    public string? PriorityText { get; set; }
    public string? ContractType { get; set; }
    public string? StatusText { get; set; }
    public string? CommentText { get; set; }
    public DateTime? ExpectedDecisionDate { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
    public decimal? EstimatedMarginPct { get; set; }
    public decimal? EstimatedRevenue { get; set; }
    public decimal? EstimatedContribution { get; set; }
}
