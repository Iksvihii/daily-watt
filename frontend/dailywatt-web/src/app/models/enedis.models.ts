export interface SaveCredentialsRequest {
  login: string;
  password: string;
  meterNumber: string;
  address?: string;
  latitude?: number;
  longitude?: number;
}

export interface CreateImportJobRequest {
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
