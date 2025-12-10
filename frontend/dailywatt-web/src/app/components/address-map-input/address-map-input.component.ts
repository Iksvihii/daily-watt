import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  inject,
  signal,
  CUSTOM_ELEMENTS_SCHEMA,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import L from "leaflet";
import { GeocodingService } from "../../services/geocoding.service";

@Component({
  selector: "app-address-map-input",
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: "./address-map-input.component.html",
  styleUrl: "./address-map-input.component.less",
})
export class AddressMapInputComponent implements OnInit {
  private geocodingService = inject(GeocodingService);
  @Input() initialAddress = "";
  @Input() initialLatitude?: number;
  @Input() initialLongitude?: number;

  @Output() addressSelected = new EventEmitter<{
    address: string;
    latitude: number;
    longitude: number;
  }>();

  addressInput = signal("");
  suggestions = signal<string[]>([]);
  selectedMarker: L.Marker | null = null;
  map: L.Map | null = null;
  isLoadingSuggestions = signal(false);
  errorMessage = signal("");
  private searchTimeout: any = null;

  // Map options
  mapCenter = { lat: 48.8566, lng: 2.3522 }; // Default: Paris
  mapZoom = 13;

  constructor() {
    // Initialize address from input
    this.addressInput.set(this.initialAddress);

    // If initial coordinates are provided, update map center
    if (this.initialLatitude && this.initialLongitude) {
      this.mapCenter = {
        lat: this.initialLatitude,
        lng: this.initialLongitude,
      };
    }
  }

  ngOnInit(): void {
    // Initialize map after view loads
    setTimeout(() => {
      this.initializeMap();
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
    if (this.initialLatitude && this.initialLongitude) {
      this.selectedMarker = L.marker([
        this.initialLatitude,
        this.initialLongitude,
      ]).addTo(this.map);

      if (this.initialAddress) {
        this.selectedMarker.bindPopup(this.initialAddress).openPopup();
      }
    }

    // Click to set coordinates
    this.map.on("click", (e: L.LeafletMouseEvent) => {
      this.setMarker(e.latlng.lat, e.latlng.lng);
    });
  }

  async onAddressInput(): Promise<void> {
    const query = this.addressInput();
    this.errorMessage.set("");

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
          .getAddressSuggestions(query)
          .toPromise();
        this.suggestions.set(result || []);
      } catch {
        this.errorMessage.set("Unable to fetch address suggestions");
        this.suggestions.set([]);
      } finally {
        this.isLoadingSuggestions.set(false);
      }
    }, 400);
  }

  async selectSuggestion(address: string): Promise<void> {
    this.addressInput.set(address);
    this.suggestions.set([]);
    this.errorMessage.set("");

    this.isLoadingSuggestions.set(true);
    try {
      const result = await this.geocodingService
        .geocodeAddress(address)
        .toPromise();

      if (result) {
        this.setMarker(result.latitude, result.longitude, address);
      } else {
        this.errorMessage.set("Address could not be found");
      }
    } catch {
      this.errorMessage.set("Error geocoding address");
    } finally {
      this.isLoadingSuggestions.set(false);
    }
  }

  private setMarker(
    latitude: number,
    longitude: number,
    address?: string
  ): void {
    if (!this.map) return;

    // Remove existing marker
    if (this.selectedMarker) {
      this.map.removeLayer(this.selectedMarker);
    }

    const displayAddress =
      address ||
      this.addressInput() ||
      `${latitude.toFixed(6)}, ${longitude.toFixed(6)}`;

    // Add new marker with popup
    this.selectedMarker = L.marker([latitude, longitude])
      .addTo(this.map)
      .bindPopup(`<strong>Selected location</strong><br/>${displayAddress}`)
      .openPopup();

    // Center map on marker
    this.map.setView([latitude, longitude], this.mapZoom);

    // Emit result
    this.addressSelected.emit({
      address: displayAddress,
      latitude,
      longitude,
    });
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
