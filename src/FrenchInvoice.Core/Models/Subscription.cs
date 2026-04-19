namespace FrenchInvoice.Core.Models;

public enum SubscriptionStatus
{
    Pending,    // premier paiement en attente
    Active,     // abonnement actif et à jour
    PastDue,    // échec de renouvellement, période de grâce
    Cancelled   // annulé
}

public enum SubscriptionPlan
{
    Monthly
}

public class Subscription
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public Entity Entity { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Monthly;

    /// <summary>Montant en centimes (ex: 990 = 9.90 €)</summary>
    public int AmountCents { get; set; }

    /// <summary>Token CBR Stancer — card.id issu du premier paiement tokenisé</summary>
    public string? StancerCardToken { get; set; }

    /// <summary>ID du dernier paiement Stancer créé (initial ou renouvellement)</summary>
    public string? LastStancerPaymentId { get; set; }

    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Nombre d'échecs consécutifs pour le renouvellement en cours</summary>
    public int FailedAttemptsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
