using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// ----------------- BASE DE DATOS -----------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// ----------------- URLS FRONTEND -----------------
var isCodespaces = Environment.GetEnvironmentVariable("CODESPACES") == "true";
var baseUrl = isCodespaces
    ? $"https://{Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME")}-5174.app.github.dev"
    : "http://localhost:5174";
var frontendUrl = isCodespaces
    ? $"https://{Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME")}-5173.app.github.dev"
    : "http://localhost:5173";

// ----------------- CORS -----------------
const string CorsPolicy = "AllowVite";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:5173", "http://127.0.0.1:5173")
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
    db.Database.EnsureCreated();
}

// ----------------- MIDDLEWARE -----------------
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// ----------------- HELPERS -----------------
string? GetUsername(HttpContext http) => http.User.Identity?.Name;

bool IsPasswordValid(User user, string inputPassword)
{
    if (user.Password.StartsWith("$2")) // BCrypt
        return BCrypt.Net.BCrypt.Verify(inputPassword, user.Password);

    return user.Password == inputPassword; // Legacy
}

async Task UpgradePasswordHashIfNeeded(User user, string inputPassword, AppDbContext db)
{
    if (!user.Password.StartsWith("$2") && user.Password == inputPassword)
    {
        user.Password = BCrypt.Net.BCrypt.HashPassword(inputPassword);
        await db.SaveChangesAsync();
    }
}

async Task<User> GetOrCreateGoogleUser(string email, AppDbContext db)
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == email);
    if (user == null)
    {
        user = new User
        {
            Username = email,
            Password = BCrypt.Net.BCrypt.HashPassword("GOOGLE_AUTH_" + Guid.NewGuid().ToString())
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    return user;
}

// ----------------- ENDPOINTS -----------------
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// ---------- REGISTER ----------
app.MapPost("/api/register", async (UserDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Usuario y contraseña requeridos.");

    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
        return Results.BadRequest("Usuario ya existe.");

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    var user = new User { Username = dto.Username.Trim(), Password = hashedPassword };

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Registrado con éxito" });
});

// ---------- LOGIN ----------
app.MapPost("/api/login", async (UserDto dto, AppDbContext db, JwtService jwt) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    if (user == null) return Results.Unauthorized();

    if (!IsPasswordValid(user, dto.Password))
        return Results.Unauthorized();

    await UpgradePasswordHashIfNeeded(user, dto.Password, db);

    var token = jwt.GenerateToken(user.Username);
    return Results.Ok(new { token, username = user.Username });
});

// ---------- LOGIN CON GOOGLE ----------
app.MapGet("/api/auth/google-login", () =>
{
    var properties = new AuthenticationProperties { RedirectUri = "/api/auth/google-callback" };
    return Results.Challenge(properties, new List<string> { "Google" });
});

app.MapGet("/api/auth/google-callback", async (HttpContext context, AppDbContext db, JwtService jwt) =>
{
    try
    {
        var result = await context.AuthenticateAsync("Google");
        if (!result.Succeeded)
            return Results.Redirect($"{frontendUrl}/login?error=google_auth_failed");

        var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Results.Redirect($"{frontendUrl}/login?error=no_email");

        var user = await GetOrCreateGoogleUser(email, db);
        var token = jwt.GenerateToken(user.Username);

        return Results.Redirect($"{frontendUrl}/login?token={token}&username={Uri.EscapeDataString(user.Username)}");
    }
    catch
    {
        return Results.Redirect($"{frontendUrl}/login?error=callback_exception");
    }
});

app.MapPost("/api/auth/google", async (GoogleTokenDto dto, AppDbContext db, JwtService jwt, IConfiguration cfg) =>
{
    try
    {
        var clientId = cfg["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(clientId))
            return Results.BadRequest("Token o ClientId faltante");

        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.Token,
            new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });

        if (string.IsNullOrWhiteSpace(payload.Email))
            return Results.BadRequest("No se obtuvo email de Google");

        var user = await GetOrCreateGoogleUser(payload.Email, db);
        var token = jwt.GenerateToken(user.Username);

        return Results.Ok(new { token, username = user.Username });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error validando token de Google: {ex.Message}");
    }
});

// ---------- CRUD FORMULARIO ----------
app.MapPost("/api/form", async (FormDataDto dto, AppDbContext db, HttpContext http) =>
{
    var username = GetUsername(http);
    if (username is null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Email))
        return Results.BadRequest("Nombre y Email requeridos.");

    var contacto = new FormData
    {
        Username = username,
        Nombre = dto.Nombre.Trim(),
        Email = dto.Email.Trim(),
        Telefono = dto.Telefono?.Trim() ?? "",
        Nota = dto.Nota?.Trim() ?? ""
    };

    db.FormData.Add(contacto);
    await db.SaveChangesAsync();
    return Results.Ok(contacto);
}).RequireAuthorization();

app.MapGet("/api/form", async (AppDbContext db, HttpContext http) =>
{
    var username = GetUsername(http);
    if (username is null) return Results.Unauthorized();

    var contactos = await db.FormData
        .Where(f => f.Username == username)
        .OrderByDescending(f => f.Id)
        .ToListAsync();

    return Results.Ok(contactos);
}).RequireAuthorization();

app.MapGet("/api/form/{id}", async (int id, AppDbContext db, HttpContext http) =>
{
    var username = GetUsername(http);
    if (username is null) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    return contacto is null ? Results.NotFound() : Results.Ok(contacto);
}).RequireAuthorization();

app.MapPut("/api/form/{id}", async (int id, FormDataDto dto, AppDbContext db, HttpContext http) =>
{
    var username = GetUsername(http);
    if (username is null) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    if (contacto is null) return Results.NotFound();

    contacto.Nombre = dto.Nombre.Trim();
    contacto.Email = dto.Email.Trim();
    contacto.Telefono = dto.Telefono?.Trim() ?? "";
    contacto.Nota = dto.Nota?.Trim() ?? "";

    await db.SaveChangesAsync();
    return Results.Ok(contacto);
}).RequireAuthorization();

app.MapDelete("/api/form/{id}", async (int id, AppDbContext db, HttpContext http) =>
{
    var username = GetUsername(http);
    if (username is null) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    if (contacto is null) return Results.NotFound();

    db.FormData.Remove(contacto);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Contacto eliminado" });
}).RequireAuthorization();

app.Run();

// ----------------- RECORDS Y MODELOS -----------------
record UserDto(string Username, string Password);
record GoogleTokenDto(string Token);
record FormDataDto(string Nombre, string Email, string? Telefono, string? Nota);

class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}

class FormData
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public string Telefono { get; set; } = "";
    public string Nota { get; set; } = "";
}

class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<FormData> FormData => Set<FormData>();
}

class JwtService
{
    private readonly string _key;
    public JwtService(string key) => _key = key;

    public string GenerateToken(string username)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
