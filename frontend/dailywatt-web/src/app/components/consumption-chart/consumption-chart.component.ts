import { Component, Input, OnChanges } from '@angular/core';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import 'chart.js/auto';
import { BaseChartDirective } from 'ng2-charts';
import { ConsumptionPoint, WeatherDay } from '../../models/dashboard.models';

@Component({
  selector: 'app-consumption-chart',
  standalone: true,
  imports: [BaseChartDirective],
  templateUrl: './consumption-chart.component.html',
  styleUrl: './consumption-chart.component.less'
})
export class ConsumptionChartComponent implements OnChanges {
  @Input() consumption: ConsumptionPoint[] = [];
  @Input() weather?: WeatherDay[];

  lineChartData: ChartConfiguration<'line'>['data'] = { datasets: [], labels: [] };
  lineChartOptions: ChartOptions<'line'> = {
    responsive: true,
    plugins: {
      legend: {
        labels: { color: '#f4f4f6' }
      }
    },
    scales: {
      x: { ticks: { color: '#9fb3c8' } },
      y: {
        position: 'left',
        title: { display: true, text: 'kWh', color: '#9fb3c8' },
        ticks: { color: '#9fb3c8' }
      },
      y1: {
        position: 'right',
        grid: { drawOnChartArea: false },
        title: { display: true, text: 'Temperature (°C)', color: '#ffb703' },
        ticks: { color: '#ffb703' }
      }
    }
  };

  ngOnChanges(): void {
    const labels = this.consumption.map(c => new Date(c.timestampUtc).toLocaleString());
    const consumptionValues = this.consumption.map(c => c.kwh);

    const datasets: ChartConfiguration<'line'>['data']['datasets'] = [
      {
        data: consumptionValues,
        label: 'Consumption',
        yAxisID: 'y',
        tension: 0.3,
        fill: false,
        borderColor: '#3ad1c5',
        pointRadius: 0
      }
    ];

    if (this.weather && this.weather.length) {
      const weatherMap = new Map(this.weather.map(w => [w.date, w.tempAvg]));
      const temps = this.consumption.map(c => {
        const date = new Date(c.timestampUtc).toISOString().substring(0, 10);
        return weatherMap.get(date);
      });

      datasets.push({
        data: temps as number[],
        label: 'Temperature',
        yAxisID: 'y1',
        tension: 0.2,
        fill: false,
        borderColor: '#ffb703',
        borderDash: [5, 4],
        pointRadius: 0
      });
    }

    this.lineChartData = { labels, datasets };
  }
}
