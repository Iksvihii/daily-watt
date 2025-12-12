import { Component, inject, signal, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { EnedisService } from "../../services/enedis.service";
import {
  SaveCredentialsRequest,
  CredentialsResponse,
  EnedisMeter,
  CreateMeterRequest,
  UpdateMeterRequest,
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
  saving = signal(false);
  loading = signal(true);
  showPassword = signal(false);
  isInitialLoad = signal(true);

  meters = signal<EnedisMeter[]>([]);
  editingMeter = signal<EnedisMeter | null>(null);
  isAddingMeter = signal(false);

  selectedCoordinates = signal<{
    city?: string;
    latitude?: number;
    longitude?: number;
  }>({});

  loadedCity = signal<string>("");

  credentialsForm = this.fb.group({
    login: ["" as string, Validators.required],
    password: [""],
  });

  meterForm = this.fb.group({
    prm: ["" as string, Validators.required],
    label: [""],
    city: [""],
    latitude: [{ value: null as number | null, disabled: true }],
    longitude: [{ value: null as number | null, disabled: true }],
  });

  ngOnInit(): void {
    this.loadCredentials();
    this.loadMeters();
  }

  private loadCredentials(): void {
    this.enedis.getCredentials().subscribe({
      next: (credentials: CredentialsResponse) => {
        this.credentialsForm.patchValue({
          login: credentials.login,
        });
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

  private loadMeters(): void {
    this.enedis.getMeters().subscribe({
      next: (meters: EnedisMeter[]) => {
        this.meters.set(meters);
      },
      error: () => {
        // No meters yet
        this.meters.set([]);
      },
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((val: boolean) => !val);
  }

  onLocationSelected(data: {
    city: string;
    latitude: number;
    longitude: number;
  }): void {
    // Only set city if it's a valid city name (not coordinates)
    // Coordinates look like "45.1234, 2.5678" or similar patterns
    const isCoordinates =
      data.city && data.city.match(/^-?\d+\.?\d*,\s*-?\d+\.?\d*$/);

    this.selectedCoordinates.set(data);

    // Only set city field if it's not just coordinates
    if (!isCoordinates) {
      this.meterForm.get("city")?.setValue(data.city);
    }

    this.meterForm.get("latitude")?.setValue(data.latitude);
    this.meterForm.get("longitude")?.setValue(data.longitude);
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

  startAddMeter() {
    this.isAddingMeter.set(true);
    this.editingMeter.set(null);
    this.meterForm.reset();
    this.loadedCity.set("");
  }

  cancelMeterEdit() {
    this.isAddingMeter.set(false);
    this.editingMeter.set(null);
    this.meterForm.reset();
    this.loadedCity.set("");
  }

  saveMeter() {
    if (this.meterForm.invalid) {
      return;
    }

    const formValue = this.meterForm.getRawValue();
    const city = formValue.city || "";
    const isCoordinates = city && city.match(/^-?\d+\.?\d*,\s*-?\d+\.?\d*$/);

    if (this.editingMeter()) {
      // Update existing meter
      const updateRequest: UpdateMeterRequest = {
        label: formValue.label || undefined,
        city: isCoordinates ? undefined : city || undefined,
        latitude: formValue.latitude || undefined,
        longitude: formValue.longitude || undefined,
      };
      this.enedis
        .updateMeter(this.editingMeter()!.id, updateRequest)
        .subscribe({
          next: () => {
            this.message.set("Meter updated");
            this.loadMeters();
            this.cancelMeterEdit();
          },
          error: (err: { error?: { error?: string } }) => {
            this.message.set(err.error?.error || "Failed to update meter");
          },
        });
    } else {
      // Create new meter
      const createRequest: CreateMeterRequest = {
        prm: formValue.prm || "",
        label: formValue.label || undefined,
        city: isCoordinates ? undefined : city || undefined,
        latitude: formValue.latitude || undefined,
        longitude: formValue.longitude || undefined,
      };
      this.enedis.createMeter(createRequest).subscribe({
        next: () => {
          this.message.set("Meter added");
          this.loadMeters();
          this.cancelMeterEdit();
        },
        error: (err: { error?: { error?: string } }) => {
          this.message.set(err.error?.error || "Failed to add meter");
        },
      });
    }
  }

  editMeter(meter: EnedisMeter) {
    this.editingMeter.set(meter);
    this.isAddingMeter.set(false);
    this.meterForm.patchValue({
      prm: meter.prm,
      label: meter.label || "",
      city: meter.city || "",
      latitude: meter.latitude || null,
      longitude: meter.longitude || null,
    });
    if (meter.city) {
      this.loadedCity.set(meter.city);
    }
  }

  deleteMeter(meter: EnedisMeter) {
    if (!confirm(`Delete meter ${meter.label || meter.prm}?`)) {
      return;
    }
    this.enedis.deleteMeter(meter.id).subscribe({
      next: () => {
        this.message.set("Meter deleted");
        this.loadMeters();
      },
      error: (err: { error?: { error?: string } }) => {
        this.message.set(err.error?.error || "Failed to delete meter");
      },
    });
  }

  setFavorite(meter: EnedisMeter) {
    this.enedis.setFavoriteMeter(meter.id).subscribe({
      next: () => {
        this.message.set("Default meter set");
        this.loadMeters();
      },
      error: (err: { error?: { error?: string } }) => {
        this.message.set(err.error?.error || "Failed to set default meter");
      },
    });
  }
}
