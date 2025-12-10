import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import {
  GetTimeSeriesRequest,
  TimeSeriesResponse,
} from "../models/dashboard.models";

@Injectable({ providedIn: "root" })
export class DashboardService {
  private http = inject(HttpClient);

  getTimeSeries(request: GetTimeSeriesRequest): Observable<TimeSeriesResponse> {
    const params = new HttpParams()
      .set("from", request.from)
      .set("to", request.to)
      .set("granularity", request.granularity)
      .set("withWeather", request.withWeather ?? false);

    return this.http.get<TimeSeriesResponse>(
      `${environment.apiUrl}/api/dashboard/timeseries`,
      { params }
    );
  }
}
