namespace TekniApi;

public static class ApiHelpRegistry
{
    public static List<ApiHelpSpec> GetAll()
    {
        return new List<ApiHelpSpec>
        {
            new ApiHelpSpec
            {
                Name = "help",
                Endpoint = "/api/help",
                Method = "GET",
                Description = "Returns documentation for all APIs or a specific API.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec
                    {
                        Name = "apiName",
                        Type = "string",
                        Required = false,
                        Description = "Optional. If provided, returns documentation for only that API."
                    }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec
                    {
                        Description = "List all available APIs",
                        Url = "/api/help"
                    },
                    new ApiExampleSpec
                    {
                        Description = "Get documentation for the financials API",
                        Url = "/api/help?apiName=financials"
                    }
                },
                Response = new ApiResponseSpec
                {
                    Format = "application/json",
                    Shape = "success, count, data[]"
                }
            },
            new ApiHelpSpec
            {
                Name = "financials",
                Endpoint = "/api/financials",
                Method = "GET",
                Description = "Returns financial rows with optional filters.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec
                    {
                        Name = "companyId",
                        Type = "string",
                        Required = false,
                        Description = "Company identifier, for example TEKNI"
                    },
                    new ApiParameterSpec
                    {
                        Name = "periodFrom",
                        Type = "int",
                        Required = false,
                        Description = "Start period in YYYYMM format, for example 202501"
                    },
                    new ApiParameterSpec
                    {
                        Name = "periodTo",
                        Type = "int",
                        Required = false,
                        Description = "End period in YYYYMM format, for example 202512"
                    },
                    new ApiParameterSpec
                    {
                        Name = "top",
                        Type = "int",
                        Required = false,
                        DefaultValue = "1000",
                        Description = "Maximum number of rows returned"
                    }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec
                    {
                        Description = "Get 100 rows for one company and one period range",
                        Url = "/api/financials?companyId=TEKNI&periodFrom=202501&periodTo=202512&top=100"
                    },
                    new ApiExampleSpec
                    {
                        Description = "Get rows for one month only",
                        Url = "/api/financials?periodFrom=202510&periodTo=202510"
                    }
                },
                Response = new ApiResponseSpec
                {
                    Format = "application/json",
                    Shape = "success, count, data[]"
                }
            }
        };
    }
}

public class ApiHelpSpec
{
    public string Name { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Method { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ApiParameterSpec> Parameters { get; set; } = new();
    public List<ApiExampleSpec> Examples { get; set; } = new();
    public ApiResponseSpec? Response { get; set; }
}

public class ApiParameterSpec
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string Description { get; set; } = "";
}

public class ApiExampleSpec
{
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
}

public class ApiResponseSpec
{
    public string Format { get; set; } = "";
    public string Shape { get; set; } = "";
}