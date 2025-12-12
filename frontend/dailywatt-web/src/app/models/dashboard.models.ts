export type Granularity = "day" | "month" | "year";

export interface GetTimeSeriesRequest {
  meterId?: string;
  from: string;
  to: string;
  granularity: Granularity;
  withWeather?: boolean;
}

export interface ConsumptionPoint {
  timestampUtc: string;
  kwh: number;
}

export interface WeatherDay {
  date: string;
  tempAvg: number;
  tempMin: number;
  tempMax: number;
  source: string;
}

export interface Summary {
  totalKwh: number;
  avgKwhPerDay: number;
  maxDayKwh: number;
  maxDay?: string | null;
}

export interface TimeSeriesResponse {
  consumption: ConsumptionPoint[];
  weather?: WeatherDay[];
  summary: Summary;
}
