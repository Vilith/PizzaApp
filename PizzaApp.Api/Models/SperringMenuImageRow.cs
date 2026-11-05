// Den aktuella menybilden ersätts på samma rad vid nästa uppladdning.
namespace PizzaApp.Api.Models;
public class SperringMenuImageRow
{
    public int Id { get; set; }
    public string DataUrl { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}
