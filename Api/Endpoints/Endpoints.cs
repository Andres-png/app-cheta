using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Google.Apis.Auth;
using Api.Data;
using Api.Models;
using Api.Dtos;
using Api.Services;

namespace Api.Endpoints;

public static class Endpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // ---------- HEALTH ----------
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

        // ---------- REGISTER ----------
        app.MapPost("/api/register", async (UserDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return Results.BadRequest("Usuario y contraseña requeridos.");

            if (await db.Users.AnyAsync(u => u.Username == dto.Username))
                return Results.BadRequest("Usuario ya existe.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username.Trim(),
                Password = hashedPassword
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Registrado con éxito" });
        });

        // ---------- LOGIN ----------
        app.MapPost("/api/login", async (UserDto dto, AppDbContext db, JwtService jwt) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null) return Results.Unauthorized();

            bool isPasswordValid = user.Password.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(dto.Password, user.Password)
                : user.Password == dto.Password;

            if (!isPasswordValid) return Results.Unauthorized();

            if (!user.Password.StartsWith("$2"))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                await db.SaveChangesAsync();
            }

            var token = jwt.GenerateToken(user.Username);
            return Results.Ok(new { token, username = user.Username });
        });

        // ---------- LOGIN CON GOOGLE ----------
        app.MapGet("/api/auth/google-login", () =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/google-callback"
            };

            return Results.Challenge(properties, new List<string> { "Google" });
        });

        app.MapGet("/api/auth/google-callback", async (HttpContext context, AppDbContext db, JwtService jwt, IConfiguration config) =>
        {
            var frontendUrl = config["Frontend:FrontendUrl"];

            try
            {
                var result = await context.AuthenticateAsync("Google");

                if (!result.Succeeded)
                    return Results.Redirect($"{frontendUrl}/login?error=google_auth_failed");

                var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                    return Results.Redirect($"{frontendUrl}/login?error=no_email");

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

                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.Token, settings);
                var email = payload.Email;

                if (string.IsNullOrWhiteSpace(email))
                    return Results.BadRequest("No se obtuvo email de Google");

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
    }
}
