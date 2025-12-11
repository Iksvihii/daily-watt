import { Component, inject, signal, OnInit } from \"@angular/core\";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { EnedisService } from "../../services/enedis.service";
import { ImportJob, SaveCredentialsRequest } from "../../models/enedis.models";
import { AddressMapInputComponent } from "../address-map-input/address-map-input.component";

@Component({
  selector: "app-enedis-settings",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AddressMapInputComponent],
  templateUrl: "./enedis-settings.component.html",
  styleUrl: "./enedis-settings.component.less",
})
export class EnedisSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private enedis = inject(EnedisService);

  message = signal<string | undefined>(undefined);
  job = signal<ImportJob | undefined>(undefined);
  saving = signal(false);
  importing = signal(false);
  showLocationMap = signal(false);
  loading = signal(true);
  showPassword = signal(false);

  selectedCoordinates = signal<{
    address?: string;
    latitude?: number;
    longitude?: number;
  }>({});

  credentialsForm = this.fb.group({
    login: ["" as string, Validators.required],
    password: ["" as string, Validators.required],
    meterNumber: ["" as string, Validators.required],
    address: [""],
    latitude: [null as number | null],
    longitude: [null as number | null],
  });

  importForm = this.fb.group({
    from: [this.defaultFrom(), Validators.required],
    to: [new Date().toISOString().slice(0, 16), Validators.required],
  });

  ngOnInit(): void {
    this.loadCredentials();
  }

  private loadCredentials(): void {
    this.enedis.getCredentials().subscribe({
      next: (credentials) => {
        this.credentialsForm.patchValue({
          login: credentials.login,
          meterNumber: credentials.meterNumber,
          address: credentials.address,
          latitude: credentials.latitude,
          longitude: credentials.longitude,
        });
        this.loading.set(false);
      },
      error: () => {
        // No credentials saved yet, form stays empty
        this.loading.set(false);
      },
    });
  }

  toggleLocationMap(): void {
    this.showLocationMap.update((val) => !val);
  }

  onLocationSelected(data: {
    address: string;
    latitude: number;
    longitude: number;
  }): void {
    this.selectedCoordinates.set(data);
    this.credentialsForm.patchValue({
      address: data.address,
      latitude: data.latitude,
      longitude: data.longitude,
    });
  }

  saveCredentials() {
    if (this.credentialsForm.invalid) {
      return;
    }
    this.saving.set(true);
    const request = this.credentialsForm.value as SaveCredentialsRequest;
    this.enedis.saveCredentials(request).subscribe({
      next: () => {
        this.message.set("Credentials saved");
        this.showLocationMap.set(false);
        this.saving.set(false);
      },
      error: (err: { error?: { error?: string } }) => {
        this.message.set(err.error?.error || "Failed to save credentials");
        this.saving.set(false);
      },
    });
  }

  startImport() {
    if (this.importForm.invalid) {
      return;
    }
    this.importing.set(true);
    const payload = {
      fromUtc: new Date(this.importForm.value.from as string).toISOString(),
      toUtc: new Date(this.importForm.value.to as string).toISOString(),
    };

    this.enedis.createImportJob(payload).subscribe({
      next: (job) => {
        this.job.set(job);
        this.pollJob(job.id);
      },
      error: (err: { error?: { error?: string } }) => {
        this.message.set(err.error?.error || "Failed to start import");
        this.importing.set(false);
      },
    });
  }

  private pollJob(id: string) {
    this.enedis.pollJobUntilDone(id).subscribe((job) => {
      this.job.set(job);
      if (job.status === "Completed" || job.status === "Failed") {
        this.importing.set(false);
      }
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 2);
    return d.toISOString().slice(0, 16);
  }
}
