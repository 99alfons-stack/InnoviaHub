import AdminStatusBadge from "./AdminStatusBadge";
import type { SensorTelemetry, TelemetryConnectionState } from "../hooks/useSensorTelemetry";

const connectionLabels: Record<TelemetryConnectionState, { label: string; color: string }> = {
  connecting: { label: "Ansluter till Realtime.Hub…", color: "#7a94aa" },
  connected: { label: "Realtid · ansluten", color: "#00d4aa" },
  reconnecting: { label: "Återansluter…", color: "#f59e0b" },
  disconnected: { label: "Anslutning saknas", color: "#f43f5e" },
};

const metricFormats: Record<string, { label: string; icon: string; format: (value: number) => string }> = {
  temperature: { label: "Temperatur", icon: "🌡", format: (value) => `${value.toFixed(1)}°C` },
  co2: { label: "CO₂", icon: "💨", format: (value) => `${Math.round(value)} ppm` },
};

export default function AdminSensors({ telemetry }: { telemetry: SensorTelemetry }) {
  const { devices, readings, connectionState, error, now, lastSeen, deviceStatus } = telemetry;
  const connection = connectionLabels[connectionState];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold" style={{ fontFamily: "Outfit, sans-serif", color: "#e2eaf2" }}>
          Sensorövervakning
        </h1>
        <span className="flex items-center gap-2 text-xs mono" style={{ color: connection.color }}>
          <span className={connectionState === "connected" ? "pulse-dot w-2 h-2 rounded-full" : "w-2 h-2 rounded-full"} style={{ background: connection.color }} />
          {connection.label}
        </span>
      </div>
      {error && (
        <div className="rounded-xl p-4 text-sm" style={{ background: "rgba(244,63,94,0.1)", border: "1px solid rgba(244,63,94,0.25)", color: "#f43f5e" }}>
          {error}
        </div>
      )}
      {!error && devices.length === 0 && connectionState === "connecting" && (
        <div className="text-sm" style={{ color: "#7a94aa" }}>Hämtar sensorer från DeviceRegistry…</div>
      )}
      <div className="grid md:grid-cols-2 gap-4">
        {devices.map((device) => {
          const deviceReadings = readings[device.id] ?? {};
          const status = deviceStatus(device.id);
          const seen = lastSeen(device.id);
          const types = Object.keys(deviceReadings).sort();

          return (
            <div key={device.id} className="rounded-xl p-4" style={{ background: "#0d1824", border: "1px solid #1e3347" }}>
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <div className="rounded-lg flex items-center justify-center text-xl" style={{ width: 40, height: 40, background: "rgba(0,212,170,0.1)" }}>
                    {types.length === 1 ? metricFormats[types[0]]?.icon ?? "◈" : "◈"}
                  </div>
                  <div>
                    <div className="text-sm font-semibold" style={{ color: "#e2eaf2" }}>{device.model}</div>
                    <div className="text-xs mono" style={{ color: "#7a94aa" }}>{device.serial}</div>
                  </div>
                </div>
                <AdminStatusBadge status={status} />
              </div>
              <div className="mt-3 flex items-end justify-between gap-4">
                <div className="flex gap-6">
                  {types.length === 0 && <span className="text-sm" style={{ color: "#7a94aa" }}>Väntar på första avläsning…</span>}
                  {types.map((type) => {
                    const metric = metricFormats[type];
                    const reading = deviceReadings[type];
                    return (
                      <div key={type}>
                        <div className="text-xs" style={{ color: "#7a94aa" }}>{metric?.label ?? type}</div>
                        <div className="mono text-xl" style={{ color: status === "offline" ? "#7a94aa" : "#00d4aa" }}>
                          {metric ? metric.format(reading.value) : reading.value}
                        </div>
                      </div>
                    );
                  })}
                </div>
                {seen !== null && (
                  <span className="text-xs mono" style={{ color: "#7a94aa" }} title={new Date(seen).toLocaleString("sv-SE")}>
                    {Math.max(0, Math.round((now - seen) / 1000))} s sedan
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
