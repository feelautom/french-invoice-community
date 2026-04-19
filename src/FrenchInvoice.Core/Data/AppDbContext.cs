using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Revenue> Revenues => Set<Revenue>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Declaration> Declarations => Set<Declaration>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<FixedCharge> FixedCharges => Set<FixedCharge>();
    public DbSet<PayoutRecord> PayoutRecords => Set<PayoutRecord>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<SiretData> SiretDatas => Set<SiretData>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<CsvMappingProfile> CsvMappingProfiles => Set<CsvMappingProfile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<AccountingPeriodClosing> AccountingPeriodClosings => Set<AccountingPeriodClosing>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Entity ──
        modelBuilder.Entity<Entity>(e =>
        {
            e.Property(r => r.PlafondCA).HasColumnType("decimal(18,2)");
            e.Property(r => r.TauxTVA).HasColumnType("decimal(5,2)");
            e.Property(r => r.TauxLiberatoire).HasColumnType("decimal(5,2)");
            e.Property(r => r.FraisVariables).HasColumnType("decimal(5,2)");
            e.Property(r => r.TypeActivite).HasConversion<string>();
            e.Property(r => r.PeriodiciteDeclaration).HasConversion<string>();
            e.HasOne(r => r.SiretData).WithMany().HasForeignKey(r => r.SiretDataId);
        });

        // ── User ──
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.HasOne(u => u.Entity).WithMany().HasForeignKey(u => u.EntityId);
        });

        // ── Revenue ──
        modelBuilder.Entity<Revenue>(e =>
        {
            e.Property(r => r.Montant).HasColumnType("decimal(18,2)");
            e.Property(r => r.Categorie).HasConversion<string>();
            e.HasIndex(r => r.EntityId);
        });

        // ── Expense ──
        modelBuilder.Entity<Expense>(e =>
        {
            e.Property(r => r.Montant).HasColumnType("decimal(18,2)");
            e.HasIndex(r => r.EntityId);
        });

        // ── Declaration ──
        modelBuilder.Entity<Declaration>(e =>
        {
            e.Property(r => r.MontantCA).HasColumnType("decimal(18,2)");
            e.Property(r => r.MontantCotisations).HasColumnType("decimal(18,2)");
            e.Property(r => r.TauxApplique).HasColumnType("decimal(5,2)");
            e.Property(r => r.Statut).HasConversion<string>();
            e.HasIndex(r => r.EntityId);
        });

        // ── BankTransaction ──
        modelBuilder.Entity<BankTransaction>(e =>
        {
            e.Property(r => r.Montant).HasColumnType("decimal(18,2)");
            e.Property(r => r.Solde).HasColumnType("decimal(18,2)");
            e.HasOne(r => r.Revenue).WithMany().HasForeignKey(r => r.RevenueId);
            e.HasOne(r => r.Expense).WithMany().HasForeignKey(r => r.ExpenseId);
            e.HasIndex(r => r.EntityId);
        });

        // ── FixedCharge ──
        modelBuilder.Entity<FixedCharge>(e =>
        {
            e.Property(r => r.Montant).HasColumnType("decimal(18,2)");
            e.HasIndex(r => r.EntityId);
        });

        // ── PayoutRecord ──
        modelBuilder.Entity<PayoutRecord>(e =>
        {
            e.Property(r => r.MontantBrut).HasColumnType("decimal(18,2)");
            e.Property(r => r.Frais).HasColumnType("decimal(18,2)");
            e.Property(r => r.MontantNet).HasColumnType("decimal(18,2)");
            e.HasOne(r => r.BankTransaction).WithMany().HasForeignKey(r => r.BankTransactionId);
            e.HasIndex(r => new { r.Platform, r.ExternalId }).IsUnique();
            e.HasIndex(r => r.EntityId);
        });

        // ── SiretData ──
        modelBuilder.Entity<SiretData>(e =>
        {
            e.HasIndex(s => s.Siret).IsUnique();
            e.HasIndex(s => s.Siren);
        });

        // ── Client ──
        modelBuilder.Entity<Client>(e =>
        {
            e.HasIndex(c => new { c.EntityId, c.Nom });
            e.HasOne(c => c.SiretData).WithMany().HasForeignKey(c => c.SiretDataId);
        });

        // ── Invoice ──
        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.MontantHT).HasColumnType("decimal(18,2)");
            e.Property(i => i.MontantTVA).HasColumnType("decimal(18,2)");
            e.Property(i => i.MontantTTC).HasColumnType("decimal(18,2)");
            e.Property(i => i.Statut).HasConversion<string>();
            e.Property(i => i.TypeFacture).HasConversion<string>();
            e.HasIndex(i => new { i.EntityId, i.Numero }).IsUnique().HasFilter("Numero != ''");
            e.HasOne(i => i.Client).WithMany().HasForeignKey(i => i.ClientId);
            e.HasOne(i => i.Revenue).WithMany().HasForeignKey(i => i.RevenueId);
        });

        // ── InvoiceLine ──
        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.Property(l => l.Quantite).HasColumnType("decimal(18,4)");
            e.Property(l => l.PrixUnitaire).HasColumnType("decimal(18,2)");
            e.Property(l => l.TauxTVA).HasColumnType("decimal(5,2)");
            e.Property(l => l.MontantHT).HasColumnType("decimal(18,2)");
            e.HasOne(l => l.Invoice).WithMany(i => i.Lignes).HasForeignKey(l => l.InvoiceId);
        });

        // ── Quote ──
        modelBuilder.Entity<Quote>(e =>
        {
            e.Property(q => q.MontantHT).HasColumnType("decimal(18,2)");
            e.Property(q => q.MontantTVA).HasColumnType("decimal(18,2)");
            e.Property(q => q.MontantTTC).HasColumnType("decimal(18,2)");
            e.Property(q => q.Statut).HasConversion<string>();
            e.HasIndex(q => new { q.EntityId, q.Numero }).IsUnique().HasFilter("Numero != ''");
            e.HasOne(q => q.Client).WithMany().HasForeignKey(q => q.ClientId);
            e.HasOne(q => q.Invoice).WithMany().HasForeignKey(q => q.InvoiceId);
        });

        // ── CsvMappingProfile ──
        modelBuilder.Entity<CsvMappingProfile>(e =>
        {
            e.Property(p => p.ProfileType).HasConversion<string>();
            e.HasIndex(p => new { p.EntityId, p.Nom }).IsUnique();
        });

        // ── QuoteLine ──
        modelBuilder.Entity<QuoteLine>(e =>
        {
            e.Property(l => l.Quantite).HasColumnType("decimal(18,4)");
            e.Property(l => l.PrixUnitaire).HasColumnType("decimal(18,2)");
            e.Property(l => l.TauxTVA).HasColumnType("decimal(5,2)");
            e.Property(l => l.MontantHT).HasColumnType("decimal(18,2)");
            e.HasOne(l => l.Quote).WithMany(q => q.Lignes).HasForeignKey(l => l.QuoteId);
        });

        // ── Property ──
        modelBuilder.Entity<Property>(e =>
        {
            e.HasIndex(p => new { p.EntityId, p.Nom }).IsUnique();
        });

        // ── Relations Property (FK nullable sur tous les modèles concernés) ──
        modelBuilder.Entity<Client>(e2 => e2.HasOne(c => c.Property).WithMany().HasForeignKey(c => c.PropertyId));
        modelBuilder.Entity<Invoice>(e2 => e2.HasOne(i => i.Property).WithMany().HasForeignKey(i => i.PropertyId));
        modelBuilder.Entity<Quote>(e2 => e2.HasOne(q => q.Property).WithMany().HasForeignKey(q => q.PropertyId));
        modelBuilder.Entity<Revenue>(e2 => e2.HasOne(r => r.Property).WithMany().HasForeignKey(r => r.PropertyId));
        modelBuilder.Entity<Expense>(e2 => e2.HasOne(x => x.Property).WithMany().HasForeignKey(x => x.PropertyId));
        modelBuilder.Entity<BankTransaction>(e2 => e2.HasOne(b => b.Property).WithMany().HasForeignKey(b => b.PropertyId));
        modelBuilder.Entity<PayoutRecord>(e2 => e2.HasOne(p => p.Property).WithMany().HasForeignKey(p => p.PropertyId));
        modelBuilder.Entity<FixedCharge>(e2 => e2.HasOne(f => f.Property).WithMany().HasForeignKey(f => f.PropertyId));

        // ── AccountingEntry ──
        modelBuilder.Entity<AccountingEntry>(e =>
        {
            e.Property(a => a.Montant).HasColumnType("decimal(18,2)");
            e.Property(a => a.EntryType).HasConversion<string>();
            e.HasIndex(a => new { a.EntityId, a.SequenceNumber }).IsUnique();
            e.HasIndex(a => a.EntityId);
            e.HasOne(a => a.Revenue).WithMany().HasForeignKey(a => a.RevenueId);
            e.HasOne(a => a.Expense).WithMany().HasForeignKey(a => a.ExpenseId);
        });

        // ── AccountingPeriodClosing ──
        modelBuilder.Entity<AccountingPeriodClosing>(e =>
        {
            e.Property(c => c.TotalRecettes).HasColumnType("decimal(18,2)");
            e.Property(c => c.TotalDepenses).HasColumnType("decimal(18,2)");
            e.HasIndex(c => new { c.EntityId, c.PeriodEnd }).IsUnique();
        });

        // ── AuditLog ──
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.EntityId);
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.Action);
        });

        // ── Subscription ──
        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(s => s.Status).HasConversion<string>();
            e.Property(s => s.Plan).HasConversion<string>();
            e.HasIndex(s => s.EntityId).IsUnique();
            e.HasOne(s => s.Entity).WithMany().HasForeignKey(s => s.EntityId);
        });
    }
}
