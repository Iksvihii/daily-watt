import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DashboardService } from '../../services/dashboard.service';
import { Granularity, TimeSeriesResponse } from '../../models/dashboard.models';
import { ConsumptionChartComponent } from '../consumption-chart/consumption-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ConsumptionChartComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  from = this.defaultFrom();
  to = new Date().toISOString().slice(0, 16);
  granularity: Granularity = 'hour';
  withWeather = true;
  loading = false;
  error?: string;

  data?: TimeSeriesResponse;

  constructor(private dashboard: DashboardService) {}

  load() {
    this.loading = true;
    this.error = undefined;
    this.dashboard.getTimeSeries(
      new Date(this.from).toISOString(),
      new Date(this.to).toISOString(),
      this.granularity,
      this.withWeather
    ).subscribe({
      next: res => {
        this.data = res;
        this.loading = false;
      },
      error: err => {
        this.error = err.error?.error || 'Unable to load data';
        this.loading = false;
      }
    });
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 16);
  }
}
