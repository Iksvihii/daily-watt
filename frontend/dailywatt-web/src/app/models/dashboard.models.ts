export type Granularity = "30min" | "hour" | "day";

export interface GetTimeSeriesRequest {
  from: string;
  to: string;
  granularity: Granularity;
  startDate?: string;
  endDate?: string;
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
