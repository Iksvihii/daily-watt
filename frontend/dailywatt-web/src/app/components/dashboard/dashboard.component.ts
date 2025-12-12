import { CommonModule } from "@angular/common";
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
} from "@angular/core";
import { FormBuilder, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { DashboardService } from "../../services/dashboard.service";
import { Granularity, TimeSeriesResponse } from "../../models/dashboard.models";
import { ConsumptionChartComponent } from "../consumption-chart/consumption-chart.component";
import { EnedisService } from "../../services/enedis.service";
import { EnedisMeter } from "../../models/enedis.models";
import { ImportJobNotificationService } from "../../services/import-job-notification.service";
import { Subscription } from "rxjs";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatNativeDateModule } from "@angular/material/core";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ConsumptionChartComponent,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: "./dashboard.component.html",
  styleUrl: "./dashboard.component.scss",
})
export class DashboardComponent implements OnInit, OnDestroy {
  private dashboard = inject(DashboardService);
  private enedis = inject(EnedisService);
  private fb = inject(FormBuilder);
  private importNotification = inject(ImportJobNotificationService);
  private importSubscription?: Subscription;

  // Initialize with defaults - will be overridden in ngOnInit with stored values
  from = signal<string>("");
  to = signal<string>("");
  granularity = signal<Granularity>("day");
  withWeather = signal(true);
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  data = signal<TimeSeriesResponse | undefined>(undefined);
  meters = signal<EnedisMeter[]>([]);
  selectedMeterId = signal<string | undefined>(undefined);
  selectedMeter = computed(() => {
    const id = this.selectedMeterId();
    return id ? this.meters().find((m) => m.id === id) : undefined;
  });

  private readonly SESSION_STORAGE_PREFIX = "dashboard_";

  ngOnInit(): void {
    // Restaurer tous les filtres depuis le sessionStorage
    this.from.set(this.getStoredFrom() || this.defaultFrom());
    this.to.set(this.getStoredTo() || new Date().toISOString().slice(0, 16));
    this.granularity.set(this.getStoredGranularity() || "day");
    this.withWeather.set(this.getStoredWithWeather() ?? true);

    this.loadMeters();

    // Écouter les notifications d'import terminé pour recharger les données
    this.importSubscription =
      this.importNotification.importCompleted$.subscribe((event) => {
        // Recharger uniquement si l'import concerne le meter actuellement sélectionné
        if (event.success && event.meterId === this.selectedMeterId()) {
          this.load();
        }
      });
  }

  ngOnDestroy(): void {
    this.importSubscription?.unsubscribe();
  }

  private getStoredFrom(): string | null {
    return sessionStorage.getItem(this.SESSION_STORAGE_PREFIX + "from");
  }

  private getStoredTo(): string | null {
    return sessionStorage.getItem(this.SESSION_STORAGE_PREFIX + "to");
  }

  private getStoredGranularity(): Granularity | null {
    const stored = sessionStorage.getItem(
      this.SESSION_STORAGE_PREFIX + "granularity"
    );
    if (stored === "day" || stored === "month" || stored === "year") {
      return stored as Granularity;
    }
    return null;
  }

  private getStoredWithWeather(): boolean | null {
    const stored = sessionStorage.getItem(
      this.SESSION_STORAGE_PREFIX + "withWeather"
    );
    return stored ? JSON.parse(stored) : null;
  }

  private getStoredMeterId(): string | null {
    return sessionStorage.getItem(this.SESSION_STORAGE_PREFIX + "meterId");
  }

  private savePreferencesToSession(): void {
    sessionStorage.setItem(this.SESSION_STORAGE_PREFIX + "from", this.from());
    sessionStorage.setItem(this.SESSION_STORAGE_PREFIX + "to", this.to());
    sessionStorage.setItem(
      this.SESSION_STORAGE_PREFIX + "granularity",
      this.granularity()
    );
    sessionStorage.setItem(
      this.SESSION_STORAGE_PREFIX + "withWeather",
      JSON.stringify(this.withWeather())
    );
    const meterId = this.selectedMeterId();
    if (meterId) {
      sessionStorage.setItem(this.SESSION_STORAGE_PREFIX + "meterId", meterId);
    }
  }

  // Enedis status and sync have been removed in favor of manual Excel import via settings

  loadMeters() {
    this.enedis.getMeters().subscribe({
      next: (meters) => {
        this.meters.set(meters);

        // Restaurer le meter précédemment sélectionné s'il existe
        const storedMeterId = this.getStoredMeterId();
        const storedMeter = storedMeterId
          ? meters.find((m) => m.id === storedMeterId)
          : null;

        if (storedMeter) {
          this.selectedMeterId.set(storedMeter.id);
        } else {
          // Sinon, sélectionner le meter favori par défaut
          const favorite = meters.find((m) => m.isFavorite);
          if (favorite) {
            this.selectedMeterId.set(favorite.id);
          } else if (meters.length > 0) {
            this.selectedMeterId.set(meters[0].id);
          }
        }

        // Auto-load data if a meter is selected
        if (this.selectedMeterId()) {
          this.load();
        }
      },
      error: () => {
        this.meters.set([]);
      },
    });
  }

  onMeterChange(meterId: string) {
    this.selectedMeterId.set(meterId);
    this.load();
  }

  /**
   * Convert datetime string (YYYY-MM-DDTHH:mm) to date string (YYYY-MM-DD)
   */
  getDateString(datetimeStr: string): string {
    return datetimeStr.slice(0, 10);
  }

  /**
   * Convert date string (YYYY-MM-DD) to datetime string (YYYY-MM-DDTHH:mm)
   * Sets time to 00:00 (midnight UTC)
   */
  getDateTimeString(dateStr: string): string {
    return `${dateStr}T00:00`;
  }

  /**
   * Quick date range selection (e.g., 7, 30, 90 days)
   */
  setDateRange(days: number): void {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - days);

    this.to.set(to.toISOString().slice(0, 16));
    this.from.set(from.toISOString().slice(0, 16));
    this.savePreferencesToSession();
    this.load();
  }

  /**
   * Check if current date range matches a quick select option
   */
  isRangeActive(days: number): boolean {
    try {
      const fromDate = new Date(this.from());
      const toDate = new Date(this.to());
      const expectedFromDate = new Date(toDate);
      expectedFromDate.setDate(expectedFromDate.getDate() - days);

      // Compare dates at midnight (ignore time)
      return (
        fromDate.toDateString() === expectedFromDate.toDateString() &&
        toDate.toDateString() === new Date().toDateString()
      );
    } catch {
      return false;
    }
  }

  load() {
    this.savePreferencesToSession();
    this.loading.set(true);
    this.error.set(undefined);
    this.dashboard
      .getTimeSeries({
        meterId: this.selectedMeterId(),
        from: new Date(this.from()).toISOString(),
        to: new Date(this.to()).toISOString(),
        granularity: this.granularity(),
        withWeather: this.withWeather(),
      })
      .subscribe({
        next: (res) => {
          this.data.set(res);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.error || "Unable to load data");
          this.loading.set(false);
        },
      });
  }

  // Sync button removed; imports are initiated via Enedis settings and processed by the worker

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 16);
  }
}
