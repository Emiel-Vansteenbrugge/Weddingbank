# The Wedding Bank

Digitale basis voor The Wedding Bank. De architectuur staat in [docs/adr/0001-architectuur.md](docs/adr/0001-architectuur.md). De deploy staat in [docs/deployment/github.md](docs/deployment/github.md).

Deze stap bevat de solution, een lege frontend, lokale PostgreSQL 18, Dockerfiles en de CI/CD-basis. Accounts, MFA, spel en beheer volgen later.

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

## Controles

```bash
dotnet test src/backend/WeddingBank.slnx
npm run typecheck --prefix src/frontend
npm run lint --prefix src/frontend
npm run build --prefix src/frontend
```
