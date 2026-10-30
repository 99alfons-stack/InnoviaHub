# Innovia Hub

Innovia Hub är en webbapp för att boka resurser i ett kontorshus, till exempel mötesrum, skrivbord och utrustning.
Medlemmar loggar in och bokar lediga tider i ett bokningsflöde i flera steg. Administratörer hanterar resurser,
resurstyper, medlemmar och bokningar i en egen adminvy. Bokningar uppdateras i realtid via SignalR. Till systemet hör
också en AI-chatt som svarar på frågor om lediga tider och en sensorvy som visar live-data från Innovia IoT.

## Tekniker och ramverk

| Del | Tekniker |
|---|---|
| Backend (`InnoviaHub.Api`) | .NET 10, ASP.NET Core Web API, ASP.NET Core Identity (cookie-inloggning, rollerna Admin/Member), SignalR, OpenAPI + Scalar |
| Dataåtkomst (`InnoviaHub.DataAccess`) | Entity Framework Core 10, Npgsql, repository-mönster |
| Delade modeller (`InnoviaHub.Shared`) | DTO:er och enums som används av API:t |
| Databas | PostgreSQL 17 i Docker (Docker Compose) |
| Frontend (`InnoviaHub.Client`) | React 19, TypeScript, Vite, Tailwind CSS 4, `@microsoft/signalr` |
| AI | OpenAI Responses API (`gpt-4o-mini`) |
| IoT (`innovia-iot`) | .NET 8-tjänster: DeviceRegistry.Api, Ingest.Gateway, Realtime.Hub, Edge.Simulator, samt MQTT (Eclipse Mosquitto), PostgreSQL och Redis i Docker |

## Projektstruktur

```
InnoviaHub.Api/         Controllers, services, SignalR-hub, seeding av roller och admin
InnoviaHub.DataAccess/  DbContext, entiteter, repositories och migrationer
InnoviaHub.Shared/      DTO:er och enums
InnoviaHub.Client/      React-klienten
innovia-iot/            Innovia IoT-plattformen (med våra ändringar)
compose.yaml            PostgreSQL för Innovia Hub
```

## Köra projektet lokalt

### Förutsättningar

- .NET SDK 10 (och .NET SDK 8 för Innovia IoT)
- Node.js och npm
- Docker Desktop
- En OpenAI API-nyckel

Kör alla kommandon från projektets rotmapp om inget annat står.

### 1. Starta databasen

Skapa en `.env`-fil i rotmappen och fyll i egna värden:

```env
CONTAINER_NAME=<container_name>
DATABASE=<database_name>
USERNAME=<username>
PASSWORD=<password>
```

```powershell
docker compose up -d
```

Databasen körs på port `5433`.

### 2. Konfigurera och starta API:t

Sätt miljövariablerna i samma terminalfönster som API:t ska startas i. Använd samma värden som i `.env`-filen.
`ADMIN_EMAIL` och `ADMIN_PASSWORD` är valfria och används för att skapa den första administratören.

```powershell
$env:SQL_ConnectionString = "Host=localhost;Port=5433;Database=<database_name>;Username=<username>;Password=<password>"
$env:ADMIN_EMAIL = "admin@example.com"
$env:ADMIN_PASSWORD = 'Exempel1234!'
```

> **Krav på `ADMIN_PASSWORD`:** minst 6 tecken, en stor bokstav, en liten bokstav, en siffra och ett specialtecken.
> Använd enkla citattecken (`'...'`) i PowerShell så att `$` inte tolkas som en variabel.

Spara OpenAI-nyckeln med user-secrets. Den lagras utanför repot och följer inte med till GitHub.
API:t startar inte utan nyckeln.

```powershell
dotnet user-secrets set OPENAI_API_KEY "<din_nyckel>" --project InnoviaHub.Api
```

Starta API:t:

```powershell
dotnet run --project InnoviaHub.Api --launch-profile http
```

Migrationerna körs automatiskt när API:t startar, och roller och admin skapas samtidigt.
API:t körs på `http://localhost:5193` och Scalar/OpenAPI finns på `http://localhost:5193/scalar`.

### 3. Starta klienten

Skapa en `.env`-fil i `InnoviaHub.Client`:

```env
VITE_API_URL=http://localhost:5193
VITE_REALTIME_HUB_URL=http://localhost:5103/hub/telemetry
```

Öppna ett nytt terminalfönster:

```powershell
Set-Location InnoviaHub.Client
npm install
npm run dev
```

Klienten körs på `http://localhost:5173`.

### 4. Starta Innovia IoT (för sensorvyn)

Det här steget behövs bara för fliken **Sensorer** i adminvyn. Kör varje `dotnet run` i ett eget terminalfönster:

```powershell
cd innovia-iot
docker compose -f deploy/docker-compose.yml up -d
dotnet run --project src/DeviceRegistry.Api   # :5101
dotnet run --project src/Realtime.Hub         # :5103
dotnet run --project src/Ingest.Gateway       # :5102 (starta efter Realtime.Hub)
.\scripts\seed-devices.ps1                    # registrerar tenanten innovia och 10 enheter
dotnet run --project src/Edge.Simulator
```

Adressen till DeviceRegistry och tenanten ställs in under `Iot` i `InnoviaHub.Api/appsettings.json`.
IoT-databasen saknar volym, så kör `seed-devices.ps1` igen efter `docker compose down`.

### Vanliga kommandon

```powershell
docker compose down                              # stoppa databasen
dotnet build InnoviaHub.slnx                     # bygg API:t
npm run build --prefix InnoviaHub.Client         # bygg klienten
npm run lint --prefix InnoviaHub.Client          # lint på klienten
dotnet tool install --global dotnet-ef           # installera EF CLI vid behov
```

HTTP-anrop för att testa API:t finns i `InnoviaHub.Api/Http` och kan köras med REST Client i VS Code.

## Funktioner

- Inloggning med rollerna Admin och Member
- Byte av lösenord vid första inloggningen
- Bokningsflöde i flera steg: välj resurstyp, datum och tid, och bekräfta
- Realtidsnotiser om bokningar via SignalR, med toast-meddelanden
- Adminvy med dashboard och hantering av resurser, resurstyper, zoner, medlemmar och bokningar

## Vidareutvecklade funktioner

De här funktionerna har vi lagt till utöver grundprojektet:

### AI-chatt för tillgänglighet
- En chattbubbla (`ChatBubble.tsx`) som visas för alla inloggade användare.
- Endpointen `POST /api/Chat` skickar frågan till OpenAI (`gpt-4o-mini`) via Responses API.
- `AvailabilityContextService` skickar med alla aktiva resurser och bokningarna för de kommande 14 dagarna i svensk tid,
  så att chatten kan svara på vad som är ledigt utan att hitta på något.
- API-nyckeln lagras med user-secrets och skickas aldrig till klienten.

### Sensorer från Innovia IoT i realtid
- Ny flik **Sensorer** i adminvyn som visar alla enheter med senaste temperatur och CO₂.
- `GET /api/Sensors` (bara för admin) hämtar enhetslistan från DeviceRegistry.Api.
- Klienten tar emot mätvärden direkt från Realtime.Hub via SignalR (`useSensorTelemetry.ts`) och ansluter igen
  automatiskt om anslutningen bryts.
- En enhet som inte har rapporterat på 15 sekunder visas som **Ingen data**. Antalet sådana enheter visas som larm i menyn.
- Ändringar i Innovia IoT: simulatorn skickar data för 10 enheter, två buggar i Ingest.Gateway är fixade
  (enhetscachen och tidszoner), CORS för klienten och ett seed-skript för enheterna.

Mer detaljer finns i [Implementation.md](Implementation.md) och [innovia-iot/BUGS.md](innovia-iot/BUGS.md).
