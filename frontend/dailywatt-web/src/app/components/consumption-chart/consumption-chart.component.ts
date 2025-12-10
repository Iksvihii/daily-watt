import { CommonModule } from "@angular/common";
import { Component, Input, OnChanges } from "@angular/core";
import { ConsumptionPoint, WeatherDay } from "../../models/dashboard.models";

@Component({
  selector: "app-consumption-chart",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./consumption-chart.component.html",
  styleUrl: "./consumption-chart.component.less",
})
export class ConsumptionChartComponent implements OnChanges {
  @Input() consumption: ConsumptionPoint[] = [];
  @Input() weather?: WeatherDay[];

  width = 0;
  height = 220;
  consumptionPoints = "";
  temperaturePoints = "";
  minLabel = "";
  maxLabel = "";

  ngOnChanges(): void {
    if (!this.consumption?.length) {
      this.reset();
      return;
    }

    const consumptionValues = this.consumption.map((c) => c.kwh);
    const min = Math.min(...consumptionValues);
    const max = Math.max(...consumptionValues);
    this.minLabel = `${min.toFixed(1)} kWh`;
    this.maxLabel = `${max.toFixed(1)} kWh`;

    const width = Math.max(this.consumption.length - 1, 1) * 48;
    this.width = width;

    const scale = (value: number, vmin: number, vmax: number) => {
      if (vmax === vmin) return 0.5;
      return (value - vmin) / (vmax - vmin);
    };

    const yFor = (value: number) => {
      const norm = scale(value, min, max);
      return this.height - norm * this.height;
    };

    const xFor = (index: number) => {
      const step = width / Math.max(this.consumption.length - 1, 1);
      return index * step;
    };

    this.consumptionPoints = this.consumption
      .map((c, idx) => `${xFor(idx)},${yFor(c.kwh)}`)
      .join(" ");

    if (this.weather && this.weather.length) {
      const weatherMap = new Map(this.weather.map((w) => [w.date, w.tempAvg]));
      const temps = this.consumption.map((c) => {
        const date = new Date(c.timestampUtc).toISOString().substring(0, 10);
        return weatherMap.get(date);
      });

      const tempValues = temps.filter((t) => t !== undefined) as number[];
      if (tempValues.length) {
        const tMin = Math.min(...tempValues);
        const tMax = Math.max(...tempValues);
        const yForTemp = (value: number) => {
          const norm = scale(value, tMin, tMax);
          return this.height - norm * this.height;
        };
        this.temperaturePoints = temps
          .map((t, idx) =>
            t === undefined ? "" : `${xFor(idx)},${yForTemp(t)}`
          )
          .filter((p) => p)
          .join(" ");
      } else {
        this.temperaturePoints = "";
      }
    } else {
      this.temperaturePoints = "";
    }
  }

  private reset() {
    this.width = 0;
    this.consumptionPoints = "";
    this.temperaturePoints = "";
    this.minLabel = "";
    this.maxLabel = "";
  }
}
