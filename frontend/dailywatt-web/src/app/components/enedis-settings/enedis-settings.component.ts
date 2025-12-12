import { Component, inject, signal, OnInit, OnDestroy } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  FormBuilder,
  ReactiveFormsModule,
  FormsModule,
  Validators,
} from "@angular/forms";
import { EnedisService } from "../../services/enedis.service";
import { ToastService } from "../../services/toast.service";
import { ImportJobNotificationService } from "../../services/import-job-notification.service";
import {
  EnedisMeter,
  CreateMeterRequest,
  UpdateMeterRequest,
} from "../../models/enedis.models";
import { AddressMapInputComponent } from "../address-map-input/address-map-input.component";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";

@Component({
  selector: "app-enedis-settings",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AddressMapInputComponent,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: "./enedis-settings.component.html",
  styleUrl: "./enedis-settings.component.scss",
})
export class EnedisSettingsComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private enedis = inject(EnedisService);
  private toast = inject(ToastService);
  private importNotification = inject(ImportJobNotificationService);

  message = signal<string | undefined>(undefined);
  loading = signal(true);
  isInitialLoad = signal(true);

  meters = signal<EnedisMeter[]>([]);
  editingMeter = signal<EnedisMeter | null>(null);
  isAddingMeter = signal(false);
  importMessage = signal<string | undefined>(undefined);
  importInProgress = signal(false);
  selectedImportFile = signal<File | null>(null);

  selectedCoordinates = signal<{
    city?: string;
    latitude?: number;
    longitude?: number;
  }>({});

  loadedCity = signal<string>("");

  meterForm = this.fb.group({
    prm: ["" as string, Validators.required],
    label: [""],
    city: [""],
    latitude: [{ value: null as number | null, disabled: true }],
    longitude: [{ value: null as number | null, disabled: true }],
  });

  ngOnInit(): void {
    this.loadMeters();
  }

  ngOnDestroy(): void {
    console.log("EnedisSettingsComponent destroyed");
  }

  private loadMeters(): void {
    this.enedis.getMeters().subscribe({
      next: (meters: EnedisMeter[]) => {
        this.meters.set(meters);
        this.loading.set(false);
      },
      error: () => {
        // No meters yet
        this.meters.set([]);
        this.loading.set(false);
      },
    });
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
        prm: formValue.prm || this.editingMeter()!.prm,
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

  onImportFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files && target.files[0] ? target.files[0] : null;
    this.selectedImportFile.set(file);
  }

  startImportForMeter(meter: EnedisMeter) {
    const file = this.selectedImportFile();
    if (!file) {
      this.toast.error("Please choose an Excel file first");
      return;
    }
    this.importInProgress.set(true);
    this.enedis.uploadConsumptionFile({ file, meterId: meter.id }).subscribe({
      next: (job) => {
        this.toast.success(
          `Import started for ${meter.label || meter.prm}. Processing...`
        );
        this.importInProgress.set(false);
        this.selectedImportFile.set(null);
        // Start polling using global service
        this.importNotification.startPollingJob(
          job.id,
          meter.id,
          meter.label || meter.prm
        );
      },
      error: (err: { error?: { error?: string } }) => {
        this.toast.error(err.error?.error || "Failed to start import");
        this.importInProgress.set(false);
      },
    });
  }
}
