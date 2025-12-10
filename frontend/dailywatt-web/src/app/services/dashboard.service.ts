import { Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import {
  Granularity,
  GetTimeSeriesRequest,
  TimeSeriesResponse,
} from "../models/dashboard.models";

@Injectable({ providedIn: "root" })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getTimeSeries(request: GetTimeSeriesRequest): Observable<TimeSeriesResponse> {
    const params = new HttpParams()
      .set("from", request.from)
      .set("to", request.to)
      .set("granularity", request.granularity)
      .set("withWeather", request.withWeather ?? false);

    if (request.startDate) {
      params.set("startDate", request.startDate);
    }
    if (request.endDate) {
      params.set("endDate", request.endDate);
    }

    return this.http.get<TimeSeriesResponse>(
      `${environment.apiUrl}/api/dashboard/timeseries`,
      { params }
    );
  }
}
