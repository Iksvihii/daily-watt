import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, interval, switchMap, takeWhile } from "rxjs";
import { environment } from "../../environments/environment";
import {
  CreateImportJobRequest,
  EnedisStatus,
  ImportJob,
  SaveCredentialsRequest,
} from "../models/enedis.models";

@Injectable({ providedIn: "root" })
export class EnedisService {
  private http = inject(HttpClient);

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
}
