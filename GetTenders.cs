using System.Data;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;

namespace TekniApi;

public class GetTenders
{
    private readonly string _connectionString = SqlEnv.GetConnectionString();

    [Function("GetTenders")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tenders")] HttpRequestData req)
    {
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string? userPrincipal = queryParams["userPrincipal"]?.Trim();
        string? companyId = queryParams["companyId"]?.Trim();
        int? year = int.TryParse(queryParams["year"], out var y) ? y : null;

        if (string.IsNullOrWhiteSpace(userPrincipal) || string.IsNullOrWhiteSpace(companyId) || year is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                success = false,
                error = "Query parameters 'userPrincipal', 'companyId' and 'year' are required."
            });
            return bad;
        }

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("api.usp_tender_get", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };
            cmd.Parameters.Add(new SqlParameter("@user_principal", SqlDbType.NVarChar, 256) { Value = userPrincipal });
            cmd.Parameters.Add(new SqlParameter("@company_id", SqlDbType.NVarChar, 50) { Value = companyId });
            cmd.Parameters.Add(new SqlParameter("@year", SqlDbType.Int) { Value = year.Value });

            var rows = await SqlEnv.ReadRowsAsync(cmd);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, count = rows.Count, data = rows });
            return response;
        }
        catch (SqlException ex)
        {
            var status = ex.Number == 50001 ? HttpStatusCode.Forbidden : HttpStatusCode.InternalServerError;
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
}
