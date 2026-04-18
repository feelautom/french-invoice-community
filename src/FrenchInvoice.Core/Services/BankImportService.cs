using System.Globalization;
using FrenchInvoice.Core.Models;

namespace FrenchInvoice.Core.Services;

public interface IBankCsvParser
{
    string BankName { get; }
    List<BankTransaction> Parse(string csvContent);
}

public class BoursobankParser : IBankCsvParser
{
    public string BankName => "Boursobank";

    public List<BankTransaction> Parse(string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))
        {
            var cols = ParseCsvLine(line.TrimEnd('\r'));
            if (cols.Length < 7) continue;

            var dateStr = cols[0].Trim();
            var label = cols[2].Trim();
            var amountStr = cols[6].Trim().Replace(",", ".").Replace(" ", "");
            var balanceStr = cols.Length > 10 ? cols[10].Trim().Replace(",", ".").Replace(" ", "") : null;

            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                decimal.TryParse(amountStr, CultureInfo.InvariantCulture, out var montant))
            {
                transactions.Add(new BankTransaction
                {
                    Date = date,
                    Libelle = label,
                    Montant = montant,
                    Solde = decimal.TryParse(balanceStr, CultureInfo.InvariantCulture, out var s) ? s : null
                });
            }
        }
        return transactions;
    }

    internal static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ';' && !inQuotes) { result.Add(current.ToString()); current.Clear(); continue; }
            current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}

public class BnpParibasParser : IBankCsvParser
{
    public string BankName => "BNP Paribas";

    public List<BankTransaction> Parse(string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split(';');
            if (cols.Length < 3) continue;

            if (DateTime.TryParse(cols[0].Trim(), CultureInfo.GetCultureInfo("fr-FR"), out var date) &&
                decimal.TryParse(cols[2].Trim().Replace(",", ".").Replace(" ", ""), CultureInfo.InvariantCulture, out var montant))
            {
                transactions.Add(new BankTransaction
                {
                    Date = date,
                    Libelle = cols[1].Trim(),
                    Montant = montant
                });
            }
        }
        return transactions;
    }
}

public class CreditMutuelParser : IBankCsvParser
{
    public string BankName => "Crédit Mutuel / CIC";

    public List<BankTransaction> Parse(string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split(';');
            if (cols.Length < 4) continue;

            if (DateTime.TryParse(cols[0].Trim(), CultureInfo.GetCultureInfo("fr-FR"), out var date))
            {
                var debitStr = cols[2].Trim().Replace(",", ".").Replace(" ", "");
                var creditStr = cols[3].Trim().Replace(",", ".").Replace(" ", "");

                decimal montant = 0;
                if (decimal.TryParse(creditStr, CultureInfo.InvariantCulture, out var credit) && credit != 0)
                    montant = credit;
                else if (decimal.TryParse(debitStr, CultureInfo.InvariantCulture, out var debit))
                    montant = -Math.Abs(debit);

                transactions.Add(new BankTransaction
                {
                    Date = date,
                    Libelle = cols[1].Trim(),
                    Montant = montant
                });
            }
        }
        return transactions;
    }
}

public class GenericCsvParser : IBankCsvParser
{
    public string BankName => "Générique";
    public int DateColumn { get; set; }
    public int LibelleColumn { get; set; } = 1;
    public int MontantColumn { get; set; } = 2;
    public char Separator { get; set; } = ';';

    public List<BankTransaction> Parse(string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split(Separator);
            if (cols.Length <= Math.Max(DateColumn, Math.Max(LibelleColumn, MontantColumn))) continue;

            if (DateTime.TryParse(cols[DateColumn].Trim(), CultureInfo.GetCultureInfo("fr-FR"), out var date) &&
                decimal.TryParse(cols[MontantColumn].Trim().Replace(",", ".").Replace(" ", ""), CultureInfo.InvariantCulture, out var montant))
            {
                transactions.Add(new BankTransaction
                {
                    Date = date,
                    Libelle = cols[LibelleColumn].Trim(),
                    Montant = montant
                });
            }
        }
        return transactions;
    }
}

public class BankImportService
{
    private readonly Dictionary<string, IBankCsvParser> _parsers;

    public BankImportService()
    {
        _parsers = new Dictionary<string, IBankCsvParser>(StringComparer.OrdinalIgnoreCase)
        {
            ["Boursobank"] = new BoursobankParser(),
            ["BNP Paribas"] = new BnpParibasParser(),
            ["Crédit Mutuel / CIC"] = new CreditMutuelParser(),
            ["Générique"] = new GenericCsvParser()
        };
    }

    public IReadOnlyList<string> AvailableBanks => _parsers.Keys.ToList();

    public List<BankTransaction> ParseCsv(string bankName, string csvContent)
    {
        if (!_parsers.TryGetValue(bankName, out var parser))
            throw new ArgumentException($"Parser non trouvé pour la banque : {bankName}");

        return parser.Parse(csvContent);
    }

    public List<BankTransaction> ParseWithProfile(CsvMappingProfile profile, string csvContent)
    {
        var transactions = new List<BankTransaction>();
        var separator = string.IsNullOrEmpty(profile.Separator) ? ";" : profile.Separator;
        var sepChar = separator == "\\t" ? '\t' : separator[0];

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var startLine = profile.HeaderRow > 0 ? profile.HeaderRow : 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            var rawLine = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(rawLine)) continue;

            var cols = ParseLine(rawLine, sepChar);

            var dateStr = GetColumn(cols, profile.DateColumn);
            if (dateStr == null) continue;

            DateTime date;
            if (!string.IsNullOrEmpty(profile.DateFormat))
            {
                if (!DateTime.TryParseExact(dateStr, profile.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    continue;
            }
            else
            {
                if (!DateTime.TryParse(dateStr, CultureInfo.GetCultureInfo("fr-FR"), out date))
                    continue;
            }

            var libelle = GetColumn(cols, profile.LibelleColumn) ?? "";

            decimal montant;
            if (profile.MontantColumn.HasValue)
            {
                var montantStr = NormalizeDecimal(GetColumn(cols, profile.MontantColumn.Value));
                if (!decimal.TryParse(montantStr, CultureInfo.InvariantCulture, out montant))
                    continue;
            }
            else if (profile.DebitColumn.HasValue && profile.CreditColumn.HasValue)
            {
                var creditStr = NormalizeDecimal(GetColumn(cols, profile.CreditColumn.Value));
                var debitStr = NormalizeDecimal(GetColumn(cols, profile.DebitColumn.Value));

                montant = 0;
                if (decimal.TryParse(creditStr, CultureInfo.InvariantCulture, out var credit) && credit != 0)
                    montant = credit;
                else if (decimal.TryParse(debitStr, CultureInfo.InvariantCulture, out var debit))
                    montant = -Math.Abs(debit);
                else
                    continue;
            }
            else
            {
                continue;
            }

            decimal? solde = null;
            if (profile.SoldeColumn.HasValue)
            {
                var soldeStr = NormalizeDecimal(GetColumn(cols, profile.SoldeColumn.Value));
                if (decimal.TryParse(soldeStr, CultureInfo.InvariantCulture, out var s))
                    solde = s;
            }

            transactions.Add(new BankTransaction
            {
                Date = date,
                Libelle = libelle.Trim(),
                Montant = montant,
                Solde = solde
            });
        }

        return transactions;
    }

    public List<(Revenue Revenue, decimal Frais)> ParseRevenuesWithProfile(CsvMappingProfile profile, string csvContent, int entityId, ActivityCategory categorie)
    {
        var results = new List<(Revenue, decimal)>();
        var separator = string.IsNullOrEmpty(profile.Separator) ? ";" : profile.Separator;
        var sepChar = separator == "\\t" ? '\t' : separator[0];

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var startLine = profile.HeaderRow > 0 ? profile.HeaderRow : 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            var rawLine = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(rawLine)) continue;

            var cols = ParseLine(rawLine, sepChar);

            var dateStr = GetColumn(cols, profile.DateColumn);
            if (dateStr == null) continue;

            DateTime date;
            if (!string.IsNullOrEmpty(profile.DateFormat))
            {
                if (!DateTime.TryParseExact(dateStr, profile.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    continue;
            }
            else
            {
                if (!DateTime.TryParse(dateStr, CultureInfo.GetCultureInfo("fr-FR"), out date))
                    continue;
            }

            var description = GetColumn(cols, profile.LibelleColumn) ?? "";

            decimal montant;
            if (profile.MontantColumn.HasValue)
            {
                var montantStr = NormalizeDecimal(GetColumn(cols, profile.MontantColumn.Value));
                if (!decimal.TryParse(montantStr, CultureInfo.InvariantCulture, out montant))
                    continue;
            }
            else if (profile.CreditColumn.HasValue)
            {
                var creditStr = NormalizeDecimal(GetColumn(cols, profile.CreditColumn.Value));
                if (!decimal.TryParse(creditStr, CultureInfo.InvariantCulture, out montant) || montant <= 0)
                    continue;
            }
            else
            {
                continue;
            }

            var client = profile.ClientColumn.HasValue
                ? GetColumn(cols, profile.ClientColumn.Value) ?? ""
                : "";

            var modePaiement = profile.ModePaiementColumn.HasValue
                ? GetColumn(cols, profile.ModePaiementColumn.Value) ?? "En ligne"
                : "En ligne";

            var reference = profile.ReferenceColumn.HasValue
                ? GetColumn(cols, profile.ReferenceColumn.Value)
                : null;
            if (reference != null)
                reference = $"CSV:{profile.Nom}:{reference}";

            decimal frais = 0;
            if (profile.FraisColumn.HasValue)
            {
                var fraisStr = NormalizeDecimal(GetColumn(cols, profile.FraisColumn.Value));
                if (decimal.TryParse(fraisStr, CultureInfo.InvariantCulture, out var f))
                    frais = Math.Abs(f);
            }

            results.Add((new Revenue
            {
                EntityId = entityId,
                Date = date,
                Montant = Math.Abs(montant),
                Description = description.Trim(),
                Client = client.Trim(),
                ModePaiement = modePaiement.Trim(),
                Categorie = categorie,
                ReferenceFacture = reference,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, frais));
        }

        return results;
    }

    public List<string[]> PreviewCsv(string csvContent, string separator, int headerRow)
    {
        var sepChar = separator == "\\t" ? '\t' : (string.IsNullOrEmpty(separator) ? ';' : separator[0]);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string[]>();

        var startLine = headerRow > 0 ? headerRow : 0;
        for (int i = startLine; i < lines.Length && result.Count < 5; i++)
        {
            var rawLine = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            result.Add(ParseLine(rawLine, sepChar));
        }

        return result;
    }

    public string[]? GetHeaders(string csvContent, string separator, int headerRow)
    {
        if (headerRow <= 0) return null;
        var sepChar = separator == "\\t" ? '\t' : (string.IsNullOrEmpty(separator) ? ';' : separator[0]);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < headerRow) return null;
        return ParseLine(lines[headerRow - 1].TrimEnd('\r'), sepChar);
    }

    public static string DetectSeparator(string csvContent)
    {
        var firstLines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3).ToArray();
        if (firstLines.Length == 0) return ";";

        var candidates = new[] { ";", ",", "\t", "|" };
        var best = ";";
        int bestScore = 0;

        foreach (var sep in candidates)
        {
            var sepChar = sep == "\t" ? '\t' : sep[0];
            var counts = firstLines.Select(l => l.Split(sepChar).Length).ToArray();
            if (counts.All(c => c == counts[0]) && counts[0] > 1 && counts[0] > bestScore)
            {
                bestScore = counts[0];
                best = sep;
            }
        }

        return best;
    }

    private static string[] ParseLine(string line, char separator)
    {
        if (separator == ';')
            return BoursobankParser.ParseCsvLine(line);

        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == separator && !inQuotes) { result.Add(current.ToString()); current.Clear(); continue; }
            current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string? GetColumn(string[] cols, int index)
    {
        if (index < 0 || index >= cols.Length) return null;
        var val = cols[index].Trim();
        return string.IsNullOrEmpty(val) ? null : val;
    }

    private static string? NormalizeDecimal(string? value)
    {
        if (value == null) return null;
        return value.Replace(",", ".").Replace(" ", "").Replace("\u00A0", "");
    }

    public static List<CsvMappingProfile> CreateSystemProfiles(int entityId) =>
    [
        new CsvMappingProfile
        {
            EntityId = entityId,
            Nom = "Boursobank",
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "yyyy-MM-dd",
            DateColumn = 0,
            LibelleColumn = 2,
            MontantColumn = 6,
            SoldeColumn = 10,
            IsSystem = true
        },
        new CsvMappingProfile
        {
            EntityId = entityId,
            Nom = "BNP Paribas",
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2,
            IsSystem = true
        },
        new CsvMappingProfile
        {
            EntityId = entityId,
            Nom = "Crédit Mutuel / CIC",
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "",
            DateColumn = 0,
            LibelleColumn = 1,
            DebitColumn = 2,
            CreditColumn = 3,
            MontantColumn = null,
            IsSystem = true
        },
        new CsvMappingProfile
        {
            EntityId = entityId,
            Nom = "Générique",
            Separator = ";",
            HeaderRow = 1,
            DateFormat = "",
            DateColumn = 0,
            LibelleColumn = 1,
            MontantColumn = 2,
            IsSystem = true
        }
    ];
}
