using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ApiKeyAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthorizationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. On cherche la clé d'API dans les headers de la requête HTTP.
        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var providedApiKey))
        {
            // Si le header n'est pas là, c'est un accès non autorisé.
            context.Result = new UnauthorizedObjectResult("Clé d'API manquante.");
            return;
        }

        // 2. On récupère la vraie clé d'API depuis notre configuration.
        var validApiKey = _configuration.GetValue<string>("BackOffice:ApiKey");
        if (string.IsNullOrEmpty(validApiKey))
        {
            // Si la clé n'est pas configurée côté serveur, c'est une erreur critique.
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }


        // 3. On compare la clé fournie avec la clé attendue.
        if (!validApiKey.Equals(providedApiKey))
        {
            // Si les clés ne correspondent pas, l'accès est refusé.
            context.Result = new UnauthorizedObjectResult("Clé d'API invalide.");
            return;
        }

        // Si tout est bon, on laisse la requête continuer.
        await Task.CompletedTask;
    }
}

// On crée aussi l'attribut pour que ce soit plus simple à utiliser.
// Crée un fichier ApiKeyAuthorizeAttribute.cs
public class ApiKeyAuthorizeAttribute : TypeFilterAttribute
{
    public ApiKeyAuthorizeAttribute() : base(typeof(ApiKeyAuthorizationFilter))
    {
    }
}