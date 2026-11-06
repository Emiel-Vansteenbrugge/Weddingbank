# 0001. Architectuur van The Wedding Bank

Status: aanvaard

## Context

The Wedding Bank is één applicatie met een publiek deelnemersgedeelte en één beheeromgeving. Test en productie draaien op dezelfde Debian-VM en blijven volledig gescheiden. Dit document legt de aanvaarde technische keuzes vast. Accounts, MFA-schermen, spel en beheer worden pas gebouwd in latere stappen.

## Stack en structuur

- Backend: .NET 10, vier projecten. `WeddingBank.Domain`, `WeddingBank.Application`, `WeddingBank.Infrastructure` en `WeddingBank.Api`. Geen extra architectuurframeworks.
- Frontend: React, TypeScript en Vite. In ontwikkeling proxyt Vite `/api` en `/hubs` naar de API.
- Database: PostgreSQL 18, officieel image `postgres:18`. Het volume staat op `/var/lib/postgresql`.
- Eén monorepo. Logging gaat naar stdout. Secrets staan alleen in env-bestanden op de server.

De API past het schema niet aan bij het opstarten. Migraties draaien in een eenmalige container met hetzelfde image.

## Omgevingen

Productie volgt `main` op GitHub en wordt gedeployed door de self-hosted runner op de VM. Die bouwt de images lokaal en start Compose. Het env-bestand met de secrets blijft op de server.

PostgreSQL publiceert in test en productie geen hostpoort. Lokaal publiceert `docker-compose.dev.yml` PostgreSQL alleen op `127.0.0.1:5432`. Dat bestand wordt niet gebruikt op de server.

Per omgeving zijn er drie Docker-netwerken:

- `data` is intern en bevat PostgreSQL en de API.
- `services` is intern en bevat de API, de frontend en de proxy.
- `edge` is niet intern en bevat alleen de proxy, omdat Docker een hostpoort alleen vanaf zo'n netwerk publiceert.

De proxy en de frontend kunnen PostgreSQL niet bereiken. PostgreSQL heeft geen uitgaande internetroute. De API wel, via een apart netwerk, omdat het contactformulier Resend aanroept. De proxy heeft uitgaand verkeer technisch ook, omdat zijn publicatie-netwerk niet intern is. De host bouwt de images zelf.

## Deployment

GitHub Actions test op een gehoste runner. De deploy-job draait op de self-hosted runner van de VM, bouwt de images daar en start Compose. Productiesecrets blijven in het env-bestand op de server. De operationele stappen staan in [docs/deployment/github.md](../deployment/github.md).

## Database, back-up en herstel

Het runtime-databaseaccount heeft alleen de rechten die de applicatie nodig heeft. Migraties gebruiken een apart account. Back-ups gebruiken de database-superuser via `docker exec`.

Vóór elke productiemigratie en elke nacht wordt een `pg_dump` gemaakt. Eén keer per week wordt de nieuwste dagdump teruggezet in een wegwerpdatabase, buiten de test- en productienetwerken.

Een herstel naar productie schrijft eerst naar een tijdelijke database. De live database wordt pas hernoemd nadat connectiviteit en `__EFMigrationsHistory` op die tijdelijke database kloppen, inclusief het migratie-id uit de sidecar van de dump. Faalt een stap, dan blijft de bestaande database in gebruik. Na een geslaagde wissel blijft de vervangen database staan tot een operator die zelf verwijdert.

Een slechte applicatieversie gaat terug naar de vorige image-digest. De database wordt daarbij niet overschreven.

## Beheeromgeving

Er is één beheeromgeving: `https://wedding-bank.evsb.io/admin`. Dezelfde URL werkt thuis en buitenshuis. Test gebruikt `https://test.wedding-bank.evsb.io/admin`.

Beheerders zijn aparte accounts met alleen de rol `Administrator`. MFA via TOTP is verplicht. De beheer-API biedt alleen spelbeheer. Er is geen shell, Docker-socket, vrije SQL of serverbeheer. SSH op de VM blijft een hosttaak buiten de applicatie.

## CSRF

Authenticatie gebruikt een `HttpOnly` cookie met `Secure` en `SameSite=Lax`. `SameSite=Lax` is een extra browserbeperking. Wijzigende verzoeken (`POST`, `PUT`, `PATCH`, `DELETE`) eisen daarnaast:

- een geldig ASP.NET Core-antiforgerytoken in een header;
- een `Origin`-header van de site zelf, of van een toegestane host. Http en https op de standaardpoort van die host gelden allebei.

Ontbreekt `Origin` of hoort die bij een andere site, dan wordt het verzoek geweigerd. Er is geen terugval op `Referer`. GET-verzoeken wijzigen geen toestand. De deelnemers- en beheerinterface blijven same-origin achter de proxy, waardoor geen open CORS-policy nodig is.

## Client-IP achter de proxies

Het pad is browser, Nginx Proxy Manager, de interne proxy, de API. De API heeft geen hostpoort.

NPM overschrijft `X-Forwarded-For` met `$remote_addr` en zet `X-Forwarded-Proto`. Een door de browser meegestuurde forwarded header wordt daar vervangen. De interne proxy geeft alleen die waarde door. De hostfirewall laat op poort 8080 en 8081 alleen NPM toe.

ASP.NET Core verwerkt forwarded headers alleen als de TCP-verbinding uit het vertrouwde edge-netwerk komt, met een limiet van één hop. Komt de verbinding ergens anders vandaan, dan worden de headers genegeerd. Rate limiting en het auditlog gebruiken het IP-adres na deze middleware, niet de ruwe header.

## MFA en sessies

Een beheerderssessie ontstaat pas nadat het wachtwoord en de TOTP-code allebei kloppen. Het tussencookie van de MFA-stap geeft geen toegang tot beheer of deelnemersfuncties. Zolang MFA niet is afgerond, blijven alle beheerroutes geblokkeerd.

De sessie eindigt na 30 minuten inactiviteit. Sliding expiration verlengt die idle-tijd, maar nooit voorbij 8 uur na het aanmaken van de volledige sessie. Uitloggen, een wachtwoordwijziging of het intrekken van sessies vernieuwt de security stamp, waarna bestaande cookies ongeldig zijn.

Vijf mislukte wachtwoord- of TOTP-pogingen vergrendelen het account 15 minuten. Dezelfde endpoints hebben een rate limit per IP.

TOTP-geheimen staan versleuteld in PostgreSQL. De ASP.NET Data Protection-sleutels waarmee die geheimen en de sessiecookies beschermd zijn, staan op een eigen volume per omgeving. Dat volume overleeft een nieuwe container, staat niet in het image en niet in git.

## Auditlog

De applicatie schrijft beheeracties, aanmeldingen, lockouts en MFA-gebeurtenissen zelf weg. Er is geen applicatiefunctie om die rijen te wijzigen of te verwijderen. Het runtime-databaseaccount mag op de audittabel alleen invoegen en lezen.

Dat is beveiliging op applicatieniveau. Het is geen onveranderbare opslag. De migratierol, de database-superuser, een databaseherstel en iedereen met toegang tot de host kunnen de rijen wel wijzigen. Een herstel vervangt het auditlog door de inhoud van de dump. Deze opzet claimt geen write-once-opslag.

## Spelreset (nog niet gebouwd)

Dit is een vastgelegde eis, geen geïmplementeerde functie. De reset is alleen voor een bevoegde beheerder en vraagt een expliciete bevestiging voordat iets gebeurt.

Een reset geeft Juul en Amber allebei een schone speelronde. Hij wist deelnemersvoortgang: voltooide opdrachten, inzendingen, puzzelvoortgang, gelezen berichten, hints en andere mededelingen, telefoonverzoeken en overige speltoestand. Accounts, wachtwoorden, dossiers, documenten, quests, taken, aanvaarde antwoorden en ontgrendelregels blijven staan. Het auditlog blijft staan en krijgt een regel dat de reset is uitgevoerd.

Dit is geen databaseherstel en geen testomgeving leegmaken. De spelinhoud zelf wordt niet verwijderd of herschreven. De bedoeling is dat het huis het spel kan uitspelen, de oplossingen kan nakijken, één keer kan resetten, en de deelnemers op de trouwdag een schone ronde geeft.

## CI/CD

De operationele keten staat in [docs/deployment/github.md](../deployment/github.md). Een push naar `main` deployt productie.

Accounts, MFA, spel, beheer en databasemigraties volgen later.
