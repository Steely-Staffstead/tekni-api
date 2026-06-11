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
                Description = "Returns documentation for all APIs or a specific API. Upd 2026-04-07",
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
                        Description = "Get documentation for the orderbook save API",
                        Url = "/api/help?apiName=orderbook-save"
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
                Description = "Returns financial rows with optional current-year and prior-year period filters. Default top is 20000 rows when not supplied.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = false, Description = "Company identifier, for example TEGR" },
                    new ApiParameterSpec { Name = "periodFrom", Type = "int", Required = false, Description = "Current period start in YYYYMM format, for example 202605" },
                    new ApiParameterSpec { Name = "periodTo", Type = "int", Required = false, Description = "Current period end in YYYYMM format, for example 202605" },
                    new ApiParameterSpec { Name = "periodFromPy", Type = "int", Required = false, Description = "Prior-year comparison period start in YYYYMM format, for example 202505. Alias: periodfrom_py" },
                    new ApiParameterSpec { Name = "periodToPy", Type = "int", Required = false, Description = "Prior-year comparison period end in YYYYMM format, for example 202505. Alias: periodto_py" },
                    new ApiParameterSpec { Name = "ebitda", Type = "bit", Required = false, Description = "Optional filter. Use 1 for EBITDA rows, 0 for non-EBITDA rows. Omit for all." },
                    new ApiParameterSpec { Name = "IC_included", Type = "bit", Required = false, Description = "Optional filter on f.IC_included. Use 1 or 0. Omit for all." },
                    new ApiParameterSpec { Name = "top", Type = "int", Required = false, DefaultValue = "20000", Description = "Maximum number of rows returned. Capped at 20000." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec { Description = "Get one current period and matching prior-year period", Url = "/api/financials?companyId=TEKNI&periodFrom=202605&periodTo=202605&periodFromPy=202505&periodToPy=202505" },
                    new ApiExampleSpec { Description = "Same call with snake_case PY aliases and EBITDA filter", Url = "/api/financials?companyId=TEKNI&periodFrom=202605&periodTo=202605&periodfrom_py=202505&periodto_py=202505&ebitda=1&IC_included=0" },
                    new ApiExampleSpec { Description = "Get rows for one period, default top 20000", Url = "/api/financials?periodFrom=202605&periodTo=202605" }
                },
                Response = new ApiResponseSpec
                {
                    Format = "application/json",
                    Shape = "success, count, top, filters, data[] including f.source, f.type, f.IC_included, a.cat_order, c.cg_order"
                }
            },
            new ApiHelpSpec
            {
                Name = "companies",
                Endpoint = "/api/companies",
                Method = "GET",
                Description = "Returns companies the user has access to based on sec.vw_user_company_access.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "User principal / email address." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec { Description = "Get accessible companies for one user", Url = "/api/companies?userPrincipal=user@company.no" }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, count, data[]" }
            },
            new ApiHelpSpec
            {
                Name = "orderbook-excel",
                Endpoint = "/api/orderbook/excel",
                Method = "GET",
                Description = "Returns orderbook in Excel-friendly Jan-Dec shape from normalized storage.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "User principal / email address." },
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = true, Description = "Company identifier." },
                    new ApiParameterSpec { Name = "year", Type = "int", Required = true, Description = "Selected base year, for example 2026." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec { Description = "Get orderbook in Excel shape", Url = "/api/orderbook/excel?userPrincipal=user@company.no&companyId=TEKNI&year=2026" }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, count, data[]" }
            },
            new ApiHelpSpec
            {
                Name = "orderbook-normalized",
                Endpoint = "/api/orderbook/normalized",
                Method = "GET",
                Description = "Returns orderbook in normalized project/month row format.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "User principal / email address." },
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = true, Description = "Company identifier." },
                    new ApiParameterSpec { Name = "year", Type = "int", Required = true, Description = "Selected base year, for example 2026." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec { Description = "Get orderbook normalized", Url = "/api/orderbook/normalized?userPrincipal=user@company.no&companyId=TEKNI&year=2026" }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, count, data[]" }
            },

            new ApiHelpSpec
            {
                Name = "tenders",
                Endpoint = "/api/tenders",
                Method = "GET",
                Description = "Returns tender/anbuds rows for one company and year.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "User principal / email address." },
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = true, Description = "Company identifier." },
                    new ApiParameterSpec { Name = "year", Type = "int", Required = true, Description = "Selected base year, for example 2026." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec { Description = "Get tenders for one company and year", Url = "/api/tenders?userPrincipal=user@company.no&companyId=TEKNI&year=2026" }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, count, data[]" }
            },
            new ApiHelpSpec
            {
                Name = "tenders-save",
                Endpoint = "/api/tenders/save",
                Method = "POST",
                Description = "Saves the full tender/anbuds dataset for one company and year.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "Body field. User principal / email address." },
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = true, Description = "Body field. Company identifier." },
                    new ApiParameterSpec { Name = "year", Type = "int", Required = true, Description = "Body field. Selected base year." },
                    new ApiParameterSpec { Name = "lines", Type = "array", Required = true, Description = "Body field. Tender rows with lineNo, projectName, projectType, customerName, contractorName, priorityText, contractType, statusText, commentText, expectedDecisionDate, expectedStartDate, estimatedMarginPct, estimatedRevenue, estimatedContribution." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec
                    {
                        Description = "Save tender payload",
                   Url = "/api/tenders/save  { \"userPrincipal\":\"user@company.no\", \"companyId\":\"TEKNI\", \"year\":2026, \"lines\":[{\"lineNo\":1,\"projectName\":\"Project A\",\"statusText\":\"Levert\"}] }"
          }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, message, lineCount" }
            },
            new ApiHelpSpec
            {
                Name = "orderbook-save",
                Endpoint = "/api/orderbook/save",
                Method = "POST",
                Description = "Saves the full orderbook dataset for one company and year using normalized project and month payloads.",
                Parameters = new List<ApiParameterSpec>
                {
                    new ApiParameterSpec { Name = "userPrincipal", Type = "string", Required = true, Description = "Body field. User principal / email address." },
                    new ApiParameterSpec { Name = "companyId", Type = "string", Required = true, Description = "Body field. Company identifier." },
                    new ApiParameterSpec { Name = "year", Type = "int", Required = true, Description = "Body field. Selected base year." },
                    new ApiParameterSpec { Name = "projects", Type = "array", Required = true, Description = "Body field. Project rows with lineNo, projectName, customerName, contractType, estimatedValue, remainingValue, nextYearAmount, commentText." },
                    new ApiParameterSpec { Name = "months", Type = "array", Required = true, Description = "Body field. Month rows with lineNo, periodDate (yyyy-MM-01), amount." }
                },
                Examples = new List<ApiExampleSpec>
                {
                    new ApiExampleSpec
                    {
                        Description = "Save orderbook payload",
                        Url = "/api/orderbook/save  { \"userPrincipal\":\"user@company.no\", \"companyId\":\"TEKNI\", \"year\":2026, \"projects\":[{\"lineNo\":1,\"projectName\":\"Project A\"}], \"months\":[{\"lineNo\":1,\"periodDate\":\"2026-01-01\",\"amount\":1000}] }"
                    }
                },
                Response = new ApiResponseSpec { Format = "application/json", Shape = "success, message, projectCount, monthCount" }
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
