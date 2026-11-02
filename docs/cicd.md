# CI/CD

De actieve pipeline staat in [deployment/gitlab-cicd.md](deployment/gitlab-cicd.md). Het onderstaande beschrijft de eerdere GitHub-opzet. Die workflows blijven in de repository staan en worden niet gebruikt. De pull-agent leest GitLab, niet GitHub.

Test volgt `develop`. Productie volgt `main` en start alleen na een handmatige goedkeuring. GitHub-runners bouwen en publiceren images. De Debian-server haalt die images zelf op. Er is geen self-hosted runner en geen SSH-verbinding vanuit GitHub naar de server.

## Branches

| Branch | Omgeving | Deploy |
|---|---|---|
| `develop` | Test | Automatisch nadat CI op een push slaagt |
| `main` | Productie | Alleen via de workflow `Deploy production` |

Werk op een featurebranch en open een pull request naar `develop`. CI draait op die pull request, maar publiceert geen images.

Promotie naar productie is een pull request van `develop` naar `main`. Een push naar `main` publiceert images en draait CI. Die push start geen productiedeploy. Daarna start iemand op `main` de workflow `Deploy production`. De server ziet die geslaagde run en deployt de digests uit het approval-artifact.

`develop` bestaat nog niet op GitHub. Die branch wordt aangemaakt vanaf de commit die deze CI/CD-bestanden bevat, en daarna gepusht. Er is lokaal nog geen commit van deze bestanden, en `gh` is op deze machine niet ingelogd.

## Continuous integration

De workflow `CI` draait op elke pull request en op elke push naar `develop` of `main`:

- backend restore, build en test
- frontend `npm ci`, typecheck, lint en production-build
- controle van de deployscripts en van het server-compose-bestand
- Docker-build van `weddingbank-api`, `weddingbank-web` en `weddingbank-proxy`

Images worden alleen gepubliceerd als al die jobs slagen én de gebeurtenis een push naar `develop` of `main` is. De tag is `sha-<commit>` op GHCR. Er wordt geen `latest` gepubliceerd. De workflow zet daarna een artifact `release-manifest` klaar met de drie image-digests. Dat artifact blijft 90 dagen beschikbaar.

## Pull-agent

`infra/deploy/weddingbank-agent.sh` staat later op de server als `/usr/local/sbin/weddingbank-agent.sh`. Het script is vast: het voert geen workflowcode en geen git-checkout uit.

Elke minuut kijkt de testtimer naar de nieuwste geslaagde CI-push op `develop`. De productietimer kijkt alleen naar een geslaagde handmatige run van `Deploy production` op `main`. De agent downloadt het bijbehorende artifact, controleert de digests en draait `docker compose up`. Een mislukte healthcheck zet de vorige image-digests terug. Volumes worden niet verwijderd.

Zolang `WEDDINGBANK_RUN_MIGRATIONS=false` is, wordt er geen migratiecontainer gestart en maakt productie geen pre-migratiedump. Die stap staat klaar voor wanneer de applicatie later een `migrate`-service krijgt. Er is nu geen databasecode.

De deploymentstatus staat op de server in `/var/lib/weddingbank/<omgeving>/status.json` en in journald. De agent heeft geen schrijfrecht op GitHub. De goedkeuring voor productie is de geslaagde workflowrun en het artifact `production-approval`.

Tokens op de server:

| Bestand | Recht |
|---|---|
| `/opt/weddingbank/deploy/agent.env` | Fine-grained PAT, alleen deze repository, alleen `Actions: Read` en verplichte `Metadata: Read` |
| `/opt/weddingbank/deploy/ghcr.env` | Classic PAT met alleen `read:packages`, van een account dat alleen deze drie images mag lezen |

Geen van beide tokens kan code of images publiceren. Productiewachtwoorden blijven in `/opt/weddingbank/prod/.env`.

## Wat nog handmatig moet

GitHub:

1. Inloggen met `gh auth login` en deze bestanden committen.
2. `develop` aanmaken vanaf die commit en pushen naar `origin`.
3. Bij Actions de workflowrechten op lezen en schrijven zetten, zodat CI naar GHCR kan publiceren.
4. Na de eerste publicatie de drie packages privé houden en de tags immutable maken.
5. Een apart GitHub-account leesrecht geven op alleen die drie images, en daarmee een classic token met uitsluitend `read:packages` maken.
6. Een fine-grained token maken met alleen `Actions: Read` op deze repository.
7. Branch protection naar keuze: pull requests naar `develop` en `main`, en een groene CI als voorwaarde.

Debian, pas wanneer de tokens en wachtwoorden bestaan:

1. Docker Engine en de Compose-plugin, plus `jq`, `curl` en `unzip`.
2. Tijdzone `Europe/Brussels` als de nachtdump om 03:15 lokale tijd moet lopen.
3. Mappen `/opt/weddingbank/test`, `/opt/weddingbank/prod` en `/opt/weddingbank/deploy`, en `/var/lib/weddingbank`.
4. `docker-compose.yml` kopiëren naar de test- en productiemap.
5. `.env` vullen vanuit de voorbeelden. `replace-on-server` weigert de agent.
6. De scripts kopiëren naar `/usr/local/sbin` en de systemd-units installeren, nog niet starten zolang de tokens ontbreken.
7. Firewall: alleen Nginx Proxy Manager naar `10.120.10.100:8080` en `:8081`. PostgreSQL heeft geen hostpoort. Het publicatie-netwerk van de proxy is niet intern, omdat Docker anders geen hostpoort openzet. API en PostgreSQL blijven op interne netwerken.
8. NPM overschrijft `X-Forwarded-For` met het clientadres en stuurt websockets door. `wedding-bank.evsb.io` gaat naar poort 8080, `test.wedding-bank.evsb.io` naar poort 8081.

Er is vanuit deze repository geen deployment uitgevoerd.
