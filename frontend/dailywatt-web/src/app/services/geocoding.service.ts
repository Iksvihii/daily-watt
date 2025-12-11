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
   * Get city name suggestions for autocomplete (limited to France)
   */
  getCitySuggestions(query: string): Observable<string[]> {
    return this.http.get<string[]>(
      `${environment.apiUrl}/api/enedis/geocode/suggestions`,
      {
        params: { query },
      }
    );
  }

  /**
   * Geocode a city name to coordinates (returns city center)
   */
  geocodeCity(city: string): Observable<GeocodeResult> {
    return this.http.post<GeocodeResult>(
      `${environment.apiUrl}/api/enedis/geocode`,
      { city }
    );
  }
}
