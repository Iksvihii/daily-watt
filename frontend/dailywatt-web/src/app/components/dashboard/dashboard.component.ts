import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { DashboardService } from "../../services/dashboard.service";
import { Granularity, TimeSeriesResponse } from "../../models/dashboard.models";
import { ConsumptionChartComponent } from "../consumption-chart/consumption-chart.component";
import { EnedisService } from "../../services/enedis.service";
import { EnedisStatus } from "../../models/enedis.models";

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
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
  status = signal<EnedisStatus | null>(null);
  syncing = signal(false);
  statusMessage = signal<string | undefined>(undefined);

  private readonly SESSION_STORAGE_PREFIX = "dashboard_";

  ngOnInit(): void {
    this.loadStatus();
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
    return (stored as Granularity) || null;
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

  loadStatus() {
    this.statusMessage.set(undefined);
    this.enedis.getStatus().subscribe({
      next: (s) => {
        this.status.set(s);
        if (s.configured) {
          this.load();
        }
      },
      error: () => this.statusMessage.set("Unable to load Enedis status"),
    });
  }

  load() {
    this.savePreferencesToSession();
    this.loading.set(true);
    this.error.set(undefined);
    this.dashboard
      .getTimeSeries({
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

  syncNow() {
    if (this.syncing()) return;
    this.syncing.set(true);
    const payload = {
      fromUtc: new Date(this.defaultFrom()).toISOString(),
      toUtc: new Date().toISOString(),
    };
    this.enedis.createImportJob(payload).subscribe({
      next: (job) => {
        this.statusMessage.set(`Sync started (job ${job.id})`);
        this.syncing.set(false);
      },
      error: (err) => {
        this.statusMessage.set(err.error?.error || "Failed to start sync");
        this.syncing.set(false);
      },
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 16);
  }
}
