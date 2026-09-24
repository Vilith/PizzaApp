# Inloggning och administratörer

PizzaApp använder Supabase Auth med e-post/lösenord. API:t verifierar varje Bearer-token med projektets `/auth/v1/user` över HTTPS och läser **aktuell serverstyrd** `app_metadata.pizza_role`. `user_metadata`, namn, e-post och värden som skickas i en beställning ger aldrig behörighet. Ingen egen lösenordsdatabas eller service-role-nyckel behövs i appen eller API:t.

## 1. Supabase-projektet

- Aktivera e-post/lösenord i Supabase Auth.
- Stäng av **Allow new users to sign up** och anonym inloggning. Konton skapas av projektets administratör. API:t kräver dessutom uttrycklig tilldelning av `pizza_role`, så ett konto utan roll nekas även om registrering skulle aktiveras av misstag.
- Hämta projektets HTTPS-URL och en **publishable key** (`sb_publishable_...`) under projektets API-nycklar. Denna implementation accepterar avsiktligt inte secret/service-role-nycklar eller äldre JWT-baserade anon-nycklar som konfiguration.

## 2. Konfigurera API:t

Sätt lokalt med User Secrets (ersätt exempelvärdena):

```powershell
dotnet user-secrets set "Supabase:Url" "https://DITT-PROJEKT.supabase.co" --project PizzaApp.Api
dotnet user-secrets set "Supabase:PublishableKey" "sb_publishable_DIN_PUBLIKA_NYCKEL" --project PizzaApp.Api
```

I molnet används miljövariablerna `Supabase__Url`, `Supabase__PublishableKey` och `ConnectionStrings__DefaultConnection`. Databaslösenordet ska endast finnas på servern. API:t behöver utgående HTTPS till Supabase Auth. Vid saknad eller felaktig konfiguration nekas skyddade anrop; det finns inget oskyddat utvecklingsläge.

`GET /api/auth/config` är den enda anonyma app-rutten och returnerar endast projekt-URL och den publika nyckeln. `GET /api/auth/me` kräver ett godkänt konto. Swagger/OpenAPI i utvecklingsläge skyddas också av standardpolicyn.

## 3. Skapa första administratören och användarna

Skapa kontot med e-post/lösenord i **Authentication → Users → Add user → Create new user**. Använd endast e-postadresser du vet tillhör rätt person; kontot ska vara bekräftat. Kopiera användarens UUID. Sätt rollen i Supabases SQL Editor med projektägarens behörighet:

```sql
UPDATE auth.users
SET raw_app_meta_data = COALESCE(raw_app_meta_data, '{}'::jsonb)
    || jsonb_build_object('pizza_role', 'admin')
WHERE id = 'ERSÄTT-MED-ANVÄNDARENS-UUID'::uuid
RETURNING id, email, raw_app_meta_data ->> 'pizza_role' AS pizza_role;
```

För en vanlig användare: använd samma kommando med **`member`** i stället för `admin`. Kontrollera att precis avsett konto returneras. För att dra in tillgången:

```sql
UPDATE auth.users
SET raw_app_meta_data = raw_app_meta_data - 'pizza_role'
WHERE id = 'ERSÄTT-MED-ANVÄNDARENS-UUID'::uuid;
```

API:t hämtar aktuell roll vid varje anrop. Appens rollindikering uppdateras vid ny inloggning eller tokenförnyelse, men servern använder alltid den aktuella rollen. Ingen kan själv ändra `app_metadata` via de vanliga användar-API:erna. Det finns ingen första-användaren-blir-admin-regel.

Kontohantering och lösenordsåterställning sköts tills vidare av projektadministratören i Supabase; appen har ingen registreringssida, kontoadministration eller hantering av inbjudnings-/återställningslänkar. Använd **Create new user**, inte ett inbjudningsflöde som förutsätter en callback-sida som appen ännu inte har.

## 4. Databas och appadress

```powershell
dotnet ef database update --project PizzaApp.Api -- --environment Development
```

`OrderOwnership` lägger till `OwnerUserId`. Äldre beställningar får ingen gissad ägare och kan bara ändras av administratörer. Historiken bevaras.

Migrationen aktiverar RLS och tar bort direktbehörighet för `PUBLIC`, `anon` och `authenticated` på `Orders`, `CompletedOrderDays`, `Restaurants` och `MenuItems`. Därmed kan Supabases publika Data API inte kringgå .NET-API:ts regler, även om tabellerna ligger i `public`. API:ts databasanslutning ska använda tabellägaren eller en särskild betrodd backend-roll som kan läsa/skriva dessa tabeller trots RLS; använd inte `anon`/`authenticated`. Migrationens `Down` återöppnar avsiktligt inte direktåtkomsten. Detta skydd ingår i den genererade PostgreSQL-migrationen; kör den före publicering.

Ange API-serverns HTTPS-adress i `PizzaApp/clientsettings.json` och bygg appen på nytt. Filen innehåller inga hemligheter. Standardvärdet `https://localhost:7113/` är för lokal Windows-utveckling och fungerar inte mot datorn från en fysisk telefon.

## Behörigheter och sessioner

| Åtgärd | Medlem | Administratör |
|---|---|---|
| Se restauranger, menyer och dagens gemensamma lista | Ja | Ja |
| Lägga beställning på sitt konto | Ja | Ja |
| Ändra/ta bort beställning | Egna | Alla |
| Ändra vilka som kan hämta | Egna beställningar | Alla |
| Avsluta dagen och spara historik | Nej | Ja |
| Ändra en redan avslutad dag | Nej | Nej |

Ägarskap sätts av servern från den verifierade identiteten. Adminrollen kringgår inte deadline eller historiklåsning. Inloggning och roller kontrolleras även vid direkta API-anrop: ogiltig/obefintlig inloggning ger 401, otillåten åtgärd ger 403.

Lösenord och tokens skrivs inte till filer eller webbläsarlagring. Sessionen finns endast i minnet, förnyas automatiskt medan appen används och kräver ny inloggning efter att appen stängts. Utloggning rensar lokala uppgifter även vid nätverksfel och försöker även avsluta Supabase-sessionen. Supabase-access-token kan vara giltig tills den löper ut även efter utloggning; för omedelbar indragning av PizzaApp-behörighet används borttagning av `pizza_role` ovan.

## Verifiering innan publicering

Tester använder en simulerad Supabase Auth-server, isolerad SQLite och riktiga ASP.NET-autentiserings-/auktoriseringskomponenter. De skickar inga riktiga lösenord eller tokens till Supabase. PostgreSQL-migrationen kontrolleras via genererad SQL, inte genom en anslutning till produktionsdatabasen.

Efter konfiguration och migration: logga in med en medlem och en admin, kontrollera att medlemmen inte kan ändra adminens order eller avsluta dagen, och att ett anrop till `/api/restaurants` utan token ger 401. Kontrollera även att Supabase Data API inte kan läsa appens tabeller med den publika nyckeln eller en vanlig användartoken. Livekontroll mot ert Supabase-projekt och mobiltestning behöver göras i den konfigurerade miljön.

Referenser: [Supabase lösenordsinloggning](https://supabase.com/docs/guides/auth/passwords), [serververifierad användare](https://supabase.com/docs/reference/javascript/auth-getuser), [RLS och app_metadata](https://supabase.com/docs/guides/database/postgres/row-level-security), [registreringsinställningar](https://supabase.com/docs/guides/auth/general-configuration).
