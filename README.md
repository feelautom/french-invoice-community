# FrenchInvoice Community Edition

Outil de gestion comptable pour **auto-entrepreneurs** et **micro-entreprises** en France.

Factures Factur-X, devis, comptabilite, declarations URSSAF -- tout-en-un, auto-heberge.

## Fonctionnalites

- **Factures Factur-X** : PDF conformes avec XML embarque (ZUGFeRD v2.3, profil Comfort)
- **Devis** : creation, envoi, expiration automatique, conversion en facture
- **Comptabilite** : suivi CA, cotisations sociales, charges, benefice net
- **Declarations URSSAF** : generation mensuelle/trimestrielle avec alertes echeances
- **Import bancaire** : CSV multi-banques (Boursobank, BNP, Credit Mutuel, generique)
- **Export/Import** : sauvegarde ZIP chiffree avec integrite SHA-256
- **Livre des recettes** et **Registre des achats** en PDF
- **API REST** avec documentation Swagger
- **ACRE** : calcul automatique des cotisations reduites

## Stack technique

- [.NET 9](https://dotnet.microsoft.com/) -- Blazor Server
- [SQLite](https://www.sqlite.org/) + Entity Framework Core
- [MudBlazor](https://mudblazor.com/) -- UI Material Design
- [QuestPDF](https://www.questpdf.com/) -- generation PDF
- [ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) -- XML Factur-X

## Demarrage rapide

### Avec Docker (recommande)

```bash
docker-compose up --build -d
```

L'application est accessible sur [http://localhost:5000](http://localhost:5000).

### Sans Docker

```bash
dotnet run --project src/FrenchInvoice.Community
```

### Tests

```bash
dotnet test
```

## Taux de cotisations (2026)

| Categorie | Taux |
|-----------|------|
| BIC -- Vente de marchandises | 12.3% |
| BIC -- Prestations de services | 21.2% |
| BNC -- Liberal | 21.1% |

## Conformite

- Numerotation sequentielle sans trou (Art. L441-9 Code de Commerce)
- Factur-X EN 16931 (profil Comfort, ZUGFeRD v2.3)
- Mentions legales obligatoires sur PDF (TVA art. 293B, penalites de retard, indemnite 40 EUR)
- Cadre de facturation (BT-23) pour la reforme e-invoicing sept. 2026

## Licence

[Elastic License 2.0 (ELv2)](LICENSE)
