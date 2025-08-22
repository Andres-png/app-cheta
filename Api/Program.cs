using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Api.Data;
using Api.Services;
using Api.Endpoints; // 👈 nuevo namespace donde moveremos los endpoints

var builder = WebApplication.CreateBuilder(args);

// ----------------- BASE DE DATOS -----------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// ----------------- URLS FRONTEND -----------------
var baseUrl = builder.Configuration["Frontend:BaseUrl"];
var frontendUrl = builder.Configuration["Frontend:FrontendUrl"];

if (Environment.GetEnvironmentVariable("CODESPACES") == "true")
{
    var codespaceName = Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME");
    baseUrl = $"https://{codespaceName}-5174.app.github.dev";
    frontendUrl = $"https://{codespaceName}-5173.app.github.dev";
}

// ----------------- CORS -----------------
const string CorsPolicy = "AllowVite";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(frontendUrl!, "http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ----------------- JWT -----------------
var jwtKey = builder.Configuration["JwtKey"] ?? "ClaveSuperSecretaDev_CambiaEnProd_123!";
builder.Services.AddSingleton(new JwtService(jwtKey));

// ----------------- AUTENTICACIÓN -----------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/api/auth/google-callback";

    options.SaveTokens = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ----------------- MIGRACIÓN AUTOMÁTICA -----------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ----------------- MIDDLEWARE -----------------
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// ----------------- ENDPOINTS -----------------
app.MapApiEndpoints(); // 👈 se registran desde un archivo aparte

await app.RunAsync();
