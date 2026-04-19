using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;
using FrenchInvoice.Community.Components;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "frenchinvoice.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Community edition : pas d'Infisical, pas de SoftLic, pas de PaymentPlatformService
builder.Services.AddSingleton<IEditionProvider>(new EditionProvider("Community"));
builder.Services.AddSingleton<ISecretProvider, NullSecretProvider>();

builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<DeclarationService>();
builder.Services.AddSingleton<BankImportService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<PdfGenerationService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<SiretLookupService>();
builder.Services.AddSingleton<NatureJuridiqueService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<HashChainService>();
builder.Services.AddScoped<ClosingService>();
builder.Services.AddScoped<FecExportService>();

// Auth : toujours authentifie en tant qu'Admin de l'entite 1
builder.Services.AddScoped<CommunityAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CommunityAuthStateProvider>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();

    db.Database.Migrate();

    // Auto-creer l'entite et l'utilisateur admin
    if (!db.Entities.Any())
    {
        var communityEntity = new Entity
        {
            Nom = "Mon entreprise",
            Configured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Entities.Add(communityEntity);
        db.SaveChanges();

        var communityUser = new User
        {
            Username = "admin",
            Email = "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("community"),
            EntityId = communityEntity.Id,
            Role = UserRole.Admin
        };
        db.Users.Add(communityUser);
        db.SaveChanges();

        app.Logger.LogInformation("Community: entite et utilisateur admin crees automatiquement");
    }
}

// Backfill hash chain for existing entities
using (var scope = app.Services.CreateScope())
{
    var hashChain = scope.ServiceProvider.GetRequiredService<HashChainService>();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = dbFactory.CreateDbContext();
    var entityIds = db.Entities.Select(e => e.Id).ToList();
    foreach (var eid in entityIds)
        await hashChain.BackfillChainAsync(eid);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Headers de securite
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoints de telechargement PDF
app.MapGet("/download/invoices/{id:int}/pdf", async (int id, IDbContextFactory<AppDbContext> dbFactory) =>
{
    using var db = dbFactory.CreateDbContext();
    var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id);
    if (invoice == null || string.IsNullOrEmpty(invoice.CheminPdf) || !File.Exists(invoice.CheminPdf))
        return Results.NotFound();
    var bytes = await File.ReadAllBytesAsync(invoice.CheminPdf);
    return Results.File(bytes, "application/pdf", Path.GetFileName(invoice.CheminPdf));
});

app.MapGet("/download/quotes/{id:int}/pdf", async (int id, IDbContextFactory<AppDbContext> dbFactory) =>
{
    using var db = dbFactory.CreateDbContext();
    var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id);
    if (quote == null || string.IsNullOrEmpty(quote.CheminPdf) || !File.Exists(quote.CheminPdf))
        return Results.NotFound();
    var bytes = await File.ReadAllBytesAsync(quote.CheminPdf);
    return Results.File(bytes, "application/pdf", Path.GetFileName(quote.CheminPdf));
});

// Health check
app.MapGet("/health", async (IDbContextFactory<AppDbContext> dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateDbContext();
        await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected"
        });
    }
    catch
    {
        return Results.Json(new
        {
            status = "unhealthy",
            timestamp = DateTime.UtcNow,
            database = "disconnected"
        }, statusCode: 503);
    }
});

app.Run();

// Necessaire pour WebApplicationFactory dans les tests d'integration
public partial class Program { }
