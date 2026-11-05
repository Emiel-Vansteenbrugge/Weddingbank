# The Wedding Bank

Digitale basis voor The Wedding Bank. De architectuur staat in [docs/adr/0001-architectuur.md](docs/adr/0001-architectuur.md). De deploy staat in [docs/deployment/github.md](docs/deployment/github.md).

Deze repository bevat de publieke site, de gedeelde aanmelding, en de eerste client- en beheerpanelen. Het spel volgt later.

## Lokaal starten

PostgreSQL 18, alleen bereikbaar op deze machine:

```bash
docker compose -f infra/compose/docker-compose.dev.yml up -d
```

De lokale database heet `weddingbank`, met gebruiker `weddingbank` en wachtwoord `weddingbank_dev`. Andere waarden horen in een privébestand, naar het voorbeeld in `infra/compose/env/.env.example`.

API:

```bash
dotnet run --project src/backend/WeddingBank.Api
```

De API luistert op `http://localhost:5080`. `GET /health` antwoordt met 200.

Frontend:

```bash
npm ci --prefix src/frontend
npm run dev --prefix src/frontend
```

De frontend draait op `http://localhost:5173` en proxyt `/api` en `/hubs` naar de API.

De API past het schema niet aan bij het opstarten. Pas de migraties zelf toe, vanuit de repositoryroot:

```bash
dotnet ef database update --project src/backend/WeddingBank.Infrastructure --startup-project src/backend/WeddingBank.Api
```

Daarvoor is de tool `dotnet-ef` 10.0.14 nodig. Ontbreekt die, installeer hem dan met `dotnet tool install --global dotnet-ef --version 10.0.14`.

Lokale accounts worden niet vanzelf aangemaakt. Alleen met `ASPNETCORE_ENVIRONMENT=Development`:

```bash
dotnet run --project src/backend/WeddingBank.Api -- --seed-development-users
```

De logins zijn `juul`, `amber` en `administrator`. De wachtwoorden staan alleen in `src/backend/WeddingBank.Api/appsettings.Development.json` en zijn voor deze machine.

Chapter I wordt niet bij het opstarten aangemaakt. Het commando hieronder gebruikt de database van de gekozen omgeving, maakt ontbrekende records aan en herstelt afwijkende Chapter I-configuratie. Inzendingen en een voltooide opdracht blijven staan. Het start de API niet en sluit af zodra de initialisatie klaar is.

```bash
dotnet run --project src/backend/WeddingBank.Api -- --initialize-chapter-i
```

Op de server, in de draaiende API-container:

```bash
docker exec weddingbank-api dotnet WeddingBank.Api.dll --initialize-chapter-i
```

## Tijdelijke testaccounts

Deze wisselaar is tijdelijk en moet vóór de trouwdag weg. Verwijder dan `TestAccountSwitch`, de routes `/api/auth/test-accounts`, `TestAccountBar` en de bijbehorende stijl.

De knoppen Juul, Amber en Administrator verschijnen alleen als de server dat toestaat. De frontend beslist dat niet. De server weigert het verzoek met 404 zolang `TestAccounts:Enabled` niet expliciet `true` is, en ook altijd wanneer `ASPNETCORE_ENVIRONMENT` `Production` is. Een lokale `dotnet run` zet de schakelaar in `launchSettings.json`. Het productiecompose-bestand zet hem niet en draait als Production, dus daar blijft de wisselaar uit, ook als iemand de variabele per ongeluk in `.env` zet zonder het compose-bestand én de omgevingsnaam te wijzigen.

`test.wedding-bank.evsb.io` is geen aparte serveromgeving. Die site gebruikt hetzelfde Production-compose als de live site. De wisselaar werkt daar pas als die stack zelf geen Production is én `TestAccounts__Enabled=true` alleen daar staat.

Wisselen maakt een gewone sessie voor een bestaand huisaccount. Het maakt geen accounts, wijzigt geen wachtwoorden en raakt de spelvoortgang niet.

## Controles

```bash
dotnet test src/backend/WeddingBank.slnx
npm run typecheck --prefix src/frontend
npm run lint --prefix src/frontend
npm test --prefix src/frontend
npm run build --prefix src/frontend
```
