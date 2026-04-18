using FrenchInvoice.Core.Models;
using FrenchInvoice.Core.Services;

namespace FrenchInvoice.Community.Endpoints;

public static class CommunityQuoteEndpoints
{
    public static void MapCommunityQuoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (QuoteService service, QuoteStatus? status, DateTime? from, DateTime? to) =>
        {
            var quotes = await service.GetAllAsync(status, null, from, to);
            return Results.Ok(quotes.Select(q => new
            {
                q.Id, q.Numero, q.DateEmission, q.DateValidite,
                Statut = q.Statut.ToString(),
                Client = q.Client?.Nom,
                q.ClientId, q.MontantHT, q.MontantTVA, q.MontantTTC,
                q.InvoiceId
            }));
        });

        group.MapPost("/", async (CreateQuoteRequest request, QuoteService service) =>
        {
            try
            {
                var quote = new Quote
                {
                    ClientId = request.ClientId,
                    DateEmission = request.DateEmission ?? DateTime.Today,
                    DateValidite = request.DateValidite ?? DateTime.Today.AddDays(30),
                    Notes = request.Notes,
                    Lignes = request.Lignes.Select(l => new QuoteLine
                    {
                        Description = l.Description,
                        Quantite = l.Quantite,
                        PrixUnitaire = l.PrixUnitaire,
                        TauxTVA = l.TauxTVA
                    }).ToList()
                };

                var created = await service.CreateAsync(quote);
                return Results.Created($"/api/quotes/{created.Id}", new
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

        group.MapGet("/{id:int}/pdf", async (int id, QuoteService service) =>
        {
            var quote = await service.GetByIdAsync(id);
            if (quote == null) return Results.NotFound();
            if (string.IsNullOrEmpty(quote.CheminPdf) || !File.Exists(quote.CheminPdf))
                return Results.NotFound(new { error = "PDF not found" });

            var bytes = await File.ReadAllBytesAsync(quote.CheminPdf);
            return Results.File(bytes, "application/pdf", Path.GetFileName(quote.CheminPdf));
        });

        group.MapPost("/{id:int}/convert", async (int id, QuoteService service) =>
        {
            try
            {
                var invoice = await service.ConvertirEnFactureAsync(id);
                return Results.Ok(new
                {
                    invoice.Id, invoice.Numero,
                    Statut = invoice.Statut.ToString(),
                    invoice.MontantHT, invoice.MontantTVA, invoice.MontantTTC
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

public record CreateQuoteRequest
{
    public int ClientId { get; init; }
    public DateTime? DateEmission { get; init; }
    public DateTime? DateValidite { get; init; }
    public string? Notes { get; init; }
    public List<CreateQuoteLineRequest> Lignes { get; init; } = new();
}

public record CreateQuoteLineRequest
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantite { get; init; } = 1;
    public decimal PrixUnitaire { get; init; }
    public decimal TauxTVA { get; init; }
}
