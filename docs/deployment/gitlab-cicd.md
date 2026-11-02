# GitLab CI/CD

De actieve pipeline hoort bij `https://gitlab.com/evsb-group/weddingbank`. GitHub Actions blijft als bestand in de repository staan en wordt niet gebruikt.

Een geslaagde deploy-job betekent dat GitLab de opdracht heeft vastgelegd. Het betekent niet dat de Debian-server de nieuwe images al draait of dat `/health` slaagt. De serverstatus staat in `/var/lib/weddingbank/test/status.json` of `/var/lib/weddingbank/prod/status.json`.

## Pipeline

`.gitlab-ci.yml` heeft vijf stages. Een latere stage start pas wanneer de vorige slaagt.

| Stage | Jobs | Wat ze doen |
|---|---|---|
| validate | `backend:build`, `frontend:validate`, `compose:validate`, `scripts:validate` | .NET restore en build, TypeScript-typecheck, oxlint, Compose-configuratie, deployscripttests |
| test | `backend:test` | De bestaande xUnit-test in `WeddingBank.Tests` |
| build | `frontend:build`, en bij een merge request ook `docker:build` | Production-build van de frontend. Bij een merge request ook de drie images, zonder publicatie |
| publish | `docker:publish` | Bouwt en publiceert de drie images. Alleen op `develop` en `main` |
| deploy | `deploy:test` of `deploy:production` | Legt een onveranderlijk manifest vast. Neemt geen verbinding op met de server |

De frontend heeft geen ESLint. `npm run lint` draait oxlint, de linter die in `package.json` staat.

De Dockerfiles die de pipeline bouwt, zijn:

- `infra/docker/api.Dockerfile`
- `infra/docker/web.Dockerfile`
- `infra/docker/proxy.Dockerfile`

`docker:build` en `docker:publish` gebruiken hetzelfde buildscript. Een merge request bouwt de drie images en voert `nginx -t` uit in de proxy-image. Publicatie pusht daarna en bewaart de registry-digests in `image-digests.env`. De proxy-configuratie proxyt via variabelen, zodat `nginx -t` geen DNS-lookup naar `api` of `web` nodig heeft.

De Docker-jobs praten met de Docker-daemon over TLS. De clientvariabelen staan alleen op die jobs:

| Variabele | Waarde |
|---|---|
| `DOCKER_HOST` | `tcp://docker:2376` |
| `DOCKER_TLS_CERTDIR` | `/certs` |
| `DOCKER_TLS_VERIFY` | `1` |
| `DOCKER_CERT_PATH` | `$DOCKER_TLS_CERTDIR/client` |

De service `docker:27-dind` heeft de alias `docker` en `HEALTHCHECK_TCP_PORT=2376`. De job start `docker info` pas nadat de clientcertificaten in `/certs/client` aanwezig zijn.

## Branches

| Branch | Pipeline | Images | Opdracht aan de server |
|---|---|---|---|
| `develop` | Bij iedere push | Publiceren na geslaagde stages | `deploy:test` automatisch |
| `main` | Bij iedere push | Publiceren na geslaagde stages | `deploy:production` alleen handmatig |
| Andere branch | Alleen zolang er een merge request openstaat | Niet publiceren | Geen |

Een push naar `develop` of `main` start één branch-pipeline, ook wanneer daar een merge request voor openstaat. De bijbehorende merge-request-pipeline wordt niet aangemaakt. Een featurebranch krijgt alleen een merge-request-pipeline. Een featurebranch zonder merge request start geen pipeline.

`deploy:test` gebruikt environment `test`, resource group `weddingbank-test` en package `weddingbank-test`. `deploy:production` gebruikt environment `production`, resource group `weddingbank-production` en package `weddingbank-production`. De production-job heeft `when: manual` en `allow_failure: false`, dus de pipeline blijft geblokkeerd tot iemand de job start.

`main` accepteert alleen merges door Maintainers. `develop` accepteert pushes van Developers en Maintainers, en merges alleen door Maintainers. Op GitLab.com Free is een protected environment niet beschikbaar. Iedereen die een pipeline mag starten, kan daardoor de productieknop indrukken. Het Reporter-account van de agent kan dat niet.

## Image-tags en manifests

De registry is de projectregistry van GitLab, via `$CI_REGISTRY_IMAGE`.

| Image | Tag |
|---|---|
| `$CI_REGISTRY_IMAGE/api` | `$CI_COMMIT_SHA` |
| `$CI_REGISTRY_IMAGE/web` | `$CI_COMMIT_SHA` |
| `$CI_REGISTRY_IMAGE/proxy` | `$CI_COMMIT_SHA` |

`$CI_COMMIT_SHA` is de volledige commit-id en de enige tag. De pipeline zet geen `latest`. Inloggen voor de push gebruikt `$CI_REGISTRY`, `$CI_REGISTRY_USER` en `$CI_REGISTRY_PASSWORD`. Dat wachtwoord is het tijdelijke CI-jobtoken.

Het manifest wordt geüpload met `JOB-TOKEN` naar:

- `weddingbank-test/<commit>/manifest.json`
- `weddingbank-production/<commit>/manifest.json`

De inhoud bevat het environment, het projectpad, de commit en de drie images als `registry.gitlab.com/evsb-group/weddingbank/{api,web,proxy}@sha256:…`. Er staan geen wachtwoorden in. Bestaat hetzelfde bestand al en is de inhoud gelijk, dan slaagt de upload alsnog. Dat maakt een rollback-herhaling mogelijk.

De digest-artifact blijft 90 dagen beschikbaar. De productieknop moet binnen die periode worden ingedrukt. Daarna mist de job de digests en publiceert hij niets.

## Wat de server doet

`infra/deploy/weddingbank-agent.sh` draait apart voor `test` en `production`. Elke minuut doet het proces uitgaand HTTPS en verder niets.

Het leest met het `read_api`-token:

```text
GET https://gitlab.com/api/v4/projects/evsb-group%2Fweddingbank/deployments?environment=test&status=success&order_by=id&sort=desc&per_page=1
PRIVATE-TOKEN: <read_api token>
```

PRODUCTIE gebruikt dezelfde URL met `environment=production`. Het token wordt niet gelogd.

De agent accepteert het eerste item alleen wanneer status, environment, ref, jobnaam en SHA kloppen:

| Agent | Environment | Ref | Job | Package |
|---|---|---|---|---|
| test | `test` | `develop` | `deploy:test` | `weddingbank-test` |
| production | `production` | `main` | `deploy:production` | `weddingbank-production` |

Het manifest wordt daarna met het deploytoken opgehaald via header `DEPLOY-TOKEN`. Images worden met datzelfde token gepulld. Het `read_api`-token wordt daar niet voor gebruikt.

Een deployment-id die niet hoger is dan de laatst verwerkte id van die omgeving wordt genegeerd. Een oudere job die later klaar is, vervangt daardoor geen nieuwere release. Een mislukte healthcheck bewaart die id en de SHA. Dezelfde opdracht en dezelfde SHA worden niet opnieuw gepulld.

Een rollback in GitLab maakt een nieuwe deployment met een hogere id en de oudere SHA. De agent past die toe wanneer de SHA niet de eerder mislukte SHA is. Een mislukte healthcheck zet eerst de vorige digests van dezelfde omgeving terug.

Migraties blijven uit zolang `WEDDINGBANK_RUN_MIGRATIONS=false`.

## GitLab-instellingen

Doe dit in GitLab. Maak de tokens aan en bewaar ze buiten de repository.

1. Zet CI/CD, de Container Registry en de Package Registry aan.
2. Laat de jobs zonder runner-tag. Ze komen op `saas-linux-small-amd64`. Runners met een `saas-linux-*-*` tag ondersteunen Docker-in-Docker al. De tag `gitlab-org` doet dat niet en staat niet in dit bestand.
3. Bevestig dat het CI-jobtoken naar de eigen Container Registry mag pushen en Generic packages mag uploaden. Dat is de standaard voor hetzelfde project.
4. Zet onder Settings → CI/CD → General pipelines de optie Prevent outdated deployment jobs aan. Laat Allow job retries for rollback deployments aan, zodat een environment-rollback de oude job opnieuw mag starten.
5. Maak een apart GitLab-account, voeg het toe aan `evsb-group/weddingbank` als Reporter, en maak een personal access token met alleen `read_api`.
6. Maak een project deploy token. Noem het niet `gitlab-deploy-token`, want die naam injecteert het token in elke CI-job. Geef het alleen `read_registry` en `read_package_registry`. Kopieer de username en het token die GitLab één keer toont.
7. Nadat de eerste pipeline de resource groups heeft aangemaakt, zet een Maintainer beide op `newest_ready_first`. Het Reporter-token mag deze schrijfactie niet doen.

```text
PUT https://gitlab.com/api/v4/projects/evsb-group%2Fweddingbank/resource_groups/weddingbank-test
process_mode=newest_ready_first
```

Herhaal dat voor `weddingbank-production`.

8. Een cleanup policy mag de commit-SHA-tags niet verwijderen zolang die release nog teruggezet moet kunnen worden.
9. Voeg geen webhook of andere inkomende verbinding naar `10.120.10.100` toe.

## Debian-installatie

Doe dit pas na review van deze bestanden. Installeer niets vanuit de ontwikkelmachine en start de agent niet met de oude GitHub-configuratie.

1. Installeer Docker Engine en de Compose-plugin. Sta uitgaand TCP 443 toe naar `gitlab.com` en `registry.gitlab.com`. Stel SSH en de Docker-API niet open voor deployment.
2. Kopieer `infra/compose/docker-compose.yml` naar `/opt/weddingbank/test/docker-compose.yml` en `/opt/weddingbank/prod/docker-compose.yml`.
3. Maak de env-bestanden, mode `0600`, vanuit de voorbeelden:
   - `/opt/weddingbank/test/.env` vanuit `infra/compose/env/test.env.example`
   - `/opt/weddingbank/prod/.env` vanuit `infra/compose/env/prod.env.example`
   Kies per bestand een ander databasewachtwoord. Laat `WEDDINGBANK_RUN_MIGRATIONS=false`.
4. Maak de credentialbestanden, mode `0600`:
   - `/opt/weddingbank/deploy/gitlab-api.env` vanuit `infra/compose/env/gitlab-api.env.example`
   - `/opt/weddingbank/deploy/gitlab-registry.env` vanuit `infra/compose/env/gitlab-registry.env.example`
5. Kopieer `infra/deploy/weddingbank-agent.sh` en `infra/deploy/weddingbank-backup.sh` naar `/usr/local/sbin/` en maak ze uitvoerbaar.
6. Installeer de unit-bestanden uit `infra/deploy/systemd/` en zet alleen de twee agent-timers en, wanneer back-ups gewenst zijn, de backup-timer aan. Zet `WEDDINGBANK_AGENT_TEST` niet in de units.
7. Bevestig dat Nginx Proxy Manager `test.wedding-bank.evsb.io` naar `10.120.10.100:8081` stuurt en `wedding-bank.evsb.io` naar `10.120.10.100:8080`. De deployment wijzigt Proxy Manager niet.
8. Controleer daarna `/var/lib/weddingbank/test/status.json` en `/var/lib/weddingbank/prod/status.json`. Een groene GitLab-job is niet die controle.

TEST en PRODUCTION houden gescheiden Compose-projecten, volumes, databases, poorten, env-bestanden en state. De agent weigert het verkeerde project, het verkeerde adres en een placeholder-wachtwoord.
