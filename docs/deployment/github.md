# Deploy

Een push naar `main` op `https://github.com/Emiel-Vansteenbrugge/Weddingbank` test de code op GitHub en start daarna de deploy op de self-hosted runner. Die runner staat op dezelfde VM als Onsy, EHBOStock en AmbuWear. De job bouwt de images daar en start ze met Docker Compose. Er is geen SFTP en geen registry voor nodig.

## Eenmalig op de VM

```bash
mkdir -p /home/administrator/config/weddingbank
cp /pad/naar/.env.example /home/administrator/config/weddingbank/.env
```

Zet in dat bestand een echt `POSTGRES_PASSWORD` en de `RESEND_API_KEY`. Het bestand blijft staan. Elke deploy overschrijft het niet.

De runner moet op deze repository staan, met het label `self-hosted`, dezelfde runner als de andere sites. Optioneel, onder Settings → Secrets and variables → Actions:

| Secret | Voorbeeld |
|---|---|
| `DEPLOY_PATH` | `/home/administrator/config/weddingbank` |
| `DEPLOY_HEALTH_URL` | `http://10.120.10.100:8080/health` |

Zonder die secrets zoekt het script dezelfde map en controleert het `http://10.120.10.100:8080/health`.

Nginx Proxy Manager blijft `wedding-bank.evsb.io` naar `10.120.10.100:8080` sturen.

## Wat er daarna gebeurt

`deploy/deploy.sh` kopieert de checkout naar de map hierboven, bewaart `.env`, en draait:

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

PostgreSQL en de Data Protection-sleutels blijven in hun volumes. Een mislukte healthcheck laat de job falen; de volumes worden niet verwijderd.

De API in productie past de migraties toe bij het opstarten en maakt de aanmeldingen `juul`, `amber` en `administrator` aan wanneer die nog ontbreken. Een account dat al bestaat houdt zijn wachtwoord. De databaseverbinding komt uit `POSTGRES_USER`, `POSTGRES_PASSWORD` en `POSTGRES_DB` in het serverbestand `.env`.
