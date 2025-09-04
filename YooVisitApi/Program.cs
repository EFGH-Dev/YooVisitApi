using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using YooVisitApi.Data;
using YooVisitApi.Interfaces;
using YooVisitApi.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration des services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- Configuration du client S3 pour OVHcloud ---

// 1. Récupérer les infos de appsettings.json
var s3ConfigSection = builder.Configuration.GetSection("ObjectStorage");
var serviceUrl = s3ConfigSection["ServiceUrl"];
var accessKey = s3ConfigSection["AccessKey"];
var secretKey = s3ConfigSection["SecretKey"];

// 2. Créer l'objet de configuration S3 COMPLET
var s3Config = new AmazonS3Config
{
    ServiceURL = serviceUrl,
    AuthenticationRegion = "GRA", // Ta région OVH
    ForcePathStyle = true
};
var credentials = new BasicAWSCredentials(accessKey, secretKey);

// 3. Inscrire le client S3 dans l'injection de dépendances pour qu'il soit réutilisable
builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));

// 4. Inscrire ton propre service qui va utiliser le client S3
builder.Services.AddScoped<IObjectStorageService, S3StorageService>();

// --- Fin de la configuration S3 ---

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IObjectStorageService, S3StorageService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "_myAllowSpecificOrigins",
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // On configure le sérialiseur pour utiliser le camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ApiKeyAuthorizationFilter>();

var app = builder.Build();

// --- LOGIQUE DE DÉMARRAGE : MIGRATION ET SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApiDbContext>();
        // On vérifie s'il y a des migrations en attente avant de les appliquer
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }
        // Le seeder peut être appelé ensuite
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue pendant la migration ou le seeding de la BDD.");
    }
}
// ----------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseCors("_myAllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();