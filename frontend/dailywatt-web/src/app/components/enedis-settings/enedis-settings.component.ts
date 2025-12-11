import { Component, inject, signal, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { EnedisService } from "../../services/enedis.service";
import {
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
  saving = signal(false);
  loading = signal(true);
  showPassword = signal(false);
  isInitialLoad = signal(true);

  selectedCoordinates = signal<{
    city?: string;
    latitude?: number;
    longitude?: number;
  }>({});

  loadedCity = signal<string>("");

  credentialsForm = this.fb.group({
    login: ["" as string, Validators.required],
    password: [""],
    meterNumber: ["" as string, Validators.required],
    city: [""],
    latitude: [{ value: null as number | null, disabled: true }],
    longitude: [{ value: null as number | null, disabled: true }],
  });

  ngOnInit(): void {
    this.loadCredentials();
  }

  private loadCredentials(): void {
    this.enedis.getCredentials().subscribe({
      next: (credentials: CredentialsResponse) => {
        this.credentialsForm.patchValue({
          login: credentials.login,
          meterNumber: credentials.meterNumber,
          city: credentials.city,
          latitude: credentials.latitude,
          longitude: credentials.longitude,
        });
        if (credentials.city) {
          this.loadedCity.set(credentials.city);
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
      this.credentialsForm.get("city")?.setValue(data.city);
    }

    this.credentialsForm.get("latitude")?.setValue(data.latitude);
    this.credentialsForm.get("longitude")?.setValue(data.longitude);
  }

  saveCredentials() {
    if (this.credentialsForm.invalid) {
      return;
    }
    this.saving.set(true);
    const formValue = this.credentialsForm.getRawValue();

    // Only include city if it's a valid name (not coordinates)
    const city = formValue.city || "";
    const isCoordinates = city && city.match(/^-?\d+\.?\d*,\s*-?\d+\.?\d*$/);

    const request: SaveCredentialsRequest = {
      login: formValue.login || "",
      password: formValue.password || "",
      meterNumber: formValue.meterNumber || "",
      city: isCoordinates ? "" : city,
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
}
