using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Capturer.Api.Middleware;

/// <summary>
/// Custom authentication handler for API Key authentication
/// Implementación según plan v4.0 - Capa 1: Capturer Client Authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApiKeyQueryParam = "apiKey";

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Try to get API key from header first
            string? apiKey = Request.Headers[ApiKeyHeaderName].FirstOrDefault();
            
            // Fallback to query parameter
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Request.Query[ApiKeyQueryParam].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key is missing"));
            }

            // Validate API key (in production, this would check against database/config)
            if (!IsValidApiKey(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
            }

            // Create claims and identity
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Capturer Client"),
                new Claim("ApiKey", apiKey),
                new Claim("ComputerId", GetComputerIdFromApiKey(apiKey))
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API Key authentication");
            return Task.FromResult(AuthenticateResult.Fail("Authentication error"));
        }
    }

    private bool IsValidApiKey(string apiKey)
    {
        // TODO: Implement proper API key validation
        // For now, accept keys that start with "cap_" and are at least 32 characters
        return apiKey.StartsWith("cap_") && apiKey.Length >= 32;
    }

    private string GetComputerIdFromApiKey(string apiKey)
    {
        // TODO: Extract computer ID from API key or database lookup
        // For now, return machine name
        return Environment.MachineName;
    }
}

/// <summary>
/// Options for API Key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
    public string ApiKeyQueryParam { get; set; } = "apiKey";
}