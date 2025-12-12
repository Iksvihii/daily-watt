import { CommonModule } from "@angular/common";
import { Component, OnInit, OnDestroy, inject, signal, computed } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { DashboardService } from "../../services/dashboard.service";
import { Granularity, TimeSeriesResponse } from "../../models/dashboard.models";
import { ConsumptionChartComponent } from "../consumption-chart/consumption-chart.component";
import { EnedisService } from "../../services/enedis.service";
import { EnedisMeter } from "../../models/enedis.models";
import { ImportJobNotificationService } from "../../services/import-job-notification.service";
import { Subscription } from "rxjs";

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ConsumptionChartComponent,
  ],
  templateUrl: "./dashboard.component.html",
  styleUrl: "./dashboard.component.less",
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
    this.importSubscription = this.importNotification.importCompleted$.subscribe(
      (event) => {
        // Recharger uniquement si l'import concerne le meter actuellement sélectionné
        if (event.success && event.meterId === this.selectedMeterId()) {
          this.load();
        }
      }
    );
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
        const storedMeter = storedMeterId ? meters.find(m => m.id === storedMeterId) : null;
        
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
