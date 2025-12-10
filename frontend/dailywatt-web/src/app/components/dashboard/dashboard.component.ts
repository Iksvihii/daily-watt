import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DashboardService } from '../../services/dashboard.service';
import { Granularity, TimeSeriesResponse } from '../../models/dashboard.models';
import { ConsumptionChartComponent } from '../consumption-chart/consumption-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ConsumptionChartComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.less'
})
export class DashboardComponent {
  from = signal(this.defaultFrom());
  to = signal(new Date().toISOString().slice(0, 16));
  granularity = signal<Granularity>('hour');
  withWeather = signal(true);
  loading = signal(false);
  error = signal<string | undefined>(undefined);

  data = signal<TimeSeriesResponse | undefined>(undefined);

  constructor(private dashboard: DashboardService) {}

  load() {
    this.loading.set(true);
    this.error.set(undefined);
    this.dashboard.getTimeSeries(
      new Date(this.from()).toISOString(),
      new Date(this.to()).toISOString(),
      this.granularity(),
      this.withWeather()
    ).subscribe({
      next: res => {
        this.data.set(res);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.error || 'Unable to load data');
        this.loading.set(false);
      }
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 16);
  }
}
