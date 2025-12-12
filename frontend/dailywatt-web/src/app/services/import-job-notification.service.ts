import { Injectable, inject } from "@angular/core";
import { EnedisService } from "./enedis.service";
import { ToastService } from "./toast.service";
import { Subject } from "rxjs";

export interface ImportJobCompleted {
  jobId: string;
  meterId: string;
  meterLabel: string;
  success: boolean;
  importedCount?: number;
  errorMessage?: string;
}

@Injectable({
  providedIn: "root",
})
export class ImportJobNotificationService {
  private enedis = inject(EnedisService);
  private toast = inject(ToastService);
  private activePolls = new Map<string, ReturnType<typeof setInterval>>();

  // Observable pour notifier les composants des imports terminés
  private importCompletedSubject = new Subject<ImportJobCompleted>();
  public importCompleted$ = this.importCompletedSubject.asObservable();

  /**
   * Démarre le polling d'un job d'import et notifie via toast et observable
   */
  startPollingJob(jobId: string, meterId: string, meterLabel: string): void {
    const pollInterval = 3000; // 3 secondes
    const maxAttempts = 40; // 2 minutes max
    let attempts = 0;

    const timerId = setInterval(() => {
      attempts++;
      this.enedis.getImportJob(jobId).subscribe({
        next: (job) => {
          if (job.status === "Completed") {
            clearInterval(timerId);
            this.activePolls.delete(jobId);

            this.toast.success(
              `Import completed for ${meterLabel}! ${job.importedCount} measurements imported.`,
              8000
            );

            // Notifier les autres composants
            this.importCompletedSubject.next({
              jobId,
              meterId,
              meterLabel,
              success: true,
              importedCount: job.importedCount,
            });
          } else if (job.status === "Failed") {
            clearInterval(timerId);
            this.activePolls.delete(jobId);

            this.toast.error(
              `Import failed for ${meterLabel}: ${
                job.errorMessage || "Unknown error"
              }`,
              8000
            );

            this.importCompletedSubject.next({
              jobId,
              meterId,
              meterLabel,
              success: false,
              errorMessage: job.errorMessage,
            });
          } else if (attempts >= maxAttempts) {
            clearInterval(timerId);
            this.activePolls.delete(jobId);

            this.toast.info(
              `Import for ${meterLabel} is still running. Check back later.`,
              6000
            );
          }
        },
        error: () => {
          if (attempts >= maxAttempts) {
            clearInterval(timerId);
            this.activePolls.delete(jobId);
          }
        },
      });
    }, pollInterval);

    this.activePolls.set(jobId, timerId);
  }

  /**
   * Arrête tous les polling actifs (appelé lors de la destruction de l'application)
   */
  stopAllPolling(): void {
    this.activePolls.forEach((timerId) => clearInterval(timerId));
    this.activePolls.clear();
  }
}
