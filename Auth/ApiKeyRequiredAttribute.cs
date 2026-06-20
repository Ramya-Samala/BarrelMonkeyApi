using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BarrelMonkeyApi.Auth;


/*Lightweight API key auth — attach this attribute to any controller or action
you want to protect.

 Clients must send the key in one of two ways:
   1. Header:       X-Api-Key: your-secret-key
   2. Query string: ?apiKey=your-secret-key
 The key itself is configured in appsettings.json under "Auth:ApiKey".
 If no key is configured, auth is effectively disabled (useful for local dev).*/

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyRequiredAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private const string QueryParamName = "apiKey";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Grab the configured API key from DI
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["Auth:ApiKey"];

        // If no key is set in config, skip auth entirely — handy for local dev
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            await next();
            return;
        }

        // Check header first, fall back to query string
        var providedKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault()
                          ?? context.HttpContext.Request.Query[QueryParamName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedKey) || providedKey != expectedKey)
        {
            // Log failed auth attempts — useful for spotting abuse
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<ApiKeyRequiredAttribute>>();
            logger.LogWarning("Unauthorized request to {Path} from {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress);

            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Missing or invalid API key. Provide it via the X-Api-Key header or ?apiKey= query param."
            });
            return;
        }

        // Key checks out — let the request through
        await next();
    }
}
