import { Injectable, computed, inject, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Subject, Subscription, finalize, interval } from "rxjs";
import { environment } from "../../environments/environment";

@Injectable({ providedIn: "root" })
export class BackendHealthService {
  private http = inject(HttpClient);
  private backendAvailable = signal<boolean | null>(null); // null = unknown/initial check, true = up, false = down
  private checkInProgress = signal<boolean>(false);
  private pollingSub?: Subscription;
  private healthConfirmed$ = new Subject<void>();

  isBackendAvailable = computed(() => this.backendAvailable());
  isChecking = computed(() => this.checkInProgress());

  constructor() {
    // Initial probe on app startup to avoid loading UI when backend is down
    this.pingBackend();
  }

  /** Trigger a health probe on demand (used after failed requests) */
  triggerHealthCheck(): void {
    this.pingBackend();
  }

  /** Mark backend down immediately and start polling until it recovers */
  markFailureAndProbe(): void {
    this.backendAvailable.set(false);
    this.startPollingWhenDown();
    this.pingBackend();
  }

  /** Start a periodic probe while backend is down */
  private startPollingWhenDown(): void {
    if (this.pollingSub) return;
    this.pollingSub = interval(10000).subscribe(() => this.pingBackend());
  }

  /** Stop periodic probe once backend is back */
  private stopPolling(): void {
    this.pollingSub?.unsubscribe();
    this.pollingSub = undefined;
  }

  /** Wait until backend is confirmed available (used in guards) */
  async waitForBackendReady(): Promise<boolean> {
    if (this.backendAvailable() === true) {
      return true;
    }

    // Backend is not confirmed up; trigger a probe and wait
    this.pingBackend();

    return new Promise((resolve) => {
      const sub = this.healthConfirmed$.subscribe(() => {
        sub.unsubscribe();
        resolve(true);
      });

      // Set a timeout to avoid infinite waiting
      setTimeout(() => {
        sub.unsubscribe();
        resolve(false);
      }, 5000);
    });
  }

  /** Force a quick health check before navigation (bypasses cached state) */
  async verifyBackendAccessible(): Promise<boolean> {
    return new Promise((resolve) => {
      const timeout = setTimeout(() => {
        resolve(false);
      }, 2000); // 2-second timeout for quick verification

      this.http
        .get(`${environment.apiUrl}/api/health`, { responseType: "text" })
        .subscribe({
          next: () => {
            clearTimeout(timeout);
            this.backendAvailable.set(true);
            this.stopPolling();
            resolve(true);
          },
          error: () => {
            clearTimeout(timeout);
            this.backendAvailable.set(false);
            this.startPollingWhenDown();
            resolve(false);
          },
        });
    });
  }

  /** Single ping with concurrency guard */
  private pingBackend(): void {
    if (this.checkInProgress()) return;

    this.checkInProgress.set(true);

    this.http
      .get(`${environment.apiUrl}/api/health`, { responseType: "text" })
      .pipe(finalize(() => this.checkInProgress.set(false)))
      .subscribe({
        next: () => {
          this.backendAvailable.set(true);
          this.stopPolling();
          this.healthConfirmed$.next();
        },
        error: () => {
          this.backendAvailable.set(false);
          this.startPollingWhenDown();
        },
      });
  }
}
