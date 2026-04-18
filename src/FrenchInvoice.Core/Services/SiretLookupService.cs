using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FrenchInvoice.Core.Data;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public class SiretLookupService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IHttpClientFactory _httpFactory;

    public SiretLookupService(IDbContextFactory<AppDbContext> factory, IHttpClientFactory httpFactory)
    {
        _factory = factory;
        _httpFactory = httpFactory;
    }

    /// <summary>
    /// Cherche un SIRET en base, sinon appelle l'API gouv.fr et stocke le resultat.
    /// </summary>
    public async Task<SiretData?> LookupAsync(string siretOrSiren, bool forceRefresh = false)
    {
        var query = siretOrSiren.Trim().Replace(" ", "");
        if (query.Length < 9) return null;

        using var db = _factory.CreateDbContext();

        // Chercher en cache (par SIRET exact ou par SIREN)
        if (!forceRefresh)
        {
            var cached = await db.SiretDatas
                .FirstOrDefaultAsync(s => s.Siret == query || s.Siren == query);
            if (cached != null) return cached;
        }

        // Appeler l'API
        var data = await FetchFromApiAsync(query);
        if (data == null) return null;

        // Sauvegarder ou mettre a jour en base
        var existing = await db.SiretDatas
            .FirstOrDefaultAsync(s => s.Siret == data.Siret || s.Siren == data.Siren);

        if (existing != null)
        {
            CopyProperties(data, existing);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.SiretDatas.Add(data);
        }

        await db.SaveChangesAsync();
        return existing ?? data;
    }

    private async Task<SiretData?> FetchFromApiAsync(string query)
    {
        var client = _httpFactory.CreateClient();
        var response = await client.GetAsync(
            $"https://recherche-entreprises.api.gouv.fr/search?q={query}");

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json);

        var results = doc.GetProperty("results");
        if (results.GetArrayLength() == 0) return null;

        var entreprise = results[0];
        var siege = entreprise.GetProperty("siege");
        var complements = entreprise.TryGetProperty("complements", out var comp) ? comp : default;

        var data = new SiretData
        {
            ReponseJson = json,

            // Identifiants
            Siren = GetString(entreprise, "siren"),
            Siret = GetString(siege, "siret"),

            // Entreprise
            NomComplet = GetString(entreprise, "nom_complet"),
            NomRaisonSociale = GetStringOrNull(entreprise, "nom_raison_sociale"),
            Sigle = GetStringOrNull(entreprise, "sigle"),
            NatureJuridique = GetStringOrNull(entreprise, "nature_juridique"),
            SectionActivitePrincipale = GetStringOrNull(entreprise, "section_activite_principale"),
            ActivitePrincipale = GetStringOrNull(entreprise, "activite_principale"),
            ActivitePrincipaleNAF25 = GetStringOrNull(entreprise, "activite_principale_naf25"),
            CategorieEntreprise = GetStringOrNull(entreprise, "categorie_entreprise"),
            TrancheEffectifSalarie = GetStringOrNull(entreprise, "tranche_effectif_salarie"),
            EtatAdministratif = GetStringOrNull(entreprise, "etat_administratif"),
            StatutDiffusion = GetStringOrNull(entreprise, "statut_diffusion"),
            NombreEtablissements = GetInt(entreprise, "nombre_etablissements"),
            NombreEtablissementsOuverts = GetInt(entreprise, "nombre_etablissements_ouverts"),
            DateCreationEntreprise = GetDate(entreprise, "date_creation"),
            DateFermetureEntreprise = GetDate(entreprise, "date_fermeture"),

            // Complements
            EstEntrepreneurIndividuel = GetBool(complements, "est_entrepreneur_individuel"),
            EstAssociation = GetBool(complements, "est_association"),
            EstEss = GetBool(complements, "est_ess"),
            EstServicePublic = GetBool(complements, "est_service_public"),
            EstSocieteMission = GetBool(complements, "est_societe_mission"),
            EstQualiopi = GetBool(complements, "est_qualiopi"),
            EstRge = GetBool(complements, "est_rge"),
            EstBio = GetBool(complements, "est_bio"),
            IdentifiantAssociation = GetStringOrNull(complements, "identifiant_association"),

            // Siege
            NomCommercial = GetStringOrNull(siege, "nom_commercial"),
            Enseigne = siege.TryGetProperty("liste_enseignes", out var ens) && ens.GetArrayLength() > 0
                ? ens[0].GetString() : null,
            Adresse = GetStringOrNull(siege, "adresse"),
            NumeroVoie = GetStringOrNull(siege, "numero_voie"),
            TypeVoie = GetStringOrNull(siege, "type_voie"),
            LibelleVoie = GetStringOrNull(siege, "libelle_voie"),
            ComplementAdresse = GetStringOrNull(siege, "complement_adresse"),
            CodePostal = GetStringOrNull(siege, "code_postal"),
            LibelleCommune = GetStringOrNull(siege, "libelle_commune"),
            Commune = GetStringOrNull(siege, "commune"),
            Cedex = GetStringOrNull(siege, "cedex"),
            LibelleCedex = GetStringOrNull(siege, "libelle_cedex"),
            Departement = GetStringOrNull(siege, "departement"),
            Region = GetStringOrNull(siege, "region"),
            Epci = GetStringOrNull(siege, "epci"),
            CodePaysEtranger = GetStringOrNull(siege, "code_pays_etranger"),
            LibellePaysEtranger = GetStringOrNull(siege, "libelle_pays_etranger"),
            LibelleCommuneEtranger = GetStringOrNull(siege, "libelle_commune_etranger"),
            DistributionSpeciale = GetStringOrNull(siege, "distribution_speciale"),
            IndiceRepetition = GetStringOrNull(siege, "indice_repetition"),
            EstSiege = GetBool(siege, "est_siege"),
            CaractereEmployeur = GetStringOrNull(siege, "caractere_employeur"),
            ActivitePrincipaleSiege = GetStringOrNull(siege, "activite_principale"),
            ActivitePrincipaleNAF25Siege = GetStringOrNull(siege, "activite_principale_naf25"),
            TrancheEffectifSalarieSiege = GetStringOrNull(siege, "tranche_effectif_salarie"),
            DateCreationSiege = GetDate(siege, "date_creation"),
            DateDebutActivite = GetDate(siege, "date_debut_activite"),
            DateFermetureSiege = GetDate(siege, "date_fermeture"),

            // Coordonnees
            Latitude = GetStringOrNull(siege, "latitude"),
            Longitude = GetStringOrNull(siege, "longitude"),

            // Dirigeants
            DirigeantsJson = entreprise.TryGetProperty("dirigeants", out var dir)
                ? dir.GetRawText() : null,

            // IDCC
            ListeIdccJson = complements.ValueKind != JsonValueKind.Undefined
                && complements.TryGetProperty("liste_idcc", out var idcc)
                ? idcc.GetRawText() : null,

            // Dates de mise a jour
            DateMiseAJourInsee = GetDateTime(entreprise, "date_mise_a_jour_insee"),
            DateMiseAJourRne = GetDateTime(entreprise, "date_mise_a_jour_rne"),
        };

        return data;
    }

    private static void CopyProperties(SiretData source, SiretData target)
    {
        target.ReponseJson = source.ReponseJson;
        target.Siren = source.Siren;
        target.Siret = source.Siret;
        target.NomComplet = source.NomComplet;
        target.NomRaisonSociale = source.NomRaisonSociale;
        target.Sigle = source.Sigle;
        target.NatureJuridique = source.NatureJuridique;
        target.SectionActivitePrincipale = source.SectionActivitePrincipale;
        target.ActivitePrincipale = source.ActivitePrincipale;
        target.ActivitePrincipaleNAF25 = source.ActivitePrincipaleNAF25;
        target.CategorieEntreprise = source.CategorieEntreprise;
        target.TrancheEffectifSalarie = source.TrancheEffectifSalarie;
        target.EtatAdministratif = source.EtatAdministratif;
        target.StatutDiffusion = source.StatutDiffusion;
        target.NombreEtablissements = source.NombreEtablissements;
        target.NombreEtablissementsOuverts = source.NombreEtablissementsOuverts;
        target.DateCreationEntreprise = source.DateCreationEntreprise;
        target.DateFermetureEntreprise = source.DateFermetureEntreprise;
        target.EstEntrepreneurIndividuel = source.EstEntrepreneurIndividuel;
        target.EstAssociation = source.EstAssociation;
        target.EstEss = source.EstEss;
        target.EstServicePublic = source.EstServicePublic;
        target.EstSocieteMission = source.EstSocieteMission;
        target.EstQualiopi = source.EstQualiopi;
        target.EstRge = source.EstRge;
        target.EstBio = source.EstBio;
        target.IdentifiantAssociation = source.IdentifiantAssociation;
        target.NomCommercial = source.NomCommercial;
        target.Enseigne = source.Enseigne;
        target.Adresse = source.Adresse;
        target.NumeroVoie = source.NumeroVoie;
        target.TypeVoie = source.TypeVoie;
        target.LibelleVoie = source.LibelleVoie;
        target.ComplementAdresse = source.ComplementAdresse;
        target.CodePostal = source.CodePostal;
        target.LibelleCommune = source.LibelleCommune;
        target.Commune = source.Commune;
        target.Cedex = source.Cedex;
        target.LibelleCedex = source.LibelleCedex;
        target.Departement = source.Departement;
        target.Region = source.Region;
        target.Epci = source.Epci;
        target.CodePaysEtranger = source.CodePaysEtranger;
        target.LibellePaysEtranger = source.LibellePaysEtranger;
        target.LibelleCommuneEtranger = source.LibelleCommuneEtranger;
        target.DistributionSpeciale = source.DistributionSpeciale;
        target.IndiceRepetition = source.IndiceRepetition;
        target.EstSiege = source.EstSiege;
        target.CaractereEmployeur = source.CaractereEmployeur;
        target.ActivitePrincipaleSiege = source.ActivitePrincipaleSiege;
        target.ActivitePrincipaleNAF25Siege = source.ActivitePrincipaleNAF25Siege;
        target.TrancheEffectifSalarieSiege = source.TrancheEffectifSalarieSiege;
        target.DateCreationSiege = source.DateCreationSiege;
        target.DateDebutActivite = source.DateDebutActivite;
        target.DateFermetureSiege = source.DateFermetureSiege;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
        target.DirigeantsJson = source.DirigeantsJson;
        target.ListeIdccJson = source.ListeIdccJson;
        target.DateMiseAJourInsee = source.DateMiseAJourInsee;
        target.DateMiseAJourRne = source.DateMiseAJourRne;
    }

    private static string GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static string? GetStringOrNull(JsonElement el, string prop)
        => el.ValueKind != JsonValueKind.Undefined
            && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static int GetInt(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32() : 0;

    private static bool GetBool(JsonElement el, string prop)
        => el.ValueKind != JsonValueKind.Undefined
            && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;

    private static DateTime? GetDate(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
        {
            var str = v.GetString();
            if (DateTime.TryParse(str, out var dt)) return dt;
        }
        return null;
    }

    private static DateTime? GetDateTime(JsonElement el, string prop)
        => GetDate(el, prop);
}
