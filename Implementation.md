# Implementation – Innovia IoT och AI-chatt i InnoviaHub

Det här dokumentet beskriver de två funktioner jag har lagt till i InnoviaHub:

1. **Innovia IoT** – hur jag har kopplat ihop InnoviaHub med Innovia IoT, vilka ändringar jag har gjort
   i båda projekten och hur jag uppfyller kraven för G.
2. **AI-chatt** – en chatt där användaren kan fråga vad som är ledigt och få svar från OpenAI
   baserat på resurserna och bokningarna i databasen.

# Del 1: Innovia IoT

## Översikt

```
Edge.Simulator ──MQTT──▶ Mosquitto ──▶ Ingest.Gateway ──▶ PostgreSQL
                                             │
                                             └──SignalR──▶ Realtime.Hub ──SignalR──▶ InnoviaHub.Client
                                                                                        ▲
DeviceRegistry.Api ◀──HTTP── InnoviaHub.Api (GET /api/Sensors) ◀──HTTP──────────────────┘
```

- **Enhetslistan** hämtas av mitt API (`InnoviaHub.Api`) från `DeviceRegistry.Api` och skickas vidare till klienten.
  Anropet går via mitt API så att bara inloggade administratörer kommer åt det och så att adressen till
  DeviceRegistry bara finns i serverns konfiguration.
- **Realtidsvärdena** tar klienten emot direkt från `Realtime.Hub` via SignalR (händelsen `measurementReceived`).

Allt visas under fliken **Sensorer** i adminvyn.

## Ändringar i InnoviaHub

### API (`InnoviaHub.Api`)
| Fil | Ändring |
|---|---|
| `Controllers/SensorsController.cs` | Ny endpoint `GET /api/Sensors` (kräver rollen Admin). Svarar 502 om DeviceRegistry inte svarar. |
| `Services/SensorService.cs`, `Services/Interfaces/ISensorService.cs` | Slår upp tenanten via slug och hämtar dess enheter från DeviceRegistry API. |
| `Program.cs` | Namngiven `HttpClient` (`deviceRegistry`) med 5 sekunders timeout. |
| `Extensions/ServiceCollectionExtensions.cs` | Registrerar `ISensorService`. |
| `appsettings.json` | Ny sektion `Iot` med `DeviceRegistryUrl` och `TenantSlug`. |

### Shared (`InnoviaHub.Shared`)
- `DTOs/Sensor/SensorOverviewDto.cs` och `SensorDeviceDto.cs` – det som skickas till klienten (tenant + enheter).

### Klient (`InnoviaHub.Client`)
| Fil | Ändring |
|---|---|
| `services/sensorService.ts` | Hämtar enhetslistan från `/api/Sensors`. |
| `hooks/useSensorTelemetry.ts` | Ansluter till Realtime.Hub, går med i tenantens grupp (`JoinTenant`) och sparar senaste värdet per enhet och mättyp. Går med i gruppen igen efter återanslutning. En enhet som inte rapporterat på 15 s räknas som offline. |
| `components/AdminSensors.tsx` | Visar alla enheter med senaste temperatur/CO₂, status och hur länge sedan senaste avläsningen var. Visar också anslutningsstatus mot hubben. |
| `components/AdminStatusBadge.tsx` | Nya statusar: *Väntar* och *Ingen data*. |
| `pages/AdminPage.tsx` | Anslutningen ligger på sidan i stället för i fliken, så den överlever byte av flik. Antalet larm i menyn = antal enheter utan data. |
| `components/adminData.ts` | Den hårdkodade låtsas-sensordatan är borttagen. |
| `config/api.ts` | `REALTIME_HUB_URL` (kan ändras med `VITE_REALTIME_HUB_URL`). |

## Ändringar i Innovia IoT

| Fil | Ändring |
|---|---|
| `src/Edge.Simulator/Program.cs` | Simulerar 10 enheter (`dev-101` – `dev-110`) i stället för en. Varje enhet skickar ett värde var 5:e sekund (kravet är minst var 10:e). Tidsstämplar skickas i UTC. |
| `src/Ingest.Gateway/Program.cs` | **Buggfix:** cachen för enhetsuppslag nycklades bara på tenant, så alla enheter fick den första enhetens id. Nu nycklas den på tenant + serienummer. **Buggfix:** tidsstämplar med annan tidszon än UTC (t.ex. svensk tid) kraschade sparningen – de görs nu om till UTC. |
| `src/Realtime.Hub/Program.cs` | CORS tillåter `http://localhost:5173` (InnoviaHub-klienten). |
| `scripts/seed-devices.ps1` | Nytt skript som registrerar tenanten `innovia` och de 10 enheterna i DeviceRegistry. Kan köras flera gånger. |
| `BUGS.md` | Lista över buggar jag hittade i Innovia IoT och vilka jag har fixat. |

## Så uppfyller jag kraven

| Krav | Lösning |
|---|---|
| Minst 10 registrerade enheter i DeviceRegistry | `scripts/seed-devices.ps1` registrerar tenanten `innovia` och 10 enheter: 4 med temperatur + CO₂, 3 med CO₂ och 3 med temperatur. |
| Alla enheter tar emot mock-data via Edge.Simulator | Simulatorn skickar data för alla 10 enheter var 5:e sekund. Buggfixarna i Ingest.Gateway gör att datan faktiskt sparas på rätt enhet. |
| Enheterna hämtas via DeviceRegistry API och visas i min portal | `SensorService` anropar `GET /api/tenants/by-slug/{slug}` och `GET /api/tenants/{id}/devices`. Fliken *Sensorer* visar listan. |
| Realtidsdata visas med SignalR + Realtime.Hub | `useSensorTelemetry` lyssnar på `measurementReceived` och uppdaterar korten direkt när ett nytt värde kommer in. |

## Köra allt

1. **Innovia IoT** (i `innovia-iot`):
   ```powershell
   cd deploy; docker compose up -d; cd ..
   dotnet run --project src/DeviceRegistry.Api   # :5101
   dotnet run --project src/Realtime.Hub         # :5103
   dotnet run --project src/Ingest.Gateway       # :5102  (starta efter Realtime.Hub)
   .\scripts\seed-devices.ps1                    # registrerar tenant + 10 enheter
   dotnet run --project src/Edge.Simulator
   ```
   Varje `dotnet run` körs i ett eget terminalfönster.
2. **InnoviaHub**: starta API och klient enligt [README.md](README.md).
3. Logga in som admin och öppna fliken **Sensorer**. Alla 10 enheter ska visa värden som uppdateras var 5:e sekund.

## Kända begränsningar

- IoT-databasen i `docker-compose.yml` saknar volym, så enheterna försvinner vid `docker compose down`.
  Kör `seed-devices.ps1` igen efteråt.
- Realtime.Hub har ingen inloggning: vem som helst som når hubben kan lyssna på tenantens data. Det och övriga
  kända brister i Innovia IoT finns beskrivna i `BUGS.md` i det projektet.

# Del 2: AI-chatt

## Översikt

```
ChatBubble (klient) ──POST /api/Chat──▶ ChatController ──▶ AvailabilityContextService ──▶ PostgreSQL
                                              │               (resurser + bokningar)
                                              └──POST /v1/responses──▶ OpenAI (gpt-4o-mini)
```

- Alla inloggade användare ser en chattbubbla nere i hörnet och kan ställa frågor, till exempel
  *"Är något mötesrum ledigt på fredag eftermiddag?"*.
- Mitt API bygger en textsammanfattning av alla aktiva resurser och bokningarna för de kommande 14 dagarna
  och skickar den till OpenAI tillsammans med frågan. På så sätt svarar chatten utifrån riktig data.
- Anropet till OpenAI går via mitt API, så API-nyckeln finns bara på servern och aldrig i klienten.

## Ändringar i InnoviaHub

### API (`InnoviaHub.Api`)
| Fil | Ändring |
|---|---|
| `Controllers/ChatController.cs` | Ny endpoint `POST /api/Chat` (kräver inloggning). Tar emot `{ question }`, skickar frågan till OpenAI Responses API och returnerar `{ answer }`. Svarar 400 vid tom fråga och 502 om OpenAI inte svarar. |
| `Services/AvailabilityContextService.cs`, `Services/Interfaces/IAvailabilityContextService.cs` | Bygger sammanfattningen: aktuell tid, varje aktiv resurs (typ, zon, kapacitet) och dess bokade tider. Tiderna görs om från UTC till svensk tid. |
| `Program.cs` | Läser `OPENAI_API_KEY` och skapar en namngiven `HttpClient` (`openai`) med nyckeln som Bearer-token. API:t startar inte om nyckeln saknas. |
| `Extensions/ServiceCollectionExtensions.cs` | Registrerar `IAvailabilityContextService`. |
| `InnoviaHub.Api.csproj` | `UserSecretsId` så att nyckeln kan sparas med `dotnet user-secrets`. |

### DataAccess (`InnoviaHub.DataAccess`)
- `BookingRepository.GetActiveInRangeAsync` – hämtar bokningar som inte är avbokade och som överlappar ett tidsintervall.

### Klient (`InnoviaHub.Client`)
| Fil | Ändring |
|---|---|
| `components/ChatBubble.tsx` | Chattfönster som öppnas från en bubbla. Visar konversationen, skickar frågan till `/api/Chat` och visar svaret eller ett felmeddelande. |
| `App.tsx` | Visar `ChatBubble` när användaren är inloggad. |

## Instruktioner till modellen

Chatten får instruktioner om att:
- svara kort och vänligt på svenska,
- bara använda tillgänglighetsdatan när den svarar om lediga eller bokade tider, och inte hitta på resurser eller tider,
- säga att den bara ser de kommande två veckorna om frågan gäller en senare tid,
- hänvisa till bokningssidan, eftersom den inte kan boka själv.

## Köra chatten

1. Spara OpenAI-nyckeln med user-secrets:
   ```powershell
   dotnet user-secrets set OPENAI_API_KEY "<din_nyckel>" --project InnoviaHub.Api
   ```
2. Starta API och klient enligt [README.md](README.md).
3. Logga in och klicka på chattbubblan.

## Kända begränsningar

- Varje fråga skickas för sig, så chatten minns inte tidigare frågor i samma konversation.
- Chatten ser bara de kommande 14 dagarna.
- Hela tillgänglighetsdatan skickas med varje fråga. Med många resurser och bokningar blir anropen större och dyrare.
