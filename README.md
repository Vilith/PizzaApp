# PizzaApp

Blazor WebAssembly-webbapp på .NET 10 med två restauranger och en separat beställningslista per restaurang och dag. `PizzaApp.Api` serverar både gränssnitt och API på samma adress. Ingen installation eller MAUI/Android/iOS-workload behövs.

Inloggning krävs. Se [AUTH_SETUP.md](AUTH_SETUP.md) för Supabase-konfiguration, användarkonton, adminroller och databasbehörigheter före publicering.

## Flöde

- Välj **Skapa konto**, ange nick, e-post och lösenord. Kontot blir klart direkt, utan mejlkod eller administratörsgodkännande. Logga in med ditt alias och lösenord. Äldre konton utan profil väljer nick via Slutför befintligt konto. Välj därefter Pizzeria (Kvänum Pizzeria) eller À la carte (Sperring).
- Pizzeria: välj rätt via menyflikarna (pizzor 1–12, 13–24, 25–33, 34–43 samt Sallader, Kebab och Stekrätter). Ditt val centreras automatiskt. Första tillgängliga såsen i ingredienslistan förväljs, annars Ingen sås; valet kan ändras. Såser: Vitlökssås, Bearnaisesås, Kebabsås, Kebabsås (Mixad) och Kebabsås (Stark). Drycker: Coca-Cola 33cl, Fanta 33cl, Sprite 33cl och Pepsi Max 33cl. Välj dryck och antal.
- Sperring: två veckomenyer, måndag–fredag, med dagens beställningslista längst ner. Välj en rätt under dagens datum och ange antal och eventuell kommentar. Listan visar alias, beställd maträtt och profilbild när sådan finns. Nästa veckas meny är till för planering; beställningar gäller dagens svenska datum.
- Admin kan redigera Sperrings år, veckonummer, återkommande rätter och extra rätter per vardag. Skriv en rätt per rad. Återkommande rätter visas alla fem vardagar. Menyer sparas separat; samma vecka får inte användas i båda menyerna. Veckonumren flyttas fram manuellt. Samtidiga ändringar skyddas med revisioner; ladda om innan du försöker spara en inaktuell meny igen.
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

Migrationen `SperringWeeklyMenus` skapar tabellen `SperringWeeks`. Kör den via projektets vanliga databasuppdatering före användning. Inga menyer eller beställningar fylls i automatiskt; en admin publicerar de riktiga rätterna. De gamla exempelrätterna från `SeedAlaCarteExamples` är inte beställningsbara i Sperring om de inte finns i dagens publicerade meny. Nya Sperringrätter har inget administrerbart pris i denna vy och ska inte användas som underlag för kostnadsstatistik.

## Historik för årsstatistik

Efter hämtning rensas dagens lista i vyn: beställningar, drycker och hämtarval döljs och ersätts av en kort bekräftelse. Detta gäller även efter uppdatering eller återbesök. Beställningarna och historiken behålls i databasen, och dagen är fortsatt låst.

`CompletedOrderDays` sparar en oföränderlig ögonblicksbild per restaurang och svenskt datum: restaurangnamn/typ, hämtningstid (UTC), unika hämtarnamn i `CollectorsJson` och alla beställningsuppgifter i `OrdersJson`. Där finns antal, rätt, sås, dryck, beställare, kommentar och styckpris. Ett nytt kalenderdygn får en ny lista; historiken ligger kvar. Upprepade avslut skapar inga dubbletter. Ändringar och avslut samordnas med en databastransaktion och ett gemensamt radlås per restaurang.

Historiken innehåller antal, såser/drycker, sparade styckpriser och hämtarnamn. Menypriset sparas vid beställning (eller byte av rätt); vanliga ändringar uppdaterar inte priset. Tidigare beställningar kan ha okänt menypris (`null`). Pizzastatistiken nedan använder det gemensamma lunchpriset 95 kr i stället för de sparade menypriserna.

### Pizzastatistik

Pizzerian har valet **Enbart dryck**, tillgängligt vid menyflikarna. Välj dryckessort och antal; varje extra burk kostar **15 kr** och har inget såsval. Beställningen går att ändra, ta bort och avsluta med dagens vanliga lista. Den sparas med `DrinkOnly` i historiken och räknas i dryckesantal, dryckestopplista och pantvärde, men inte i matantal eller topp 5 maträtter. Totalt beställningsvärde är matportioner × 95 kr + separat beställda burkar × 15 kr. Statistiken visar antal separata drycker och deras andel av beställningsvärdet. Ingen databasmigration behövs; dryckesbeställningen använder ett tomt menyrätts-ID i den befintliga ordertabellen.

Statistikens produktionsstart är **2026-10-02**. `Statistics:StartDate` i API:ts konfiguration utesluter hela den tidigare testperioden. `Statistics:ExcludedNames` innehåller `tff` och utesluter både beställningar med det namnet och dess hämtningsbidrag (utan skillnad på stora/små bokstäver). Beställningar med testformatet `33 cl` (whitespace mellan 33 och cl) utesluts också från samtliga totaler. `Statistics:ExcludedDates` kan användas för att undanta ytterligare hela testdagar. Dagar utan återstående beställningar räknas inte i mattotalerna. Registrerade hämtare räknas däremot oberoende av filtrerade beställningar och dryckesval, inom startdatumet och med samma undantag för namn och hela testdagar. Filtren ändrar aldrig databashistoriken. Startdatum kan också anges som miljövariabel `Statistics__StartDate` på servern.

**🍕-Statistik** i toppmenyn öppnar `/statistics`. Startsidan visar en kompakt länk med pulserande gradient under restaurangdörrarna. Periodvalet visar Sedan starten (2/10-26). Alla inloggade användare kan se statistik för sedan starten eller ett valt kalenderår.

Statistiken räknar endast avslutade pizzeriadagar i `CompletedOrderDays`; Sperring och pågående beställningar ingår inte. Portionsantal och topp 5 omfattar all mat från pizzerian, inklusive pizzor, sallader, kebab och övriga rätter, och summerar antal portioner. Totalt beställningsvärde är samtliga pizzeriaportioner (inklusive sallad, kebab och övriga rätter) × det gemensamma lunchpriset **95 kr**, även för äldre historik med andra eller okända menypriser. Dryck ingår i lunchpriset och läggs inte till som en extra kostnad. Dryckernas topplista visar endast de aktuella dryckesvalen i PizzaChoices.Drinks och antal portioner. Hämtare rankas efter antal avslutade dagar med respektive sparat namn; skillnader i stora/små bokstäver och omgivande blanksteg slås ihop, medan olika nick räknas separat.

Nya historikposter sparar rättens kategori i JSON-ögonblicksbilden, men matstatistiken omfattar alla kategorier. Äldre beställningar utan avslutad historik ingår inte. API: `GET /api/statistics/pizzeria` med valfri `?year=2026`. Ingen databasmigration behövs.

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

Direktlänkar till `/pizzerian`, `/sperring` och `/settings` fungerar också vid omladdning. Inloggningen ligger i webbläsarflikens minne: en omladdning eller ny flik kräver ny inloggning. Varje flik har sin egen session. Dörranimationen spelas bara vid restaurangval, inte direkt efter inloggning. Profilbilder och rollkontroller finns kvar.

Render: välj Web Service, Docker, Dockerfile i repots rot och port 10000. Dockerfile och .dockerignore finns för att publicera webbapp och API tillsammans. Ange serverns miljövariabler i Render. Containerbygget behöver verifieras där eftersom Docker inte finns i den lokala miljön. HTTPS hanteras av Render. Proxyinställningar för klient-IP behöver verifieras vid driftsättning; utan dem kan inloggningens hastighetsbegränsning delas av flera användare bakom samma proxy. Ingenting publiceras automatiskt av den lokala konverteringen.

## Glömt lösenord utan SMTP

Under Inställningar kan användaren byta sitt lösenord. Administratörer kan välja en kollega och skapa ett slumpat tillfälligt lösenord efter bekräftelse med sitt eget lösenord. Kollegan måste välja ett eget lösenord vid nästa inloggning innan beställningar kan göras. Kontot och historiken behålls.

Servern behöver **Supabase__SecretKey** på Render (**Supabase:SecretKey** i lokala User Secrets). Använd en Supabase secret key och håll den enbart på servern. Ingen ny migration behövs. Se [AUTH_SETUP.md](AUTH_SETUP.md) för konfiguration och arbetsgång.
## Tillfällig byggsida för Sperring

Sperring-dörren och direktadressen /sperring visar tills vidare en byggsida med hjälmkatter, kran, saftblandare och pappaskämt. Ingen meny eller orderlista hämtas för den sidan. Pizzerians flöde fungerar som tidigare. Animationerna kan pausas och respekterar prefers-reduced-motion. Sperrings befintliga data och API är kvar; detta är en tillfällig ersättning av gränssnittet.

## Utgångna menyrätter

Migrationen RemoveDuplicateMenuItems behåller Kebabtallrik (ID 62) med IsHidden = true för befintliga beställningar. Rätten visas inte i menyn och kan inte väljas för nya beställningar; befintliga beställningar kan fortfarande ändras. Kebabrulle (63) och Kycklingrulle (64) tas bort, vilket förutsätter att inga beställningar hänvisar till dem. Den synliga pizzeriamenyn har därefter 58 rätter. Kör migrationen mot avsedd databas före publicering av koden som använder IsHidden.
