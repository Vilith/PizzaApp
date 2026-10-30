// Hämta och extrahera originalmenyn utan att köra extern HTML eller JavaScript.

using System.Net;
using System.Text.RegularExpressions;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

// Hämtning: endast Sperrings fasta menyadress används, aldrig en adress från användaren.
public class SperringWebsiteMenuService(HttpClient http)
{
    public async Task<SperringWebsiteMenu> GetAsync(CancellationToken cancellationToken)
    {
        var html = await http.GetStringAsync(SperringWebsiteMenu.SourceUrl, cancellationToken);
        return new SperringWebsiteMenu(Parse(html));
    }

    // Extrahering: läs Wix synliga textblock och behåll veckomeny, sallader och tillbehör.
    // Returnera vanlig text: externa skript, stilar och HTML körs aldrig i vår sida.
    public static string[] Parse(string html)
    {
        var blocks = Regex.Matches(html,
            "<div\\b[^>]*data-testid=[\"']richTextElement[\"'][^>]*>(.*?)</div>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        var lines = new List<string>();

        foreach (Match block in blocks)
        {
            var text = Regex.Replace(block.Groups[1].Value,
                @"<(script|style)\b[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(2));
            text = Regex.Replace(text, @"<br\b[^>]*>|</(?:h[1-6]|p|li)>", "\n",
                RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
            text = Regex.Replace(text, "<[^>]*>", "", RegexOptions.None, TimeSpan.FromSeconds(2));
            text = WebUtility.HtmlDecode(text).Replace("\u200b", "").Replace('\u00a0', ' ');

            if (!Regex.IsMatch(text, @"Vecka\s+\d|Veckans\s+sallader|Vegetariska alternativ",
                    RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2))) continue;

            lines.AddRange(text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        // Om källans struktur ändras ska ett fel visas i stället för en tom eller felaktig meny.
        if (!lines.Any(line => Regex.IsMatch(line, @"^Vecka\s+\d", RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(2))))
            throw new InvalidDataException("Kunde inte läsa veckomenyn från Sperrings hemsida.");

        return lines.ToArray();
    }
}
