import { CommonModule } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  FormsModule,
} from "@angular/forms";
import { RouterLink } from "@angular/router";
import { DashboardService } from "../../services/dashboard.service";
import { Granularity, TimeSeriesResponse } from "../../models/dashboard.models";
import { ConsumptionChartComponent } from "../consumption-chart/consumption-chart.component";
import { EnedisService } from "../../services/enedis.service";
import { EnedisStatus } from "../../models/enedis.models";
import { AuthService } from "../../services/auth.service";
import { ChangePasswordRequest, UserProfile } from "../../models/auth.models";

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
  from = signal(this.defaultFrom());
  to = signal(new Date().toISOString().slice(0, 16));
  granularity = signal<Granularity>("day");
  withWeather = signal(true);
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  data = signal<TimeSeriesResponse | undefined>(undefined);
  status = signal<EnedisStatus | null>(null);
  syncing = signal(false);
  statusMessage = signal<string | undefined>(undefined);

  profile = signal<UserProfile | null>(null);
  profileMessage = signal<string | undefined>(undefined);
  profileSaving = signal(false);
  passwordSaving = signal(false);

  profileForm = this.fb.group({
    username: ["", [Validators.required, Validators.minLength(2)]],
  });

  passwordForm = this.fb.group({
    currentPassword: ["", Validators.required],
    newPassword: ["", [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private dashboard: DashboardService,
    private enedis: EnedisService,
    private auth: AuthService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.loadStatus();
    this.loadProfile();
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

  loadProfile() {
    this.auth.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.patchValue({ username: profile.username });
      },
      error: () => this.profileMessage.set("Unable to load profile"),
    });
  }

  saveProfile() {
    if (this.profileForm.invalid) return;
    this.profileSaving.set(true);
    this.profileMessage.set(undefined);
    this.auth
      .updateProfile(this.profileForm.value as { username: string })
      .subscribe({
        next: () => {
          this.profileSaving.set(false);
          this.profileMessage.set("Profile updated");
          this.loadProfile();
        },
        error: (err) => {
          this.profileSaving.set(false);
          this.profileMessage.set(
            err.error?.errors?.join(", ") || err.error?.error || "Update failed"
          );
        },
      });
  }

  changePassword() {
    if (this.passwordForm.invalid) return;
    this.passwordSaving.set(true);
    this.profileMessage.set(undefined);
    const payload = this.passwordForm.value as ChangePasswordRequest;
    this.auth.changePassword(payload).subscribe({
      next: () => {
        this.passwordSaving.set(false);
        this.profileMessage.set("Password updated");
        this.passwordForm.reset();
      },
      error: (err) => {
        this.passwordSaving.set(false);
        this.profileMessage.set(
          err.error?.errors?.join(", ") ||
            err.error?.error ||
            "Password update failed"
        );
      },
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 16);
  }
}
