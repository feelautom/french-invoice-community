# FrenchInvoice Community Edition

[![Build & Tests](https://github.com/feelautom/french-invoice-community/actions/workflows/build.yml/badge.svg)](https://github.com/feelautom/french-invoice-community/actions/workflows/build.yml)
[![CodeFactor](https://www.codefactor.io/repository/github/feelautom/french-invoice-community/badge)](https://www.codefactor.io/repository/github/feelautom/french-invoice-community)
[![License: ELv2](https://img.shields.io/badge/License-ELv2-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Factur-X](https://img.shields.io/badge/Factur--X-EN%2016931-green)](https://fnfe-mpe.org/factur-x/)

![FrenchInvoice](screenshot.png)

Outil de gestion comptable complet pour **auto-entrepreneurs** et **micro-entreprises** en France.

Factures Factur-X, devis, comptabilite, declarations URSSAF — tout-en-un, auto-heberge, gratuit.

## Fonctionnalites

- **Factures Factur-X** — PDF conformes avec XML embarque (ZUGFeRD v2.3, profil Comfort)
- **Devis** — creation, envoi, expiration automatique, conversion en facture
- **Comptabilite** — suivi CA, cotisations sociales, charges fixes/variables, benefice net
- **Declarations URSSAF** — generation mensuelle ou trimestrielle avec alertes echeances
- **Dashboard** — tresorerie, progression vers le plafond, prochaine echeance URSSAF
- **Import bancaire** — CSV multi-banques (Boursobank, BNP, Credit Mutuel, generique)
- **Clients** — gestion complete avec recherche SIRET automatique
- **Export/Import** — sauvegarde ZIP avec integrite SHA-256
- **Livre des recettes** et **Registre des achats** en PDF
- **ACRE** — calcul automatique des cotisations reduites la premiere annee
- **Mode sombre** — persistant entre les sessions

## Installation

### Prerequis

- [Docker](https://docs.docker.com/get-docker/) et [Docker Compose](https://docs.docker.com/compose/install/)

C'est tout. Pas besoin d'installer .NET, pas de base de donnees a configurer.

### 1. Cloner le depot

```bash
git clone https://github.com/FrenchInvoice/frenchinvoice-community.git
cd frenchinvoice-community
```

### 2. Lancer

```bash
docker-compose up -d
```

### 3. Ouvrir

Rendez-vous sur [http://localhost:5555](http://localhost:5555).

Au premier lancement, un assistant de configuration vous guidera :

1. Entrez votre **numero SIRET** — les informations de votre entreprise sont remplies automatiquement depuis l'API gouvernementale
2. Completez les champs manquants (telephone, TVA, type d'activite)
3. Validez — vous etes pret a facturer

### Changer le port

Par defaut, l'application ecoute sur le port **5555**. Pour changer, modifiez `docker-compose.yml` :

```yaml
ports:
  - "8080:8080"  # remplacez 5555 par le port souhaite
```

### Mise a jour

```bash
git pull
docker-compose up --build -d
```

Les migrations de base de donnees s'appliquent automatiquement. Vos donnees sont preservees dans le volume Docker `community-data`.

### Sauvegarde

Vos donnees (base SQLite + PDFs) sont dans le volume Docker `community-data`. Pour sauvegarder :

```bash
# Exporter depuis l'interface
# Menu lateral > Export/Import > Exporter (ZIP avec integrite SHA-256)

# Ou copier le volume directement
docker cp frenchinvoice-community:/app/Data ./backup
```

## Installation sans Docker

Si vous preferez ne pas utiliser Docker :

```bash
# Prerequis : .NET 9 SDK
# https://dotnet.microsoft.com/download/dotnet/9.0

dotnet run --project src/FrenchInvoice.Community
```

L'application demarre sur `http://localhost:5000`.

## Stack technique

| Composant | Technologie |
|-----------|-------------|
| Framework | [.NET 9](https://dotnet.microsoft.com/) — Blazor Server |
| Base de donnees | [SQLite](https://www.sqlite.org/) + Entity Framework Core |
| Interface | [MudBlazor](https://mudblazor.com/) — Material Design |
| Generation PDF | [QuestPDF](https://www.questpdf.com/) |
| Factur-X | [ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) — XML EN 16931 |

## Conformite

FrenchInvoice genere des factures conformes a la legislation francaise :

- **Numerotation sequentielle** sans trou (Art. L441-9 Code de Commerce)
- **Factur-X** EN 16931 (profil Comfort, ZUGFeRD v2.3)
- **Mentions legales obligatoires** : TVA art. 293B, penalites de retard, indemnite 40 EUR
- **Cadre de facturation** BT-23 pour la reforme e-invoicing (sept. 2026)

## Taux de cotisations auto-entrepreneur (2026)

| Categorie | Taux |
|-----------|------|
| BIC — Vente de marchandises | 12.3% |
| BIC — Prestations de services | 21.2% |
| BNC — Liberal | 21.1% |

## FrenchInvoice SaaS

Cette edition Community est gratuite et auto-hebergee.

Une version **SaaS** hebergee est egalement disponible sur [frenchinvoice.fr](https://frenchinvoice.fr) avec des fonctionnalites supplementaires :

- API REST pour integrer vos outils
- Import automatique des clients depuis votre site web
- Synchronisation Stancer/Stripe
- Hebergement et sauvegardes gerees pour vous

## Tests

```bash
dotnet test
```

## Licence

[Elastic License 2.0 (ELv2)](LICENSE)
