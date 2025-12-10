import { Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import { Granularity, TimeSeriesResponse } from "../models/dashboard.models";

@Injectable({ providedIn: "root" })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getTimeSeries(
    from: string,
    to: string,
    granularity: Granularity,
    withWeather: boolean
  ): Observable<TimeSeriesResponse> {
    const params = new HttpParams()
      .set("from", from)
      .set("to", to)
      .set("granularity", granularity)
      .set("withWeather", withWeather);

    return this.http.get<TimeSeriesResponse>(
      `${environment.apiUrl}/api/dashboard/timeseries`,
      { params }
    );
  }

  getTimeSeriesWithRange(
    from: string,
    to: string,
    granularity: Granularity,
    startDate: string,
    endDate: string,
    withWeather: boolean
  ): Observable<TimeSeriesResponse> {
    const params = new HttpParams()
      .set("from", from)
      .set("to", to)
      .set("granularity", granularity)
      .set("startDate", startDate)
      .set("endDate", endDate)
      .set("withWeather", withWeather);

    return this.http.get<TimeSeriesResponse>(
      `${environment.apiUrl}/api/dashboard/timeseries`,
      { params }
    );
  }
}
