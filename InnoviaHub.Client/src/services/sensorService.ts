import { BASE_API_URL } from "../config/api.ts";

export type SensorDevice = {
    id: string;
    serial: string;
    model: string;
    status: string;
};

export type SensorOverview = {
    tenantSlug: string;
    devices: SensorDevice[];
};

// Shape of the "measurementReceived" event sent by Realtime.Hub
export type RealtimeMeasurement = {
    tenantSlug: string;
    deviceId: string;
    type: string;
    value: number;
    time: string;
};

const SENSORS_API_URL = `${BASE_API_URL}/api/Sensors`;

//Hämta tenant + enheter från DeviceRegistry (via InnoviaHub.Api)
export async function getSensorOverview(): Promise<SensorOverview> {
    const response = await fetch(SENSORS_API_URL, {
        credentials: "include",
    });

    if (!response.ok) {
        throw new Error(response.status === 502
            ? "DeviceRegistry svarar inte"
            : "Kunde inte hämta sensorer");
    }

    return response.json();
}
