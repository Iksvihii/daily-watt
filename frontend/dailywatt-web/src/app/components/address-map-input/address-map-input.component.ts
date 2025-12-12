import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  inject,
  signal,
  CUSTOM_ELEMENTS_SCHEMA,
  input,
  effect,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import L from "leaflet";
import { GeocodingService } from "../../services/geocoding.service";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";

@Component({
  selector: "app-address-map-input",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: "./address-map-input.component.html",
  styleUrl: "./address-map-input.component.scss",
})
export class AddressMapInputComponent implements OnInit {
  private geocodingService = inject(GeocodingService);

  // Input signals (reactive to parent changes)
  initialAddress = input<string>("");
  initialLatitude = input<number | undefined>(undefined);
  initialLongitude = input<number | undefined>(undefined);
  isInitialLoad = input<boolean>(true);
  isLoadedAddress = input<boolean>(false);

  @Output() addressSelected = new EventEmitter<{
    city: string;
    latitude: number;
    longitude: number;
  }>();

  addressInput = signal(""); // Repurposed for city input
  suggestions = signal<string[]>([]);
  selectedMarker: L.Marker | null = null;
  map: L.Map | null = null;
  isLoadingSuggestions = signal(false);
  errorMessage = signal("");
  private searchTimeout: any = null;
  private hasUserModifiedInput = false; // Track if user has edited the input field

  // Map options
  mapCenter = { lat: 48.8566, lng: 2.3522 }; // Default: Paris
  mapZoom = 13;

  constructor() {
    // Initialize address from input
    this.addressInput.set(this.initialAddress());

    // If initial coordinates are provided, update map center
    if (this.initialLatitude() && this.initialLongitude()) {
      this.mapCenter = {
        lat: this.initialLatitude()!,
        lng: this.initialLongitude()!,
      };
    }

    // React to changes in initialAddress (when parent updates it)
    effect(() => {
      const address = this.initialAddress();
      if (address) {
        this.addressInput.set(address);
      }
    });

    // React to changes in initial coordinates - update map center for next initialization
    effect(() => {
      const lat = this.initialLatitude();
      const lng = this.initialLongitude();
      if (lat && lng) {
        this.mapCenter = { lat, lng };
        // If map already exists, recenter it
        if (this.map) {
          this.map.setView([lat, lng], 13);
        }
      }
    });
  }

  ngOnInit(): void {
    // Initialize map after view loads
    setTimeout(() => {
      this.initializeMap();

      // After map is initialized, ensure it's centered on the initial coordinates
      if (this.initialLatitude() && this.initialLongitude() && this.map) {
        this.map.setView(
          [this.initialLatitude()!, this.initialLongitude()!],
          13
        );
      }
    }, 100);
  }

  private initializeMap(): void {
    const mapContainer = document.getElementById("address-map");
    if (!mapContainer) return;

    // Fix Leaflet default icon paths
    const iconRetinaUrl =
      "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png";
    const iconUrl =
      "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png";
    const shadowUrl =
      "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png";
    const iconDefault = L.icon({
      iconRetinaUrl,
      iconUrl,
      shadowUrl,
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      tooltipAnchor: [16, -28],
      shadowSize: [41, 41],
    });
    L.Marker.prototype.options.icon = iconDefault;

    this.map = L.map("address-map").setView(
      [this.mapCenter.lat, this.mapCenter.lng],
      this.mapZoom
    );

    // Add OpenStreetMap tiles
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19,
    }).addTo(this.map);

    // Add initial marker if coordinates exist
    const initialLat = this.initialLatitude();
    const initialLng = this.initialLongitude();
    if (initialLat && initialLng) {
      this.selectedMarker = L.marker([initialLat, initialLng]).addTo(this.map);

      const address = this.initialAddress();
      if (address) {
        this.selectedMarker.bindPopup(address).openPopup();
      }
    }

    // Click to set coordinates with reverse geocoding
    this.map.on("click", async (e: L.LeafletMouseEvent) => {
      await this.handleMapClick(e.latlng.lat, e.latlng.lng);
    });
  }

  private async handleMapClick(
    latitude: number,
    longitude: number
  ): Promise<void> {
    this.isLoadingSuggestions.set(true);
    this.errorMessage.set("");

    try {
      // Try to get city name from coordinates
      const result = await this.geocodingService
        .reverseGeocodeCoordinates(latitude, longitude)
        .toPromise();

      if (result?.city) {
        this.addressInput.set(result.city);
        this.setMarker(latitude, longitude, result.city);
      } else {
        // If no city found, just place the marker without a city name
        this.setMarker(latitude, longitude);
      }
    } catch {
      // If reverse geocoding fails, just place the marker without a city name
      this.errorMessage.set(
        "Could not determine city name for these coordinates"
      );
      this.setMarker(latitude, longitude);
    } finally {
      this.isLoadingSuggestions.set(false);
    }
  }

  async onAddressInput(): Promise<void> {
    const query = this.addressInput();
    this.errorMessage.set("");

    // Mark that user has modified the input
    this.hasUserModifiedInput = true;

    // Clear previous timeout
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    if (query.length < 3) {
      this.suggestions.set([]);
      return;
    }

    // Debounce: wait 400ms before sending request
    this.searchTimeout = setTimeout(async () => {
      this.isLoadingSuggestions.set(true);
      try {
        const result = await this.geocodingService
          .getCitySuggestions(query)
          .toPromise();
        this.suggestions.set(result || []);
      } catch {
        this.errorMessage.set("Unable to fetch city suggestions");
        this.suggestions.set([]);
      } finally {
        this.isLoadingSuggestions.set(false);
      }
    }, 400);
  }

  async selectSuggestion(suggestion: string): Promise<void> {
    // Extract city name (remove postal code if present)
    // Format: "CityName (PostalCode)" or "CityName"
    const city = suggestion.includes("(")
      ? suggestion.substring(0, suggestion.indexOf("(")).trim()
      : suggestion;

    this.addressInput.set(suggestion);
    this.suggestions.set([]);
    this.errorMessage.set("");

    this.isLoadingSuggestions.set(true);
    try {
      const result = await this.geocodingService.geocodeCity(city).toPromise();

      if (result) {
        this.setMarker(result.latitude, result.longitude, suggestion);
      } else {
        this.errorMessage.set("City could not be found");
      }
    } catch {
      this.errorMessage.set("Error geocoding city");
    } finally {
      this.isLoadingSuggestions.set(false);
    }
  }

  private setMarker(latitude: number, longitude: number, city?: string): void {
    if (!this.map) return;

    // Remove existing marker
    if (this.selectedMarker) {
      this.map.removeLayer(this.selectedMarker);
    }

    // IMPORTANT: Only emit city name if provided, never emit coordinates as city
    const displayCity = city || this.addressInput();

    // Add new marker with popup (always show coordinates in popup)
    this.selectedMarker = L.marker([latitude, longitude])
      .addTo(this.map)
      .bindPopup(
        `<strong>Selected city</strong><br/>${
          displayCity || `${latitude.toFixed(4)}, ${longitude.toFixed(4)}`
        }`
      )
      .openPopup();

    // Center map on marker
    this.map.setView([latitude, longitude], this.mapZoom);

    // Only emit if we have a valid city name (not just coordinates)
    if (displayCity && !displayCity.match(/^-?\d+\.?\d*,\s*-?\d+\.?\d*$/)) {
      this.addressSelected.emit({
        city: displayCity,
        latitude,
        longitude,
      });
    }
  }

  clearSelection(): void {
    this.addressInput.set("");
    this.suggestions.set([]);
    this.errorMessage.set("");

    if (this.map && this.selectedMarker) {
      this.map.removeLayer(this.selectedMarker);
      this.selectedMarker = null;
    }
  }
}
