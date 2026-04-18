namespace FrenchInvoice.Core.Models;

public class SiretData
{
    public int Id { get; set; }

    // Identifiants
    public string Siren { get; set; } = "";
    public string Siret { get; set; } = "";

    // Entreprise (unité légale)
    public string NomComplet { get; set; } = "";
    public string? NomRaisonSociale { get; set; }
    public string? Sigle { get; set; }
    public string? NatureJuridique { get; set; }
    public string? SectionActivitePrincipale { get; set; }
    public string? ActivitePrincipale { get; set; }
    public string? ActivitePrincipaleNAF25 { get; set; }
    public string? CategorieEntreprise { get; set; }
    public string? TrancheEffectifSalarie { get; set; }
    public string? EtatAdministratif { get; set; }
    public string? StatutDiffusion { get; set; }
    public bool EstEntrepreneurIndividuel { get; set; }
    public bool EstAssociation { get; set; }
    public bool EstEss { get; set; }
    public bool EstServicePublic { get; set; }
    public bool EstSocieteMission { get; set; }
    public bool EstQualiopi { get; set; }
    public bool EstRge { get; set; }
    public bool EstBio { get; set; }
    public string? IdentifiantAssociation { get; set; }
    public int NombreEtablissements { get; set; }
    public int NombreEtablissementsOuverts { get; set; }
    public DateTime? DateCreationEntreprise { get; set; }
    public DateTime? DateFermetureEntreprise { get; set; }

    // Siège (établissement principal)
    public string? NomCommercial { get; set; }
    public string? Enseigne { get; set; }
    public string? Adresse { get; set; }
    public string? NumeroVoie { get; set; }
    public string? TypeVoie { get; set; }
    public string? LibelleVoie { get; set; }
    public string? ComplementAdresse { get; set; }
    public string? CodePostal { get; set; }
    public string? LibelleCommune { get; set; }
    public string? Commune { get; set; }
    public string? Cedex { get; set; }
    public string? LibelleCedex { get; set; }
    public string? Departement { get; set; }
    public string? Region { get; set; }
    public string? Epci { get; set; }
    public string? CodePaysEtranger { get; set; }
    public string? LibellePaysEtranger { get; set; }
    public string? LibelleCommuneEtranger { get; set; }
    public string? DistributionSpeciale { get; set; }
    public string? IndiceRepetition { get; set; }
    public bool EstSiege { get; set; }
    public string? CaractereEmployeur { get; set; }
    public string? ActivitePrincipaleSiege { get; set; }
    public string? ActivitePrincipaleNAF25Siege { get; set; }
    public string? TrancheEffectifSalarieSiege { get; set; }
    public DateTime? DateCreationSiege { get; set; }
    public DateTime? DateDebutActivite { get; set; }
    public DateTime? DateFermetureSiege { get; set; }

    // Coordonnées
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    // Dirigeants (JSON sérialisé)
    public string? DirigeantsJson { get; set; }

    // Conventions collectives (JSON sérialisé)
    public string? ListeIdccJson { get; set; }

    // Réponse brute complète
    public string ReponseJson { get; set; } = "";

    // Métadonnées
    public DateTime? DateMiseAJourInsee { get; set; }
    public DateTime? DateMiseAJourRne { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
