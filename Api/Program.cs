using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// Detectar si estamos en Codespaces
var isCodespaces = Environment.GetEnvironmentVariable("CODESPACES") == "true";
var baseUrl = isCodespaces 
    ? $"https://{Environment.GetEnvironmentVariable("cautious-fishstick-r4497qj7rwg6f5rqw")}-5174.app.github.dev"
    : "http://localhost:5174";
var frontendUrl = isCodespaces 
    ? $"https://{Environment.GetEnvironmentVariable("cautious-fishstick-r4497qj7rwg6f5rqw")}-5173.app.github.dev"
    : "http://localhost:5173";

const string CorsPolicy = "AllowVite";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var jwtKey = builder.Configuration["JwtKey"] ?? "ClaveSuperSecretaDev_CambiaEnProd_123!";
builder.Services.AddSingleton(new JwtService(jwtKey));

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
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
        
        // Configuración adicional para el callback
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        
        options.Events.OnCreatingTicket = context =>
        {
            // Agregar claims adicionales si es necesario
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// ---------- AUTENTICACIÓN ----------
app.MapPost("/api/register", async (UserDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Usuario y contraseña requeridos.");

    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
        return Results.BadRequest("Usuario ya existe.");

    var user = new User { Username = dto.Username.Trim(), Password = dto.Password };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Registrado con éxito" });
});

app.MapPost("/api/login", async (UserDto dto, AppDbContext db, JwtService jwt) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.Password == dto.Password);
    if (user == null) return Results.Unauthorized();

    var token = jwt.GenerateToken(user.Username);
    return Results.Ok(new { token, username = user.Username });
});

// ---------- LOGIN CON GOOGLE (CORREGIDO) ----------
app.MapGet("/api/auth/google-login", () =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/api/auth/google-callback"
    };
    
    return Results.Challenge(properties, new List<string> { "Google" });
});

app.MapGet("/api/auth/google-callback", async (HttpContext context, AppDbContext db, JwtService jwt) =>
{
    try
    {
        Console.WriteLine("🔄 Procesando callback de Google...");
        Console.WriteLine($"Query string: {context.Request.QueryString}");
        
        // Autenticar con Google
        var result = await context.AuthenticateAsync("Google");
        
        Console.WriteLine($"Authentication result succeeded: {result.Succeeded}");
        
        if (!result.Succeeded)
        {
            Console.WriteLine("❌ Error en autenticación con Google");
            Console.WriteLine($"Failure message: {result.Failure?.Message}");
            return Results.Redirect($"{frontendUrl}/login?error=google_auth_failed");
        }

        var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;
        
        Console.WriteLine($"Claims found - Email: {email}, Name: {name}");
        Console.WriteLine($"All claims: {string.Join(", ", result.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? new string[0])}");

        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ No se pudo obtener el email de Google");
            return Results.Redirect($"{frontendUrl}/login?error=no_email");
        }

        // Buscar o crear usuario
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == email);
        if (user == null)
        {
            user = new User
            {
                Username = email,
                Password = "GOOGLE_AUTH_" + Guid.NewGuid().ToString()
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ Usuario creado: {email}");
        }
        else
        {
            Console.WriteLine($"✅ Usuario existente encontrado: {email}");
        }

        var token = jwt.GenerateToken(user.Username);
        Console.WriteLine($"✅ Token generado para: {user.Username}");

        return Results.Redirect($"{frontendUrl}/login?token={token}&username={Uri.EscapeDataString(user.Username)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Excepción en Google callback: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Redirect($"{frontendUrl}/login?error=callback_exception");
    }
});

// ---------- CRUD FORMULARIO ----------
app.MapPost("/api/form", async (FormDataDto dto, AppDbContext db, HttpContext http) =>
{
    var username = http.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

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
    var username = http.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

    var contactos = await db.FormData
        .Where(f => f.Username == username)
        .OrderByDescending(f => f.Id)
        .ToListAsync();

    return Results.Ok(contactos);
}).RequireAuthorization();

app.MapGet("/api/form/{id}", async (int id, AppDbContext db, HttpContext http) =>
{
    var username = http.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    if (contacto == null) return Results.NotFound();

    return Results.Ok(contacto);
}).RequireAuthorization();

app.MapPut("/api/form/{id}", async (int id, FormDataDto dto, AppDbContext db, HttpContext http) =>
{
    var username = http.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    if (contacto == null) return Results.NotFound();

    contacto.Nombre = dto.Nombre.Trim();
    contacto.Email = dto.Email.Trim();
    contacto.Telefono = dto.Telefono?.Trim() ?? "";
    contacto.Nota = dto.Nota?.Trim() ?? "";

    await db.SaveChangesAsync();
    return Results.Ok(contacto);
}).RequireAuthorization();

app.MapDelete("/api/form/{id}", async (int id, AppDbContext db, HttpContext http) =>
{
    var username = http.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

    var contacto = await db.FormData.FirstOrDefaultAsync(f => f.Id == id && f.Username == username);
    if (contacto == null) return Results.NotFound();

    db.FormData.Remove(contacto);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Contacto eliminado" });
}).RequireAuthorization();

app.Run();

// ---------- RECORDS Y MODELOS ----------
record UserDto(string Username, string Password);
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