import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, signal, computed } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { DashboardService } from "../../services/dashboard.service";
import { Granularity, TimeSeriesResponse } from "../../models/dashboard.models";
import { ConsumptionChartComponent } from "../consumption-chart/consumption-chart.component";
import { EnedisService } from "../../services/enedis.service";
import { EnedisMeter } from "../../models/enedis.models";

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
export class DashboardComponent implements OnInit {
  private dashboard = inject(DashboardService);
  private enedis = inject(EnedisService);
  private fb = inject(FormBuilder);

  from = signal(this.getStoredFrom() || this.defaultFrom());
  to = signal(this.getStoredTo() || new Date().toISOString().slice(0, 16));
  granularity = signal<Granularity>(this.getStoredGranularity() || "day");
  withWeather = signal(this.getStoredWithWeather() ?? true);
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
    this.loadMeters();
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
  }

  // Enedis status and sync have been removed in favor of manual Excel import via settings

  loadMeters() {
    this.enedis.getMeters().subscribe({
      next: (meters) => {
        this.meters.set(meters);
        // Select favorite meter by default
        const favorite = meters.find((m) => m.isFavorite);
        if (favorite) {
          this.selectedMeterId.set(favorite.id);
        } else if (meters.length > 0) {
          this.selectedMeterId.set(meters[0].id);
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
