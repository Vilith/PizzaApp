# PizzaApp

MAUI/Blazor-app med två restauranger och en separat gemensam beställningslista per restaurang och dag.

## Flöde

- Välj Pizzeria (Kvänum Pizzeria) eller À la carte (Sperring).
- Pizzeria: välj pizza, sås (inklusive Ingen sås), dryck och antal.
- À la carte: välj rätt och antal, utan sås- eller dryckesval.
- Namn och kommentar är valfria. Spara direkt i restaurangens dagslista.
- Listan visar varje beställning samt en sammanställning för den som ska ringa. Identiska rätter och tillval summeras; olika kommentarer hålls isär.
- Ändra en beställning med Ändra och spara formuläret. Borttagning kräver bekräftelse.
- Uppdatera listan för att hämta andras senaste beställningar. Tidpunkten för senaste hämtning visas. Vid samtidiga ändringar måste den senaste versionen hämtas och öppnas med Ändra igen.
- Ingen inloggning krävs. Listorna är gemensamma och kan redigeras av dem som använder appen. Appen skickar inte beställningar till restaurangerna.

## Köra lokalt på Windows

Förutsätter .NET 9, MAUI Windows och databasanslutning i API-projektets User Secrets (`ConnectionStrings:DefaultConnection`). User Secrets konfigureras separat på varje dator. För Supabase-pooler ska projektidentifieraren sitta i `Username=postgres.<projekt-id>`, medan databasnamnet är `Database=postgres`.

Den nya modellen kräver migrationen `DailyRestaurantOrders`. Kör från lösningens katalog mot avsedd databas:

```powershell
dotnet tool restore
dotnet ef database update --project PizzaApp.Api -- --environment Development
dotnet run --project PizzaApp.Api --launch-profile https
```

Starta sedan `PizzaApp` med **Windows Machine** i Visual Studio. API:t ska vara igång på `https://localhost:7113`, som klienten använder. Vid behov: betro utvecklingscertifikatet med `dotnet dev-certs https --trust`.

Migrationer körs inte automatiskt vid start. Tidigare beställningar bevaras i databasen, men exkluderas från nya dagslistor eftersom den gamla modellen inte sparade vilken restaurang eller menyrätt beställningen tillhörde. Befintliga restauranger och pizzor behålls.

Migrationen `SeedAlaCarteExamples` lägger till de fyra À la carte-rätterna och priserna från wireframen som **exempeldata**, inte som verifierad meny för Sperring. Byt till restaurangens riktiga meny när den finns. Sås- och dryckesvalen är också en första fast lista; inga obekräftade tilläggspriser räknas ut.

## Deadline

Pizzerians deadline är **11:15 i Europe/Stockholm**, med automatisk hantering av sommar- och vintertid. Dagen bestäms också i svensk tid. À la carte har ingen deadline.

Som standard får beställningar läggas till, ändras och tas bort efter deadline. För att låsa pizzerian från och med 11:15, ändra API:ts `appsettings.json` och starta om API:t:

```json
"Ordering": {
  "LockAfterDeadline": true
}
```

Alternativt anges miljövariabeln `Ordering__LockAfterDeadline=true`. Servern kontrollerar låsningen för alla tre operationer. Gamla dagars beställningar ändras inte genom dagens lista.

## Tester och bygg

```powershell
dotnet test PizzaApp.Tests/PizzaApp.Tests.csproj
dotnet build PizzaApp/PizzaApp.csproj -f net9.0-windows10.0.19041.0
```

Testerna använder isolerade SQLite-databaser i minnet och ASP.NET:s testserver; de ansluter inte till Supabase. bUnit-testerna kompilerar samma Razor-komponent och klienttjänster som MAUI-appen utan att starta Windows-gränssnittet.

TDD-arbetet började med 17 fallerande regeltester och därefter fem fallerande gränssnittstester. Implementationen gjorde dem gröna. Sviten täcker dessutom API-anrop, klientens HTTP-tjänst, datavalidering, samtidiga ändringar, borttagningsbekräftelse och att PostgreSQL-modellen stämmer med migrationerna. PostgreSQL-migrationens SQL kontrolleras utan en extern databas; själva databasuppgraderingen körs separat.

Orderrutter: `GET/POST /api/restaurants/{restaurantId}/orders` samt `PUT/DELETE /api/restaurants/{restaurantId}/orders/{id}`. Uppdatering skickar aktuell `revision` i kroppen; borttagning skickar den som queryparameter. Den tidigare `/api/orders`-rutten har ersatts.
