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

## Controles

```bash
dotnet test src/backend/WeddingBank.slnx
npm run typecheck --prefix src/frontend
npm run lint --prefix src/frontend
npm test --prefix src/frontend
npm run build --prefix src/frontend
```
