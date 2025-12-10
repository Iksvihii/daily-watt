import {
  Component,
  Input,
  OnChanges,
  inject,
  signal,
  CUSTOM_ELEMENTS_SCHEMA,
  OnInit,
  OnDestroy,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { NgxEchartsModule } from "ngx-echarts";
import type { EChartsOption } from "echarts";
import {
  ConsumptionPoint,
  WeatherDay,
  Granularity,
} from "../../models/dashboard.models";
import { DashboardService } from "../../services/dashboard.service";
import { Subject } from "rxjs";
import { takeUntil, debounceTime } from "rxjs/operators";

interface ChartData {
  timestamps: number[];
  consumptionValues: number[];
  temperatureValues: (number | null)[];
  labels: string[];
}

@Component({
  selector: "app-consumption-chart",
  standalone: true,
  imports: [NgxEchartsModule, CommonModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: "./consumption-chart.component.html",
  styleUrl: "./consumption-chart.component.less",
})
export class ConsumptionChartComponent implements OnInit, OnChanges, OnDestroy {
  private dashboardService = inject(DashboardService);
  @Input() consumption: ConsumptionPoint[] = [];
  @Input() weather?: WeatherDay[];

  granularity = signal<Granularity>("day");
  chartData: ChartData | null = null;
  startRangePercent = 0;
  endRangePercent = 100;
  chartOption: EChartsOption = {};
  isLoading = signal<boolean>(false);

  private destroy$ = new Subject<void>();
  private rangeChange$ = new Subject<void>();

  granularityOptions: { label: string; value: Granularity }[] = [
    { label: "30 minutes", value: "30min" },
    { label: "Hour", value: "hour" },
    { label: "Day", value: "day" },
    { label: "Month", value: "month" },
    { label: "Year", value: "year" },
  ];

  ngOnInit(): void {
    this.rangeChange$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateChartZoom();
      });
  }

  ngOnChanges(): void {
    if (this.consumption?.length) {
      this.chartData = this.buildChartData(this.consumption, this.weather);
      this.updateChart();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private buildChartData(
    consumption: ConsumptionPoint[],
    weather?: WeatherDay[]
  ): ChartData {
    const timestamps = consumption.map((c) =>
      new Date(c.timestampUtc).getTime()
    );
    const consumptionValues = consumption.map((c) => c.kwh);
    const temperatureValues = consumption.map((c) => {
      if (!weather?.length) return null;
      const date = new Date(c.timestampUtc).toISOString().substring(0, 10);
      const w = weather.find((wd) => wd.date === date);
      return w?.tempAvg ?? null;
    });

    const labels = consumption.map((c) => {
      const d = new Date(c.timestampUtc);
      return d.toLocaleString("en-US", {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    });

    return { timestamps, consumptionValues, temperatureValues, labels };
  }

  onGranularityChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.granularity.set(target.value as Granularity);
    this.startRangePercent = 0;
    this.endRangePercent = 100;
    this.rangeChange$.next();
  }

  onStartRangeInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.startRangePercent = parseInt(target.value, 10);
    this.endRangePercent = Math.max(
      this.endRangePercent,
      this.startRangePercent + 1
    );
    this.rangeChange$.next();
  }

  onEndRangeInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.endRangePercent = parseInt(target.value, 10);
    this.startRangePercent = Math.min(
      this.startRangePercent,
      this.endRangePercent - 1
    );
    this.rangeChange$.next();
  }

  private updateChartZoom(): void {
    // Update chart zoom based on range sliders (UI only, no API call)
    this.updateChart();
  }

  private updateChart(): void {
    if (!this.chartData) {
      return;
    }

    const { timestamps, consumptionValues, temperatureValues, labels } =
      this.chartData;

    const startIdx = Math.floor(
      (this.startRangePercent / 100) * timestamps.length
    );
    const endIdx = Math.ceil((this.endRangePercent / 100) * timestamps.length);
    const displayCount = Math.min(endIdx - startIdx, timestamps.length);

    const visibleConsumption = consumptionValues.slice(startIdx, endIdx);
    const visibleTemperature = temperatureValues.slice(startIdx, endIdx);
    const visibleLabels = labels.slice(startIdx, endIdx);

    this.chartOption = {
      backgroundColor: "#0f172a",
      textStyle: {
        color: "#94a3b8",
        fontFamily: "'Inter', sans-serif",
      },
      tooltip: {
        trigger: "axis",
        backgroundColor: "#1e293b",
        borderColor: "#334155",
        borderWidth: 1,
        textStyle: { color: "#f1f5f9" },
        formatter: (params: unknown) => {
          if (!Array.isArray(params) || params.length === 0) {
            return "";
          }
          let html = `<div style="padding:8px;">`;
          params.forEach((item: unknown) => {
            const p = item as {
              value: number | null | undefined;
              seriesName: string;
              color: string;
              axisValue: string;
            };
            if (p.value === null || p.value === undefined) return;
            const unit = p.seriesName === "Temperature" ? "°C" : "kWh";
            html += `<div style="color:${p.color};margin:4px 0;">
              ${p.seriesName}: <strong>${
              p.value?.toFixed(2) || "N/A"
            } ${unit}</strong>
            </div>`;
          });
          const first = params[0] as { axisValue: string };
          html += `<div style="color:#94a3b8;margin-top:8px;font-size:12px;">${
            first?.axisValue || ""
          }</div>`;
          html += `</div>`;
          return html;
        },
      },
      grid: {
        left: 60,
        right: 20,
        top: 20,
        bottom: 60,
        containLabel: true,
      },
      xAxis: {
        type: "category",
        data: visibleLabels,
        boundaryGap: false,
        axisLine: { lineStyle: { color: "#334155" } },
        axisLabel: {
          interval: Math.max(0, Math.floor(displayCount / 8)),
          color: "#94a3b8",
        },
        splitLine: { show: false },
      },
      yAxis: [
        {
          type: "value",
          name: "Consumption (kWh)",
          position: "left",
          axisLine: { lineStyle: { color: "#3ad1c5" } },
          axisLabel: { color: "#3ad1c5" },
          splitLine: { lineStyle: { color: "#1e293b", type: "dashed" } },
          nameTextStyle: { color: "#3ad1c5" },
        },
        {
          type: "value",
          name: "Temperature (°C)",
          position: "right",
          axisLine: { lineStyle: { color: "#ffb703" } },
          axisLabel: { color: "#ffb703" },
          splitLine: { show: false },
          nameTextStyle: { color: "#ffb703" },
        },
      ],
      series: [
        {
          name: "Consumption",
          type: "line",
          data: visibleConsumption,
          smooth: true,
          itemStyle: { color: "#3ad1c5" },
          lineStyle: { color: "#3ad1c5", width: 2.5 },
          areaStyle: { color: "rgba(58, 209, 197, 0.2)" },
          yAxisIndex: 0,
          symbol: "none",
        },
        {
          name: "Temperature",
          type: "line",
          data: visibleTemperature,
          smooth: true,
          itemStyle: { color: "#ffb703" },
          lineStyle: { color: "#ffb703", width: 2, type: "dashed" },
          yAxisIndex: 1,
          symbol: "none",
        },
      ],
      dataZoom: [
        {
          type: "slider",
          show: true,
          xAxisIndex: [0],
          start: this.startRangePercent,
          end: this.endRangePercent,
          height: 30,
          bottom: 10,
          fillerColor: "rgba(58, 209, 197, 0.2)",
          borderColor: "#334155",
          textStyle: { color: "#94a3b8" },
          handleStyle: { color: "#3ad1c5", borderColor: "#3ad1c5" },
          moveHandleSize: 8,
        },
      ],
    } as EChartsOption;
  }
}
