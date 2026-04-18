using FrenchInvoice.Core.Services;

namespace FrenchInvoice.Community.Endpoints;

public static class CommunityExportEndpoints
{
    public static void MapCommunityExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/export", async (ExportService exportService) =>
        {
            var bytes = await exportService.ExportEntityAsync(1);
            return Results.File(bytes, "application/zip", $"FrenchInvoice_Export_{DateTime.Now:yyyyMMdd}.zip");
        });

        group.MapPost("/import", async (HttpContext context, ImportService importService) =>
        {
            if (!context.Request.HasFormContentType)
                return Results.BadRequest(new { error = "Multipart form requis." });

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "Fichier ZIP requis." });

            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            await importService.ImportAsync(1, ms);
            return Results.Ok(new { message = "Import termine." });
        }).DisableAntiforgery();
    }
}
