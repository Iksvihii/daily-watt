import { Component, inject, signal, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { EnedisService } from "../../services/enedis.service";
import {
  ImportJob,
  SaveCredentialsRequest,
  CredentialsResponse,
} from "../../models/enedis.models";
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
  loading = signal(true);
  showPassword = signal(false);
  isInitialLoad = signal(true);

  selectedCoordinates = signal<{
    address?: string;
    latitude?: number;
    longitude?: number;
  }>({});

  loadedAddress = signal<string>("");

  credentialsForm = this.fb.group({
    login: ["" as string, Validators.required],
    password: [""],
    meterNumber: ["" as string, Validators.required],
    address: [""],
    latitude: [{ value: null as number | null, disabled: true }],
    longitude: [{ value: null as number | null, disabled: true }],
  });

  importForm = this.fb.group({
    from: [this.defaultFrom(), Validators.required],
    to: [new Date().toISOString().slice(0, 16), Validators.required],
  });

  ngOnInit(): void {
    this.loadCredentials();
  }

  private defaultFrom(): string {
    const date = new Date();
    date.setDate(date.getDate() - 7);
    return date.toISOString().slice(0, 16);
  }

  private loadCredentials(): void {
    this.enedis.getCredentials().subscribe({
      next: (credentials: CredentialsResponse) => {
        this.credentialsForm.patchValue({
          login: credentials.login,
          meterNumber: credentials.meterNumber,
          address: credentials.address,
          latitude: credentials.latitude,
          longitude: credentials.longitude,
        });
        if (credentials.address) {
          this.loadedAddress.set(credentials.address);
        }
        this.loading.set(false);
        this.isInitialLoad.set(false);
      },
      error: () => {
        // No credentials saved yet, form stays empty
        this.loading.set(false);
        this.isInitialLoad.set(false);
      },
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((val: boolean) => !val);
  }

  onLocationSelected(data: {
    address: string;
    latitude: number;
    longitude: number;
  }): void {
    this.selectedCoordinates.set(data);
    this.credentialsForm.get("address")?.setValue(data.address);
    this.credentialsForm.get("latitude")?.setValue(data.latitude);
    this.credentialsForm.get("longitude")?.setValue(data.longitude);
  }

  saveCredentials() {
    if (this.credentialsForm.invalid) {
      return;
    }
    this.saving.set(true);
    const formValue = this.credentialsForm.getRawValue();
    const request: SaveCredentialsRequest = {
      login: formValue.login || "",
      password: formValue.password || "",
      meterNumber: formValue.meterNumber || "",
      address: formValue.address || "",
      latitude: formValue.latitude || 0,
      longitude: formValue.longitude || 0,
    };
    this.enedis.saveCredentials(request).subscribe({
      next: () => {
        this.message.set("Credentials saved");
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
      next: (job: ImportJob) => {
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
    this.enedis.pollJobUntilDone(id).subscribe((job: ImportJob) => {
      this.job.set(job);
      if (job.status === "Completed" || job.status === "Failed") {
        this.importing.set(false);
      }
    });
  }
}
