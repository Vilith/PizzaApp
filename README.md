# PizzaApp

Blazor WebAssembly-webbapp på .NET 10 med två restauranger och en separat beställningslista per restaurang och dag. `PizzaApp.Api` serverar både gränssnitt och API på samma adress. Ingen installation eller MAUI/Android/iOS-workload behövs.

Inloggning krävs. Se [AUTH_SETUP.md](AUTH_SETUP.md) för Supabase-konfiguration, användarkonton, adminroller och databasbehörigheter före publicering.

## Flöde

- Välj **Skapa konto**, ange nick, e-post och lösenord. Kontot blir klart direkt, utan mejlkod eller administratörsgodkännande. Logga in med ditt alias och lösenord. Äldre konton utan profil väljer nick via Slutför befintligt konto. Välj därefter Pizzeria (Kvänum Pizzeria) eller À la carte (Sperring).
- Pizzeria: välj rätt via menyflikarna (pizzor 1–12, 13–24, 25–33, 34–43 samt Sallader, Kebab och Stekrätter). Ditt val centreras automatiskt. Första tillgängliga såsen i ingredienslistan förväljs, annars Ingen sås; valet kan ändras. Såser: Vitlökssås, Bearnaisesås, Kebabsås, Kebabsås (Mixad) och Kebabsås (Stark). Drycker: Coca-Cola 33cl, Fanta 33cl, Sprite 33cl och Pepsi Max 33cl. Välj dryck och antal. Dagens lista rullar till listan på samma sida.
- À la carte: välj rätt och antal, utan sås- eller dryckesval.
- Ditt profilnamn används automatiskt; du behöver inte fylla i namn vid beställning. Kommentar är valfri. Under **Inställningar** kan du ändra nick samt välja eller ta bort en profilbild. Nya beställningar får det nya namnet; redan sparade beställningar och historiken behåller sitt ursprungliga namn.
- ”Att ringa in” visar beställarnas namn med kryssrutor för vilka som kan hämta. Samma konto och namn visas en gång; olika konton hålls isär även om namnet är samma. Namnlösa beställningar behöver ett namn innan de kan väljas som hämtare.
- ”Alla drycker” är en egen utfällbar lista med antal per dryck. Varje portion räknas som en dryck. ”Alla beställningar” visar fortfarande rätter, tillval och kommentarer.
- När maten är hämtad: en administratör låter de faktiska hämtarna vara ikryssade och trycker ”Pizzorna är hämtade” (”Maten är hämtad” för À la carte). Dagen låses och sparas i historiken. Minst en namngiven hämtare krävs. Hämtare kan väljas även efter deadline.
- Ändra en beställning med Ändra och spara formuläret. Borttagning kräver bekräftelse.
- Uppdatera listan för att hämta andras senaste beställningar. Tidpunkten för senaste hämtning visas. Vid samtidiga ändringar måste den senaste versionen hämtas och öppnas med Ändra igen.
- Godkända användare ser den gemensamma listan och kan skapa och ändra sina egna beställningar. Administratörer kan även ändra andras beställningar och avsluta dagen. Appen skickar inte beställningar till restaurangerna.

## Köra webbappen lokalt

Förutsätter .NET 10 SDK 10.0.401 (eller senare patch i 10.0.4xx), valfritt Visual Studio 2026 med arbetsbelastningen ASP.NET och webbutveckling, samt databasanslutning i API-projektets User Secrets (`ConnectionStrings:DefaultConnection`). User Secrets konfigureras separat på varje dator. För Supabase-pooler ska projektidentifieraren sitta i `Username=postgres.<projekt-id>`, medan databasnamnet är `Database=postgres`.

Den nya modellen kräver migrationerna till och med `CompletePizzeriaMenuSections`. Konfigurera Supabase Auth och stäng av Confirm email enligt installationsguiden. Kör från lösningens katalog mot avsedd databas:

```powershell
dotnet tool restore
dotnet ef database update --project PizzaApp.Api -- --environment Development
dotnet run --project PizzaApp.Api --launch-profile https
```

Öppna **https://localhost:7113/** i webbläsaren. I Visual Studio väljer du **PizzaApp.Api** som startprojekt (högerklicka → Ange som startprojekt), eller startprofilen **Webbapp**. Starta inte klientprojektet separat. Vid behov: betro utvecklingscertifikatet med `dotnet dev-certs https --trust`. Klienten använder automatiskt samma adress som webbsidan; `clientsettings.json` behövs inte längre.

Migrationer körs inte automatiskt vid start. Tidigare beställningar bevaras i databasen, men exkluderas från nya dagslistor eftersom den gamla modellen inte sparade vilken restaurang eller menyrätt beställningen tillhörde. Befintliga restauranger och pizzornas tidigare ID:n behålls. Migrationen `FullPizzeriaMenu` lägger in Kvänum Pizzerias 43 pizzor med menynummer och ingredienser: nummer 1–12 kostar 85 kr, 13–43 kostar 90 kr förutom nummer 36 (Flygande Tefat) som kostar 100 kr. Tomat och ost ingår i alla pizzor. Migrationen `CompletePizzeriaMenuSections` kompletterar menyn med egna sektioner för 8 sallader (90 kr), 9 kebabrätter/rullar (90 kr) och hamburgare 90gr med bröd och pommes (80 kr). Pizzerians kompletta meny innehåller 61 rätter. Sparade orderpriser och historik ändras inte.

Migrationen `SeedAlaCarteExamples` lägger till de fyra À la carte-rätterna och priserna från wireframen som **exempeldata**, inte som verifierad meny för Sperring. Byt till restaurangens riktiga meny när den finns. Sås- och dryckesvalen är också en första fast lista; inga obekräftade tilläggspriser räknas ut.

## Historik för årsstatistik

Efter hämtning rensas dagens lista i vyn: beställningar, drycker och hämtarval döljs och ersätts av en kort bekräftelse. Detta gäller även efter uppdatering eller återbesök. Beställningarna och historiken behålls i databasen, och dagen är fortsatt låst.

`CompletedOrderDays` sparar en oföränderlig ögonblicksbild per restaurang och svenskt datum: restaurangnamn/typ, hämtningstid (UTC), unika hämtarnamn i `CollectorsJson` och alla beställningsuppgifter i `OrdersJson`. Där finns antal, rätt, sås, dryck, beställare, kommentar och styckpris. Ett nytt kalenderdygn får en ny lista; historiken ligger kvar. Upprepade avslut skapar inga dubbletter. Ändringar och avslut samordnas med en databastransaktion och ett gemensamt radlås per restaurang.

Underlaget kan användas för antal pizzor, populäraste såser/drycker, summan av antal × styckpris och antal hämtningsdagar per namn. Priset sparas vid beställning (eller byte av rätt); vanliga ändringar uppdaterar inte priset. Tidigare beställningar har okänt pris (`null`), inte noll kronor. Priset avser menyrätten; extra avgifter för såser/drycker modelleras ännu inte. Namn är fritext, så olika stavningar räknas som olika personer. Någon separat vy för årsstatistik ingår ännu inte.

Nya rutter: `PUT /api/restaurants/{restaurantId}/orders/{id}/collector` och `POST /api/restaurants/{restaurantId}/orders/complete`. Avslutet skickar datum och alla visade orderrevisioner; en ändrad lista måste hämtas igen.

## Deadline och låsning

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
dotnet build PizzaApp.Api/PizzaApp.Api.csproj
```

Testerna använder isolerade SQLite-databaser i minnet och ASP.NET:s testserver; de ansluter inte till Supabase. bUnit-testerna kompilerar samma Razor-komponent och klienttjänster som webbappen.

TDD-arbetet började med 17 fallerande regeltester och därefter fem fallerande gränssnittstester. Implementationen gjorde dem gröna. Sviten täcker dessutom API-anrop, klientens HTTP-tjänst, datavalidering, samtidiga ändringar, borttagningsbekräftelse och att PostgreSQL-modellen stämmer med migrationerna. PostgreSQL-migrationens SQL kontrolleras utan en extern databas; själva databasuppgraderingen körs separat.

Orderrutter: `GET/POST /api/restaurants/{restaurantId}/orders` samt `PUT/DELETE /api/restaurants/{restaurantId}/orders/{id}`. Uppdatering skickar aktuell `revision` i kroppen; borttagning skickar den som queryparameter. Den tidigare `/api/orders`-rutten har ersatts.

Inloggning sker med unikt nick/alias och lösenord. Kör migrationerna till och med `CompletePizzeriaMenuSections`; uppgraderingssteg för befintliga konton finns i [AUTH_SETUP.md](AUTH_SETUP.md). E-post anges vid registrering men behöver inte bekräftas. SMTP behövs inte för registreringen.

## Publicera webbappen

Publicera serverprojektet, så följer klienten och alla statiska resurser med:

```powershell
dotnet publish PizzaApp.Api/PizzaApp.Api.csproj -c Release -o artifacts/publish
```

Kör `dotnet PizzaApp.Api.dll` från publiceringskatalogen på en server med ASP.NET Core 10. Sätt `ConnectionStrings__DefaultConnection`, `Supabase__Url` och `Supabase__PublishableKey` på servern. Databaslösenord ska aldrig läggas i klientprojektet eller i `wwwroot`. Befintlig Supabase-databas används; webbkonverteringen kräver ingen ny databas eller migration.

Direktlänkar till `/pizzerian`, `/sperring` och `/settings` fungerar också vid omladdning. Inloggningen ligger i webbläsarflikens minne: en omladdning eller ny flik kräver ny inloggning. Varje flik har sin egen session. Dörranimationerna, profilbilder och rollkontroller finns kvar.

Render: välj Web Service, Docker, Dockerfile i repots rot och port 10000. Dockerfile och .dockerignore finns för att publicera webbapp och API tillsammans. Ange serverns miljövariabler i Render. Containerbygget behöver verifieras där eftersom Docker inte finns i den lokala miljön. HTTPS hanteras av Render. Proxyinställningar för klient-IP behöver verifieras vid driftsättning; utan dem kan inloggningens hastighetsbegränsning delas av flera användare bakom samma proxy. Ingenting publiceras automatiskt av den lokala konverteringen.
