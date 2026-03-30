using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;

namespace TekniApi;

public class GetCompanies
{
    private readonly string _connectionString = SqlEnv.GetConnectionString();

    [Function("GetCompanies")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "companies")] HttpRequestData req)
    {
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string? userPrincipal = queryParams["userPrincipal"]?.Trim();

        if (string.IsNullOrWhiteSpace(userPrincipal))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                success = false,
                error = "Query parameter 'userPrincipal' is required."
            });
            return bad;
        }

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("api.usp_company_get_for_user", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };
            cmd.Parameters.Add(new SqlParameter("@user_principal", SqlDbType.NVarChar, 256) { Value = userPrincipal });

            var rows = await SqlEnv.ReadRowsAsync(cmd);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, count = rows.Count, data = rows });
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
