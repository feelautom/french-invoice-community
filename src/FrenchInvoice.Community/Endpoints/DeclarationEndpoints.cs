using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;

namespace FrenchInvoice.Community.Endpoints;

public static class CommunityDeclarationEndpoints
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public static void MapCommunityDeclarationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/declarations");

        // Upload justificatif
        group.MapPost("/{id}/justificatif", async (int id, IFormFile file,
            IDbContextFactory<AppDbContext> factory, IWebHostEnvironment env) =>
        {
            const int entityId = 1;

            if (file.Length == 0 || file.Length > MaxFileSize)
                return Results.BadRequest("Fichier vide ou trop volumineux (max 10 Mo)");

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return Results.BadRequest("Type de fichier non autorise (PDF, JPG, PNG uniquement)");

            using var db = factory.CreateDbContext();
            var declaration = await db.Declarations
                .FirstOrDefaultAsync(d => d.Id == id && d.EntityId == entityId);
            if (declaration == null)
                return Results.NotFound();

            var dir = Path.Combine(env.ContentRootPath, "Data", "declarations", entityId.ToString());
            Directory.CreateDirectory(dir);

            if (!string.IsNullOrEmpty(declaration.JustificatifFileName))
            {
                var oldPath = Path.Combine(dir, declaration.JustificatifFileName);
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var fileName = $"{declaration.Id}{ext}";
            var filePath = Path.Combine(dir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            declaration.JustificatifFileName = fileName;
            db.Declarations.Update(declaration);
            await db.SaveChangesAsync();

            return Results.Ok(new { fileName });
        }).DisableAntiforgery();

        // Download justificatif
        group.MapGet("/{id}/justificatif", async (int id,
            IDbContextFactory<AppDbContext> factory, IWebHostEnvironment env) =>
        {
            const int entityId = 1;

            using var db = factory.CreateDbContext();
            var declaration = await db.Declarations
                .FirstOrDefaultAsync(d => d.Id == id && d.EntityId == entityId);
            if (declaration == null || string.IsNullOrEmpty(declaration.JustificatifFileName))
                return Results.NotFound();

            var dir = Path.Combine(env.ContentRootPath, "Data", "declarations", entityId.ToString());
            var filePath = Path.Combine(dir, declaration.JustificatifFileName);

            if (!File.Exists(filePath))
                return Results.NotFound();

            var resolvedPath = Path.GetFullPath(filePath);
            var resolvedDir = Path.GetFullPath(dir);
            if (!resolvedPath.StartsWith(resolvedDir))
                return Results.Forbid();

            var contentType = Path.GetExtension(declaration.JustificatifFileName).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return Results.File(filePath, contentType, declaration.JustificatifFileName);
        });

        // Supprimer justificatif
        group.MapDelete("/{id}/justificatif", async (int id,
            IDbContextFactory<AppDbContext> factory, IWebHostEnvironment env) =>
        {
            const int entityId = 1;

            using var db = factory.CreateDbContext();
            var declaration = await db.Declarations
                .FirstOrDefaultAsync(d => d.Id == id && d.EntityId == entityId);
            if (declaration == null)
                return Results.NotFound();

            if (!string.IsNullOrEmpty(declaration.JustificatifFileName))
            {
                var dir = Path.Combine(env.ContentRootPath, "Data", "declarations", entityId.ToString());
                var filePath = Path.Combine(dir, declaration.JustificatifFileName);
                if (File.Exists(filePath)) File.Delete(filePath);

                declaration.JustificatifFileName = null;
                db.Declarations.Update(declaration);
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        });
    }
}
