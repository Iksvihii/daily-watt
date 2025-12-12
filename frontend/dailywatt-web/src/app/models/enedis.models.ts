export interface SaveCredentialsRequest {
  login: string;
  password: string;
}

export interface CredentialsResponse {
  login: string;
  updatedAt: string;
}

export interface CreateImportJobRequest {
  meterId: string;
  fromUtc: string;
  toUtc: string;
}

export type ImportJobStatus = "Pending" | "Running" | "Completed" | "Failed";

export interface ImportJob {
  id: string;
  createdAt: string;
  completedAt?: string;
  status: ImportJobStatus;
  errorCode?: string;
  errorMessage?: string;
  importedCount: number;
}

export interface EnedisStatus {
  configured: boolean;
  meterNumber?: string;
  updatedAt?: string;
}

export interface EnedisMeter {
  id: string;
  prm: string;
  label?: string;
  city?: string;
  latitude?: number;
  longitude?: number;
  isFavorite: boolean;
}

export interface CreateMeterRequest {
  prm: string;
  label?: string;
  city?: string;
  latitude?: number;
  longitude?: number;
}

export interface UpdateMeterRequest {
  label?: string;
  city?: string;
  latitude?: number;
  longitude?: number;
}
