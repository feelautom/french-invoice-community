using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Community.Endpoints;

public static class CommunityClientEndpoints
{
    public static void MapCommunityClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients");

        group.MapGet("/", async (IDbContextFactory<AppDbContext> factory) =>
        {
            using var db = factory.CreateDbContext();
            return Results.Ok(await db.Clients.Where(c => c.EntityId == 1).OrderBy(c => c.Nom).ToListAsync());
        });

        group.MapPost("/", async (Client client, IDbContextFactory<AppDbContext> factory) =>
        {
            client.EntityId = 1;
            client.CreatedAt = DateTime.UtcNow;
            client.UpdatedAt = DateTime.UtcNow;
            using var db = factory.CreateDbContext();
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return Results.Created($"/api/clients/{client.Id}", client);
        });
    }
}
