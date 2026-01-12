// --- USINGS NÉCESSAIRES ---
// Pour IOptions<T>
using Microsoft.Extensions.Options;
// Pour nos classes de settings (JwtSettings, etc.)
using YooVisitApi.Configuration;
// Pour le client S3
using Amazon.Runtime;
using Amazon.S3;
// Pour l'authentification
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
// Pour EF Core
using Microsoft.EntityFrameworkCore;
// Pour le reste...
using System.Text;
using YooVisitApi.Data;
using YooVisitApi.Interfaces;
using YooVisitApi.RealTime;
using YooVisitApi.Services;
using YooVisitApi.Hubs; // N'oublie pas le using pour ton Hub
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using YooVisitApi.Filters;

// --- INITIALISATION DU MOTEUR ---
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// --- ⚙️ 1. BINDING DES CONFIGURATIONS (Le Cœur du Refactor) ---
// On dit à .NET de lire les sections de appsettings/secrets.json
// et de les "binder" (lier) à nos classes POCO.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")
);
builder.Services.Configure<ObjectStorageSettings>(
    builder.Configuration.GetSection("ObjectStorage")
);
builder.Services.Configure<BackOfficeSettings>(
    builder.Configuration.GetSection("BackOffice")
);
// Désormais, on n'utilisera PLUS JAMAIS builder.Configuration["Jwt:Key"]

// --- ⚙️ 2. CONFIGURATION DES SERVICES (INJECTION) ---

// Base de données (EF Core)
// (GetConnectionString est le seul "magic string" acceptable)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(connectionString,
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Client S3 (Refactorisé pour IOptions)
// On enregistre le client S3 en Singleton en utilisant une "factory"
// qui utilise l'injection de dépendances (sp = Service Provider).
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    // On récupère nos settings S3 fortement typés
    ObjectStorageSettings settings = sp.GetRequiredService<IOptions<ObjectStorageSettings>>().Value;

    BasicAWSCredentials credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
    AmazonS3Config s3Config = new AmazonS3Config
    {
        ServiceURL = settings.ServiceUrl,
        AuthenticationRegion = "GRA", // Tu peux aussi mettre ça dans tes settings
        ForcePathStyle = true
    };

    // On retourne le client S3 construit, prêt à être injecté
    return new AmazonS3Client(credentials, s3Config);
});

// Service de Storage (🐞 Correction du bug de double enregistrement)
// On l'enregistre UNE SEULE FOIS. Singleton est bien car son client IAmazonS3 l'est.
builder.Services.AddSingleton<IObjectStorageService, S3StorageService>();

// Services métiers
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ApiKeyAuthorizationFilter>();

// Utilitaires
builder.Services.AddHttpClient();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddHealthChecks();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configuration Proxy (ton code était bon)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// SignalR (✨ Amélioration : Erreurs détaillées en Dev seulement)
builder.Services.AddSignalR(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = true;
    }
});

// CORS (✨ Amélioration : Politiques par environnement)
string DevCorsPolicy = "DevCorsPolicy";
string ProdCorsPolicy = "ProdCorsPolicy";

builder.Services.AddCors(options =>
{
    // Politique "Far West" pour le dev local
    options.AddPolicy(name: DevCorsPolicy,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });

    // Politique "Forteresse" pour la prod
    options.AddPolicy(name: ProdCorsPolicy,
                      policy =>
                      {
                          // Mets ici l'URL de ton app Flutter live
                          policy.WithOrigins("https://app.yoovisit.com", "https://www.yoovisit.com")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Swagger (✨ Amélioration : En Dev seulement)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// --- 🛡️ 3. AUTHENTIFICATION (Le 2ème Gros Refactor) ---
// On configure l'authentification...
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(); // ...on l'ajoute...

// ...et ENSUITE, on configure ses options en utilisant la DI !
// C'est le pattern le plus propre pour lier IOptions à AddJwtBearer.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtSettings) =>
    {
        // On récupère les settings bindés (jwtSettings.Value)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Value.Issuer,
            ValidAudience = jwtSettings.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Key))
        };

        // Ton patch vital pour SignalR (il est parfait)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
// -----------------------------------------------------


// --- 🚀 4. CONSTRUCTION DE L'APP ---
WebApplication app = builder.Build();

// --- 📦 5. CONFIGURATION DU PIPELINE (MIDDLEWARE) ---
// L'ordre est TRÈS important ici.

// Gestion des erreurs et HSTS en Production
if (!app.Environment.IsDevelopment())
{
    // Ton handler d'exception custom (très bien)
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Une erreur non gérée est survenue: {Error}", context.Features.Get<IExceptionHandlerFeature>()?.Error);
            await Results.Problem().ExecuteAsync(context);
        });
    });
    // Forcer le HTTPS en prod
    app.UseHsts();
}

// Outils de Dev (Swagger)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.UseForwardedHeaders(); // Pour les proxies
app.UseHttpsRedirection(); // Toujours rediriger vers HTTPS
app.UseWebSockets(); // Pour SignalR

// On applique la politique CORS en fonction de l'environnement
app.UseCors(app.Environment.IsDevelopment() ? DevCorsPolicy : ProdCorsPolicy);

app.UseAuthentication(); // 1. Qui es-tu ?
app.UseAuthorization(); // 2. Qu'as-tu le droit de faire ?

// On "map" les contrôleurs et les hubs
app.MapControllers();
app.MapHub<Updates>("/hubs/updates");

// --- 💾 6. LOGIQUE DE DÉMARRAGE (Migration BDD) ---
// Ton code était parfait, je le garde tel quel.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApiDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue pendant la migration ou le seeding de la BDD.");
    }
}
// ----------------------------------------------------

// --- 🏃 7. DÉMARRAGE DU SERVEUR ---
app.Run();