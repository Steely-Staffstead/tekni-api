using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace TekniApi;

public class ApiHelp
{
    [Function("ApiHelp")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "help")] HttpRequestData req)
    {
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string? apiName = queryParams["apiName"]?.Trim();

        var apis = ApiHelpRegistry.GetAll();

        object result;

        if (string.IsNullOrWhiteSpace(apiName))
        {
            result = new
            {
                success = true,
                count = apis.Count,
                data = apis
            };
        }
        else
        {
            var match = apis.FirstOrDefault(a =>
                string.Equals(a.Name, apiName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                notFound.Headers.Add("Content-Type", "application/json; charset=utf-8");

                await notFound.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"API '{apiName}' was not found."
                }, new JsonSerializerOptions { WriteIndented = true }));

                return notFound;
            }

            result = new
            {
                success = true,
                data = match
            };
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        await response.WriteStringAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        return response;
    }
}