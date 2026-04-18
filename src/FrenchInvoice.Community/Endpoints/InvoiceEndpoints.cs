using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;

namespace FrenchInvoice.Community.Endpoints;

public static class CommunityInvoiceEndpoints
{
    public static void MapCommunityInvoiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/invoices");

        group.MapGet("/", async (InvoiceService service, InvoiceStatus? status, DateTime? from, DateTime? to) =>
        {
            var invoices = await service.GetAllAsync(status, null, from, to);
            return Results.Ok(invoices.Select(i => new
            {
                i.Id, i.Numero, i.DateEmission, i.DateEcheance,
                Statut = i.Statut.ToString(),
                Client = i.Client?.Nom,
                i.ClientId, i.MontantHT, i.MontantTVA, i.MontantTTC,
                LignesCount = i.Lignes.Count
            }));
        });

        group.MapGet("/{id:int}", async (int id, InvoiceService service) =>
        {
            var invoice = await service.GetByIdAsync(id);
            if (invoice == null) return Results.NotFound();
            return Results.Ok(new
            {
                invoice.Id, invoice.Numero, invoice.DateEmission, invoice.DateEcheance,
                Statut = invoice.Statut.ToString(),
                Client = new
                {
                    invoice.Client.Id, invoice.Client.Nom, invoice.Client.Email,
                    invoice.Client.Adresse, invoice.Client.CodePostal, invoice.Client.Ville,
                    invoice.Client.Siret, invoice.Client.TvaIntracommunautaire
                },
                invoice.MontantHT, invoice.MontantTVA, invoice.MontantTTC,
                invoice.Notes, invoice.CheminPdf,
                Lignes = invoice.Lignes.Select(l => new
                {
                    l.Description, l.Quantite, l.PrixUnitaire, l.TauxTVA, l.MontantHT
                })
            });
        });

        group.MapPost("/", async (CreateInvoiceRequest request, InvoiceService service) =>
        {
            try
            {
                var invoice = new Invoice
                {
                    ClientId = request.ClientId,
                    DateEmission = request.DateEmission ?? DateTime.Today,
                    DateEcheance = request.DateEcheance ?? DateTime.Today.AddDays(30),
                    Notes = request.Notes,
                    Lignes = request.Lignes.Select(l => new InvoiceLine
                    {
                        Description = l.Description,
                        Quantite = l.Quantite,
                        PrixUnitaire = l.PrixUnitaire,
                        TauxTVA = l.TauxTVA
                    }).ToList()
                };

                var created = await service.CreateAsync(invoice);

                if (request.Finaliser)
                    created = await service.FinaliserAsync(created.Id);

                return Results.Created($"/api/invoices/{created.Id}", new
                {
                    created.Id, created.Numero,
                    Statut = created.Statut.ToString(),
                    created.MontantHT, created.MontantTVA, created.MontantTTC
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/{id:int}/pdf", async (int id, InvoiceService service) =>
        {
            var invoice = await service.GetByIdAsync(id);
            if (invoice == null) return Results.NotFound();
            if (string.IsNullOrEmpty(invoice.CheminPdf) || !File.Exists(invoice.CheminPdf))
                return Results.NotFound(new { error = "PDF not found" });

            var bytes = await File.ReadAllBytesAsync(invoice.CheminPdf);
            return Results.File(bytes, "application/pdf", Path.GetFileName(invoice.CheminPdf));
        });

        group.MapPut("/{id:int}/status", async (int id, ChangeStatusRequest request, InvoiceService service) =>
        {
            try
            {
                Invoice invoice;
                switch (request.Statut?.ToLower())
                {
                    case "envoyee":
                        invoice = await service.FinaliserAsync(id);
                        break;
                    case "payee":
                        invoice = await service.MarquerPayeeAsync(id, request.ModePaiement);
                        break;
                    case "annulee":
                        invoice = await service.AnnulerAsync(id);
                        break;
                    default:
                        return Results.BadRequest(new { error = "Statut invalide. Valeurs acceptees: envoyee, payee, annulee" });
                }
                return Results.Ok(new { invoice.Id, invoice.Numero, Statut = invoice.Statut.ToString() });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

public record CreateInvoiceRequest
{
    public int ClientId { get; init; }
    public DateTime? DateEmission { get; init; }
    public DateTime? DateEcheance { get; init; }
    public string? Notes { get; init; }
    public bool Finaliser { get; init; }
    public List<CreateInvoiceLineRequest> Lignes { get; init; } = new();
}

public record CreateInvoiceLineRequest
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantite { get; init; } = 1;
    public decimal PrixUnitaire { get; init; }
    public decimal TauxTVA { get; init; }
}

public record ChangeStatusRequest
{
    public string? Statut { get; init; }
    public string? ModePaiement { get; init; }
}
