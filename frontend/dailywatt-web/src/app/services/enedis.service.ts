import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, interval, switchMap, takeWhile } from "rxjs";
import { environment } from "../../environments/environment";
import {
  CreateImportJobRequest,
  CreateMeterRequest,
  CredentialsResponse,
  EnedisMeter,
  EnedisStatus,
  ImportJob,
  SaveCredentialsRequest,
  UpdateMeterRequest,
} from "../models/enedis.models";

@Injectable({ providedIn: "root" })
export class EnedisService {
  private http = inject(HttpClient);

  getCredentials(): Observable<CredentialsResponse> {
    return this.http.get<CredentialsResponse>(
      `${environment.apiUrl}/api/enedis/credentials`
    );
  }

  saveCredentials(request: SaveCredentialsRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/api/enedis/credentials`,
      request
    );
  }

  getStatus(): Observable<EnedisStatus> {
    return this.http.get<EnedisStatus>(
      `${environment.apiUrl}/api/enedis/status`
    );
  }

  createImportJob(request: CreateImportJobRequest): Observable<ImportJob> {
    return this.http.post<ImportJob>(
      `${environment.apiUrl}/api/enedis/import`,
      request
    );
  }

  getImportJob(id: string): Observable<ImportJob> {
    return this.http.get<ImportJob>(
      `${environment.apiUrl}/api/enedis/import/${id}`
    );
  }

  pollJobUntilDone(id: string, intervalMs = 2000): Observable<ImportJob> {
    return interval(intervalMs).pipe(
      switchMap(() => this.getImportJob(id)),
      takeWhile(
        (job) => job.status === "Pending" || job.status === "Running",
        true
      )
    );
  }

  // Meter management
  getMeters(): Observable<EnedisMeter[]> {
    return this.http.get<EnedisMeter[]>(
      `${environment.apiUrl}/api/enedis/meters`
    );
  }

  createMeter(request: CreateMeterRequest): Observable<EnedisMeter> {
    return this.http.post<EnedisMeter>(
      `${environment.apiUrl}/api/enedis/meters`,
      request
    );
  }

  updateMeter(id: string, request: UpdateMeterRequest): Observable<void> {
    return this.http.put<void>(
      `${environment.apiUrl}/api/enedis/meters/${id}`,
      request
    );
  }

  deleteMeter(id: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/api/enedis/meters/${id}`
    );
  }

  setFavoriteMeter(id: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/api/enedis/meters/${id}/favorite`,
      {}
    );
  }

  uploadConsumptionFile(payload: {
    file: File;
    meterId: string;
  }): Observable<ImportJob> {
    const formData = new FormData();
    formData.append("file", payload.file);
    formData.append("meterId", payload.meterId);
    return this.http.post<ImportJob>(
      `${environment.apiUrl}/api/enedis/import/upload`,
      formData
    );
  }
}
