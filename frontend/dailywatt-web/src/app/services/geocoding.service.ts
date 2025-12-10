import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

export interface GeocodeResult {
  latitude: number;
  longitude: number;
}

@Injectable({ providedIn: "root" })
export class GeocodingService {
  private http = inject(HttpClient);

  /**
   * Get address suggestions for autocomplete (limited to metropolitan France)
   */
  getAddressSuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(
      `${environment.apiUrl}/api/enedis/geocode/suggestions`,
      {
        params: { 
          query,
          countryCode: 'FR' // Limit to France only
        },
      }
    );
  }

  /**
   * Geocode an address to coordinates
   */
  geocodeAddress(address: string): Observable<GeocodeResult> {
    return this.http.post<GeocodeResult>(
      `${environment.apiUrl}/api/enedis/geocode`,
      { address }
    );
  }
}
