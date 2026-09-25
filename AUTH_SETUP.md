# Inloggning, registrering och administratörer

PizzaApp använder unikt nick/alias och lösenord vid inloggning. Vid registrering anges även e-post eftersom Supabase Auth använder den internt. Kontot och profilen skapas direkt; ingen mejlkod, inbjudningskod eller administratörsgodkännande behövs. Adminbehörighet tilldelas separat av projektägaren.

## Supabase: registrering utan mejl

I projektets **Authentication → Sign In / Providers**:

1. Aktivera **Allow new users to sign up**.
2. Aktivera leverantören **Email** för e-post/lösenord.
3. Under Email: stäng av **Confirm email** och spara.
4. Låt anonym inloggning vara avstängd.

Ingen SMTP-tjänst eller mejlmall behövs för registreringen. API:t kontrollerar att `mailer_autoconfirm` är `true` innan kontot skapas. Om Confirm email fortfarande är på visas ett inställningsfel innan signup, så appen inte börjar skicka mejl av misstag.

Supabase markerar nya adresser som automatiskt bekräftade när Confirm email är avstängt. Det är inte ett bevis på att användaren äger adressen. API:t verifierar fortfarande sessionen hos Supabase, kontots UUID, medlemskap och roll. Glömda lösenord hanteras manuellt av administratören via Supabase; appen har ingen lösenordsåterställning via mejl.

Äldre konton som redan väntar på bekräftelse kan behöva aktiveras separat av administratören i Supabase. Att ändra inställningen ska inte betraktas som att gamla konton automatiskt blivit klara. Radera inte profiler eller order för att lösa detta.

Hämta projektets HTTPS-URL och **publishable key** (`sb_publishable_...`) under Settings → API Keys. Ingen secret/service-role-nyckel behövs i appen eller API:t.

Se [Supabases registreringsinställningar](https://supabase.com/docs/guides/auth/general-configuration).
## Serverkonfiguration

Sätt lokalt i User Secrets med dina egna värden:

```powershell
dotnet user-secrets set "Supabase:Url" "https://DITT-PROJEKT.supabase.co" --project PizzaApp.Api
dotnet user-secrets set "Supabase:PublishableKey" "sb_publishable_DIN_PUBLIKA_NYCKEL" --project PizzaApp.Api
```

I molnet används `Supabase__Url`, `Supabase__PublishableKey` och `ConnectionStrings__DefaultConnection`. Databaslösenordet finns endast på servern. API:t behöver utgående HTTPS till Supabase Auth. Ingen oskyddad reservinloggning finns vid felaktig konfiguration.


`GET /api/auth/config` är anonym och returnerar bara projekt-URL och publik nyckel. Registreringsrutterna `POST /api/registration/start` och `/activate` är också anonyma. Även `POST /api/auth/login` är anonym och tar alias/lösenord. Rutterna delar en gräns på 20 försök per 10 minuter och anslutnings-IP per API-instans; överskridande ger 429 utan kö. Bakom en proxy kan kollegor dela gränsen. Godtyckliga vidarebefordrade IP-headers används inte. Vid flera instanser behövs ett gemensamt begränsningslager.

Övriga rutter kräver verifierat medlemskap. Även OpenAPI i utvecklingsläge skyddas.

## Registrering och återupptagen aktivering

1. Välj **Skapa konto** och ange unikt nick, e-post och lösenord (8–128 tecken).
2. API:t kontrollerar alias och Supabase-inställningar, skapar kontot, verifierar sessionen och sparar profilen.
3. När appen visar **Kontot är klart**, logga in med nick/alias och lösenord och beställ. Kontot blir en vanlig medlem.

Om kontot skapats men profilen eller nätverkssvaret inte kunde sparas, välj **Slutför befintligt konto** med e-post, lösenord och nick. API:t kräver en giltig lösenordsinloggning innan medlemskapet sparas. Upprepade aktiveringar behåller befintligt nick, bild och adminroll. En direktregistrering hos Supabase behöver också slutföra profilen på det sättet. Spärrade konton kan inte återaktiveras via registreringen.
## Första administratören och spärrning

Skapa ett vanligt konto via appen, eller manuellt i **Authentication → Users → Add user → Create new user**. Kopiera kontots UUID. Tilldela admin i Supabases SQL Editor med projektägarens behörighet:

```sql
UPDATE auth.users
SET raw_app_meta_data = COALESCE(raw_app_meta_data, '{}'::jsonb)
    || jsonb_build_object('pizza_role', 'admin')
WHERE id = 'ERSÄTT-MED-ANVÄNDARENS-UUID'::uuid
RETURNING id, email, raw_app_meta_data ->> 'pizza_role' AS pizza_role;
```

Kontrollera att bara avsett konto returneras. För manuell vanlig behörighet används `member`. För att **spärra** ett konto, även självregistrerade medlemmar:

```sql
UPDATE auth.users
SET raw_app_meta_data = COALESCE(raw_app_meta_data, '{}'::jsonb)
    || jsonb_build_object('pizza_role', 'disabled')
WHERE id = 'ERSÄTT-MED-ANVÄNDARENS-UUID'::uuid;
```

API:t hämtar aktuell serverstyrd roll vid varje anrop. En uttrycklig spärr vinner över lokalt medlemskap. **Att bara ta bort `pizza_role` spärrar inte en självregistrerad medlem.** För att häva spärren tilldelar projektägaren `member` eller `admin`. Appens rollindikering uppdateras vid ny inloggning/tokenförnyelse, men servern kontrollerar alltid aktuell roll.

Ingen kan bli admin via `user_metadata`, profil-/registreringsformuläret eller en beställning. Ingen första-användaren-blir-admin-regel finns. Lösenordsåterställning och kontoadministration hanteras tills vidare i Supabase; appen har ingen återställningslänkshantering.

## Databas och appadress

Kör migrationerna till och med **UniqueAliases** före start:

```powershell
dotnet ef database update --project PizzaApp.Api -- --environment Development
```

- `OrderOwnership`: verifierat ägarskap. Äldre order får ingen gissad ägare och kan bara ändras av admin.
- `UserProfiles`: nick, PNG-miniatyr och versionsmarkör. RLS och indragna rättigheter för `PUBLIC`, `anon` och `authenticated` skyddar direktåtkomst.
- `InvitationRegistration`: `IsRegisteredMember` i profiltabellen, standardvärde `false`. Bara det verifierade registreringsflödet sätter värdet till `true`; profiluppdateringar kan inte ändra det.

Även `Orders`, `CompletedOrderDays`, `Restaurants` och `MenuItems` skyddas av RLS och indragna direktbehörigheter. Supabases Data API ska inte kringgå .NET-API:ts regler. Backendens databasanslutning måste använda tabellägaren eller en betrodd roll som kan arbeta trots RLS, aldrig `anon`/`authenticated`. Säkerhetsmigrationers `Down` återöppnar inte direktåtkomsten.

Ange API-serverns HTTPS-adress i `PizzaApp/clientsettings.json` och bygg appen på nytt. Standardvärdet `https://localhost:7113/` gäller lokal Windows-utveckling, inte en fysisk telefon.

## Profiler, behörigheter och sessioner

Registreringen sparar nicket. Manuella/äldre konton utan profil behöver först välja **Skapa konto → Slutför befintligt konto**, med e-post, lösenord och önskat alias. `/settings` kan ändra nick och profilbild men inte roll, medlemskap, ägare eller inloggningsadress. `GET /api/auth/me` returnerar den egna profilen; `PUT /api/auth/profile` använder alltid det verifierade kontots id och versionskontroll. Vid upptaget alias väljs ett annat. Vid versionskonflikt används **Hämta sparad profil**. Nick jämförs utan skillnad på stora/små bokstäver och inledande/avslutande blanksteg. Ett namnbyte ändrar även användarnamnet vid nästa inloggning.

API:t hämtar nick från profilen för nya beställningar. Klientfältet `Name` ignoreras. Redigering och historik behåller ursprungligt namn. Bilder väljs som JPG/PNG på högst 5 MB, förminskas till högst 256 × 256 och lagras som PNG på högst 256 kB. Servern kontrollerar format, struktur, mått och storlek. Inga externa bildlänkar/SVG tillåts. Ingen Storage-bucket behövs och bilderna ligger inte i JWT eller Supabase Auth-metadata.

| Åtgärd | Medlem | Administratör |
|---|---|---|
| Se restauranger, menyer och dagens lista | Ja | Ja |
| Lägga beställning på sitt konto | Ja | Ja |
| Ändra/ta bort beställning eller markera hämtare | Egna | Alla |
| Avsluta dagen och spara historik | Nej | Ja |
| Ändra en redan avslutad dag | Nej | Nej |

Admin kringgår inte deadline/historiklåsning. Otillåten inloggning ger 401 och otillåten åtgärd 403. Lösenord och tokens skrivs inte till filer/webbläsarlagring. Sessionen finns i minnet, förnyas under användning och kräver ny inloggning efter appstängning. Utloggning rensar lokalt även vid nätverksfel och försöker avsluta Supabase-sessionen. En access-token kan gälla tills den löper ut; sätt `pizza_role` till `disabled` för omedelbar spärr av PizzaApp.

## Verifiering före publicering

Tester använder simulerad Supabase, isolerad SQLite och riktiga ASP.NET-behörighetskontroller. Inga mejl eller riktiga tokens skickas. PostgreSQL-migrationer kontrolleras via genererad SQL och appliceras separat på er databas.

Efter konfiguration: testa registrering utan mejl, slutförande av befintligt konto, inloggning och beställning. Kontrollera att medlemmen inte kan ändra andras order eller avsluta dagen. Kontrollera även 401 utan token och att Supabases Data API inte kan läsa apptabeller med publik nyckel/användartoken. Livekontroll och mobiltestning återstår i den konfigurerade miljön.

Referenser: [lösenord](https://supabase.com/docs/guides/auth/passwords), [verifierad användare](https://supabase.com/docs/reference/javascript/auth-getuser), [RLS och app_metadata](https://supabase.com/docs/guides/database/postgres/row-level-security), [registreringsinställningar](https://supabase.com/docs/guides/auth/general-configuration).

## Uppgradering till aliasinloggning

`UniqueAliases` lägger till ett unikt databasindex och en intern koppling till kontots verifierade e-post. Befintliga profiler kopplas via UUID till `auth.users` om API-databasen är i samma Supabase-projekt och migrationsrollen kan läsa tabellen. Kopplingen lämnas aldrig ut genom en anonym aliasuppslagning. Lösenord lagras fortsatt bara hos Supabase. Servern verifierar att token tillhör profilens UUID innan sessionen lämnas tillbaka.

Kontrollera eventuella gamla dubbletter före migrationen:

```sql
SELECT upper(btrim("DisplayName")) AS alias, count(*)
FROM "UserProfiles"
GROUP BY upper(btrim("DisplayName")) HAVING count(*) > 1;
```

Om dubbletter finns avbryts migrationen utan att byta namn på någon. Kom överens om unika nick och uppdatera respektive profils `DisplayName` innan migrationen körs igen. Historiska order behöver inte ändras. Om auth-tabellen ligger i en annan databas, eller e-postadressen senare ändras i Supabase, kan kontot återkopplas med **Slutför befintligt konto**. Den vägen kräver en verifierad Supabase-session med rätt lösenord och behåller befintligt alias och roll.

Alias reserveras när profilen sparas. Om två personer registrerar samma alias samtidigt kan endast en lyckas; den andra väljer ett annat nick via Slutför befintligt konto.
