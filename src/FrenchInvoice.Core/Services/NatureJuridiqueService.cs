using System.Text.Json;

namespace FrenchInvoice.Core.Services;

public class NatureJuridiqueService
{
    private readonly Dictionary<string, string> _codes;

    public NatureJuridiqueService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "categories_juridiques.json");
        if (!File.Exists(path))
            path = Path.Combine(env.ContentRootPath, "Data", "categories_juridiques.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            _codes = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        else
        {
            _codes = new();
        }
    }

    public string GetLabel(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        return _codes.TryGetValue(code, out var label) ? label : code;
    }
}
