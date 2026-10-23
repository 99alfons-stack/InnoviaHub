# Kända buggar och brister – Innovia IoT

Genomgång gjord 2026-10-18 av hela kodbasen. Buggar markerade **✅ Verifierad** har reproducerats mot körande tjänster
(Docker Compose + alla fyra API-tjänster igång lokalt). Övriga är hittade genom kodgranskning.

Allvarlighetsgrad:
- 🔴 **Kritisk** – ger fel data eller gör att en central funktion inte fungerar
- 🟠 **Hög** – säkerhetsproblem eller krasch i vanliga situationer
- 🟡 **Medel** – felaktigt beteende i kantfall, saknad validering
- ⚪ **Låg** – kodkvalitet, konfiguration, dokumentation

---

## Översikt

| # | Tjänst | Bugg | Grad |
|---|---|---|---|
| 1 | Ingest.Gateway | ~~Cachen slår ihop alla enheter i en tenant till en~~ **FIXAD 2026-10-18** | 🔴 ✅ |
| 2 | Ingest.Gateway + Edge.Simulator | ~~Tidsstämplar med annan tidszon än UTC kraschar sparningen~~ **FIXAD 2026-10-18** | 🔴 ✅ |
| 3 | Ingest.Gateway | ~~Okända enheter accepteras (följd av #1)~~ **FIXAD 2026-10-18** | 🔴 ✅ |
| 4 | Ingest.Gateway | `apiKey` kontrolleras aldrig | 🟠 ✅ |
| 5 | Portal.Adapter | Ingen tenant-isolering – vem som helst kan läsa alla enheters data | 🟠 ✅ |
| 6 | Realtime.Hub | Vilken klient som helst kan skicka falska mätvärden och gå med i vilken tenant som helst | 🟠 |
| 7 | Ingest.Gateway | Kraschar vid start om Realtime.Hub inte är igång | 🟠 |
| 8 | Ingest.Gateway | Okänd tenant ger 500 istället för 404 | 🟡 ✅ |
| 9 | Ingest.Gateway | Cachen är inte trådsäker | 🟡 |
| 10 | Ingest.Gateway | Om SignalR-anropet misslyckas returneras 500 trots att datan är sparad | 🟡 |
| 11 | Ingest.Gateway | MQTT återansluter aldrig | 🟡 |
| 12 | Ingest.Gateway | Validering saknas för tidsstämpel och mättyp | 🟡 |
| 13 | DeviceRegistry.Api | Devices kan skapas under tenants som inte finns | 🟡 ✅ |
| 14 | DeviceRegistry.Api | Ingen unikhet på `slug` eller `serial` | 🟡 |
| 15 | DeviceRegistry.Api | Klienten kan sätta `Id` själv (overposting) | ⚪ |
| 16 | Portal.Adapter | Routen tar GUID men dokumentationen använder slug | 🟡 ✅ |
| 17 | Portal.Adapter | `/series` kräver `type`, `from`, `to` trots att README säger valfria | 🟡 ✅ |
| 18 | Portal.Adapter | `/rooms/{roomId}/latest` returnerar hårdkodad låtsasdata | 🟡 |
| 19 | Rules.Engine | Singleton-worker tar in scoped DbContexts | 🟡 |
| 20 | Docker | Ingen volym för databasen – data försvinner vid `docker compose down` | 🟡 |
| 21 | Docs | Dokumenterade endpoints som inte finns | ⚪ ✅ |
| 22 | Övrigt | Konfiguration, beroenden, `.gitignore`, tester | ⚪ |

---

## 🔴 Kritiska

### 1. Cachen i `DeviceRegistryClient` slår ihop alla enheter till en ✅
**Fil:** `src/Ingest.Gateway/Program.cs:241` och `:250`

```csharp
if (_cache.TryGetValue(tenantSlug, out var hit)) return hit;
...
_cache[tenantSlug] = ids;
```

Cachen nycklas bara på tenant-slug. Första enheten som skickar data efter att Ingest startat sparas i cachen,
och **alla efterföljande enheter i samma tenant får den första enhetens DeviceId**.

**Reproducerat:** skickade en mätning från `dev-101` och sedan en från `dev-102`. Resultat: `dev-101` hade 2 rader,
`dev-102` hade 0.

**Fix:** nyckla på både slug och serial, t.ex. `var key = $"{tenantSlug}:{deviceSerial}";`

> **✔ Fixad 2026-10-18:** cachen nycklas nu på `"{tenantSlug}:{deviceSerial}"`. Verifierat: en mätning till var och en
> av dev-101 … dev-110 gav exakt 1 rad per enhet.

---

### 2. Tidsstämplar som inte är UTC kraschar sparningen ✅
**Filer:** `src/Edge.Simulator/Program.cs:32`, `src/Ingest.Gateway/Program.cs:204`

Npgsql 8 kan bara skriva `DateTimeOffset` med offset 0 till `timestamp with time zone`. Edge.Simulator skickar
`DateTimeOffset.Now`, vilket på en svensk dator ger `+02:00` (sommartid) eller `+01:00`.

**Reproducerat:** `POST /ingest/http/innovia` med `"timestamp": "2026-10-18T11:00:00+02:00"` → **500**:

```
System.ArgumentException: Cannot write DateTimeOffset with Offset=02:00:00 to PostgreSQL type
'timestamp with time zone', only offset 0 (UTC) is supported.
```

Det betyder att **simulatorns data aldrig sparas** när den körs på en dator i svensk tidszon. Felet loggas bara som
`[MQTT] Handler error` och syns lätt inte.

**Fix:** normalisera i Ingest så att alla klienter fungerar: `Time = payload.Timestamp.ToUniversalTime()`.
Byt även `DateTimeOffset.Now` → `DateTimeOffset.UtcNow` i simulatorn.

> **✔ Fixad 2026-10-18:** `IngestService.ProcessAsync` gör `payload.Timestamp.ToUniversalTime()` innan sparning och
> realtidspublicering, och simulatorn skickar `DateTimeOffset.UtcNow`. Verifierat: `11:30:00+02:00` gav 202 och
> sparades som `09:30:00+00:00`.

---

### 3. Okända enheter accepteras ✅
**Fil:** `src/Ingest.Gateway/Program.cs:241`

Följd av #1: när tenanten väl finns i cachen görs inget uppslag av serial alls.

**Reproducerat:** `POST` med `"deviceId": "dev-999"` (finns inte i registret) → **202 Accepted**, och mätningen
sparades på `dev-101`.

**Fix:** löses av samma fix som #1.

> **✔ Fixad 2026-10-18** (via #1). Verifierat: `dev-999` avvisas nu och sparas inte. Svaret är fortfarande 500 i
> stället för 404 – det är bugg #8, som inte är åtgärdad.

---

## 🟠 Höga (säkerhet / krascher)

### 4. `apiKey` kontrolleras aldrig ✅
**Fil:** `src/Ingest.Gateway/Program.cs:186`

Validatorn kräver bara att `apiKey` inte är tom. Den jämförs aldrig mot något, och `Device` har inget fält för nyckel.
Alla som kan nå Ingest kan skicka data i vilken enhets namn som helst.

**Reproducerat:** `"apiKey": "x"` accepterades.

**Fix:** lägg till ett (hashat) `ApiKey`-fält på `Device` i DeviceRegistry och verifiera det i `ResolveAsync`.

---

### 5. Portal.Adapter har ingen tenant-isolering ✅
**Fil:** `src/Portal.Adapter/Program.cs:34`, `:47`, `:64`

Parametern `tenant` tas emot i alla endpoints men används aldrig i någon query. Det räcker att känna till
en enhets GUID.

**Reproducerat:** `GET /portal/<slumpad GUID>/devices/<dev-101>/measurements` returnerade enhetens data.

**Fix:** lägg till `&& m.TenantId == tenant` i alla queries.
(`docs/architecture.md` säger: *"Alla queries filtrerar på `tenant_id`"* – det stämmer inte idag.)

---

### 6. Realtime.Hub saknar all åtkomstkontroll
**Fil:** `src/Realtime.Hub/Program.cs:26-33`

- `PublishMeasurement` är en publik hub-metod. Vilken webbläsarklient som helst kan anropa den och skicka
  påhittade mätvärden till alla som lyssnar på en tenant.
- `JoinTenant` tar emot vilken slug som helst – man kan lyssna på andra tenants data.

**Fix:** kräv autentisering för publicering (t.ex. en delad nyckel/JWT för backend-tjänster) och koppla
`JoinTenant` till användarens behörighet.

---

### 7. Ingest.Gateway kraschar om Realtime.Hub inte är igång
**Fil:** `src/Ingest.Gateway/Program.cs:47-51`

`await hub.StartAsync()` har ingen try/catch. Startas Ingest före Realtime.Hub avslutas den direkt.
Rules.Engine gör samma sak rätt (`src/Rules.Engine/Program.cs:75-83`).

**Fix:** wrappa i try/catch och logga en varning, som i Rules.Engine. `WithAutomaticReconnect()` hjälper bara
*efter* en lyckad första anslutning.

---

## 🟡 Medel

### 8. Okänd tenant ger 500 istället för 404 ✅
**Fil:** `src/Ingest.Gateway/Program.cs:243-247`

`GetFromJsonAsync` kastar `HttpRequestException` vid 404 – den returnerar aldrig `null`, så `?? throw ...`
körs aldrig. Undantaget fångas inte och klienten får 500.

**Reproducerat:** `POST /ingest/http/finnsinte` → **500**.

**Fix:** använd `GetAsync` och kontrollera statuskoden, eller fånga felet och returnera `Results.NotFound()`.

### 9. Cachen är inte trådsäker
**Fil:** `src/Ingest.Gateway/Program.cs:230`

`static Dictionary` används samtidigt från HTTP-anrop och MQTT-meddelanden. Samtidiga skrivningar till en vanlig
`Dictionary` kan korrumpera den.

**Fix:** `ConcurrentDictionary`.

### 10. Mätningar sparas men klienten får 500 om SignalR fallerar
**Fil:** `src/Ingest.Gateway/Program.cs:211-217`

Datan sparas först, sedan publiceras den. Om hubben är nere kastar `InvokeAsync` och klienten får 500. En klient
som gör retry skapar då dubbletter.

**Fix:** fånga och logga fel vid realtidspubliceringen – den ska inte påverka svaret.

### 11. MQTT återansluter aldrig
**Fil:** `src/Ingest.Gateway/Program.cs:143-155`, `src/Edge.Simulator/Program.cs`

Om Mosquitto inte är igång när Ingest startar, eller om anslutningen bryts, prenumererar Ingest aldrig igen.
Felet loggas bara en gång. Simulatorn har samma problem (den kraschar vid tappad anslutning).

**Fix:** hantera `DisconnectedAsync` och återanslut/prenumerera igen, eller använd MQTTnet:s `ManagedMqttClient`.

### 12. Bristande validering i Ingest
**Fil:** `src/Ingest.Gateway/Program.cs:181-189`

- Saknas `timestamp` blir den `0001-01-01` och sparas så.
- Ingen kontroll av `metrics[].type` (tom sträng tillåts).
- `unit` tas emot men sparas aldrig.

### 13. Devices kan skapas under tenants som inte finns ✅
**Fil:** `src/DeviceRegistry.Api/Program.cs:32-36`

**Reproducerat:** `POST /api/tenants/<slumpad GUID>/devices` → **201 Created**.

**Fix:** kontrollera att tenanten finns, returnera 404 annars. Lägg gärna till en foreign key.

### 14. Ingen unikhet på slug eller serial
**Fil:** `src/DeviceRegistry.Api/Program.cs:72-77`

Två tenants kan få samma slug och två devices samma serial. `by-slug` och `by-serial` returnerar då en godtycklig
av dem (`FirstOrDefaultAsync` utan sortering).

**Fix:** unika index i `OnModelCreating` (`HasIndex(t => t.Slug).IsUnique()`,
`HasIndex(d => new { d.TenantId, d.Serial }).IsUnique()`) och returnera 409 Conflict vid dubblett.

### 15. Klienten kan sätta `Id` själv ⚪
**Fil:** `src/DeviceRegistry.Api/Program.cs:28`, `:32`

Entiteterna binds direkt från request-body. Skickar klienten med ett `id` som redan finns blir det 500
(duplicate key). Använd separata request-DTO:er.

### 16. Portal.Adapter tar GUID, dokumentationen använder slug ✅
**Fil:** `src/Portal.Adapter/Program.cs:34` m.fl.

Huvud-README:n och `docs/api-specs.md` visar `/portal/innovia/...`, men routerna kräver `Guid tenant`.

**Reproducerat:** `GET /portal/innovia/devices/<id>/measurements` → **400**.

**Fix:** bestäm en variant. Slug är konsekvent med Ingest och Realtime.Hub.

### 17. `/series` kräver alla query-parametrar ✅
**Fil:** `src/Portal.Adapter/Program.cs:34`

`type`, `from` och `to` är icke-nullbara, men `src/Portal.Adapter/README.md` säger att de är valfria.

**Reproducerat:** `GET .../series` utan parametrar → **400**.

### 18. `/rooms/{roomId}/latest` returnerar låtsasdata
**Fil:** `src/Portal.Adapter/Program.cs:28-31`

Svarar alltid med `co2 = 950` och `temperature = 22.4` oavsett rum. Det finns inte heller något `Room`-begrepp i
DeviceRegistry (bara ett `RoomId`-fält på `Device`).

### 19. Rules.Engine: singleton-worker med scoped DbContexts
**Fil:** `src/Rules.Engine/Program.cs:99`

`RulesWorker` (hosted service = singleton) tar in `RulesDbContext` och `IngestReadDbContext` i konstruktorn.
- I miljön `Development` stoppar DI-valideringen detta och **tjänsten startar inte**.
- I `Production` startar den, men samma DbContext lever för evigt: den är inte trådsäker, cachar gamla entiteter och
  växer i minne.

**Fix:** injicera `IServiceScopeFactory` och skapa ett nytt scope i varje loopvarv.

Rules.Engine saknar dessutom `appsettings.json`, så den hamnar på default-port 5000. Övrig funktionalitet är
ofärdig (se TODO i `src/Rules.Engine/README.MD`) och räknas inte som buggar här.

### 20. Databasen saknar volym
**Fil:** `deploy/docker-compose.yml`

`db` har ingen namngiven volym. `docker compose down` (eller att containern tas bort) raderar all data.

**Fix:**
```yaml
  db:
    volumes:
      - pgdata:/var/lib/postgresql/data
volumes:
  pgdata:
```

---


