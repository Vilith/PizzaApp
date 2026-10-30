using PizzaApp.Api.Services;

namespace PizzaApp.Tests;

// Extraheringen ska behålla hela menytexten men utesluta navigation och exekverbar HTML.
public class SperringWebsiteMenuTests
{
    // Textblock kan innehålla olika rubriknivåer, radbrytningar och HTML-entiteter.
    [Fact]
    public void Reads_both_weeks_salads_and_regular_dishes_as_plain_text()
    {
        var html = """
            <div data-testid="richTextElement"><h1>Kontakt</h1><p>Navigation</p></div>
            <div data-testid="richTextElement"><h1>Vecka 45</h1><h1>&nbsp;</h1>
            <h1>M&aring;ndag:</h1><h1>Ris &amp; gr&ouml;nsaker</h1>
            <h1>Vecka 46</h1><h1>Ny lunch</h1><script>danger()</script></div>
            <div data-testid='richTextElement'><h2>Veckans Sallader<br>Måndag: Sallad<br>
            Pastarätter:<br>Pasta</h2></div>
            <div data-testid="richTextElement"><p>Vegetariska alternativ finns.</p></div>
            """;

        var lines = SperringWebsiteMenuService.Parse(html);

        Assert.Equal(new[] { "Vecka 45", "Måndag:", "Ris & grönsaker", "Vecka 46", "Ny lunch",
            "Veckans Sallader", "Måndag: Sallad", "Pastarätter:", "Pasta", "Vegetariska alternativ finns." }, lines);
        Assert.DoesNotContain(lines, line => line.Contains('<') || line.Contains("danger"));
    }

    // En ändrad webbstruktur får inte ge sken av att en tom meny är korrekt inläst.
    [Fact]
    public void Rejects_missing_week_menu()
    {
        Assert.Throws<InvalidDataException>(() => SperringWebsiteMenuService.Parse(
            "<div data-testid=\"richTextElement\"><p>Kontakt</p></div>"));
    }
}
