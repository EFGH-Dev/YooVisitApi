// --- Nouveaux Usings pour le pattern IOptions ---
// ---------------------------------------------
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging; // Optionnel : pour logguer les erreurs
using Microsoft.Extensions.Options; // Pour IOptions<T>
using YooVisitApi.Configuration;  // Pour ta classe BackOfficeSettings

// Il est probable que ce filtre soit dans son propre dossier,
// ajuste le namespace si besoin.
namespace YooVisitApi.Filters
{
    public class ApiKeyAuthorizationFilter : IAsyncAuthorizationFilter
    {
        // 1. DÉPENDANCE FORTEMENT TYPÉE
        private readonly string _validApiKey;
        private readonly ILogger<ApiKeyAuthorizationFilter> _logger; // Bonus

        // 2. LE CONSTRUCTEUR REFACTORISÉ
        public ApiKeyAuthorizationFilter(
            IOptions<BackOfficeSettings> backOfficeOptions,
            ILogger<ApiKeyAuthorizationFilter> logger) // Logger injecté
        {
            // On "déballe" la clé UNE SEULE FOIS au démarrage.
            this._validApiKey = backOfficeOptions.Value.ApiKey;
            this._logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 1. On cherche la clé d'API dans les headers.
            if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var providedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("Clé d'API manquante.");
                return;
            }

            // 2. On vérifie si la clé est configurée (sécurité).
            if (string.IsNullOrEmpty(_validApiKey))
            {
                // C'est une erreur serveur si la clé n'est pas set.
                _logger.LogError("La clé d'API BackOffice n'est pas configurée dans les settings !");
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            // 3. On compare la clé fournie avec la clé attendue.
            // Utilise 'Equals' simple, c'est suffisant.
            if (!_validApiKey.Equals(providedApiKey))
            {
                // Clé invalide.
                context.Result = new UnauthorizedObjectResult("Clé d'API invalide.");
                return;
            }

            // Si tout est bon, on laisse la requête continuer.
            // 'await Task.CompletedTask;' n'est pas nécessaire si la méthode
            // se termine naturellement sans autre 'await'.
        }
    }

    // --- Ton attribut (qui est parfait et n'a pas besoin de changer) ---
    // Mets-le dans un fichier ApiKeyAuthorizeAttribute.cs
    public class ApiKeyAuthorizeAttribute : TypeFilterAttribute
    {
        public ApiKeyAuthorizeAttribute() : base(typeof(ApiKeyAuthorizationFilter))
        {
        }
    }
}