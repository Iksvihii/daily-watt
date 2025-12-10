# Dynamic Range Selection Feature

## Overview
Implemented dynamic range and time scale selection for the consumption chart. When users change the time scale (hour/day/month/year) or adjust the range sliders, the frontend sends a request to the backend to fetch only the aggregated data needed, rather than processing all data on the client side.

## Architecture

### Frontend Changes

#### ConsumptionChartComponent (`consumption-chart.component.ts`)
- **New Inputs**: 
  - `dateRangeStart`: ISO string representing the full date range start
  - `dateRangeEnd`: ISO string representing the full date range end

- **New Signal**: 
  - `isLoading`: Tracks whether a request is in flight

- **New Methods**:
  - `fetchDataWithRange()`: Calculates the exact date range based on percentage sliders and time scale, then calls the backend service
  - `onTimeScaleChange()`: Triggers a new data fetch when time scale changes
  - `onStartRangeInput()` / `onEndRangeInput()`: Triggers a new data fetch with debouncing when range sliders are adjusted

- **Granularity Mapping**: Maps UI time scales to backend granularities:
  - `hour` → `'hour'`
  - `day` → `'day'`
  - `month` → `'day'` (frontend aggregates monthly data)
  - `year` → `'day'` (frontend aggregates yearly data)

- **RxJS Integration**: 
  - Uses `debounceTime(300)` to avoid excessive requests while sliding
  - Uses `takeUntil(destroy$)` for proper subscription cleanup

#### DashboardService (`dashboard.service.ts`)
- **New Method**: `getTimeSeriesWithRange()`
  - Parameters: `from`, `to`, `granularity`, `startDate`, `endDate`, `withWeather`
  - Returns aggregated data for the specified range at the requested granularity

#### Template Updates (`consumption-chart.component.html`)
- Disabled controls during loading state
- Added loading spinner with animation overlay
- Controls are visually disabled while fetching data

#### Dashboard Template (`dashboard.component.html`)
- Passes `dateRangeStart` and `dateRangeEnd` signals to the chart component

### Backend Changes

#### DashboardController (`DashboardController.cs`)
- **Updated Endpoint**: `GET /api/dashboard/timeseries`
- **New Optional Parameters**:
  - `startDate`: DateTime - the start of the range to fetch
  - `endDate`: DateTime - the end of the range to fetch

- **Logic**:
  - If `startDate` and `endDate` are provided, they are used for querying
  - If not provided, the full `from`-`to` range is used (backward compatible)
  - Calls `IConsumptionService.GetAggregatedAsync()` with the calculated range
  - Raw 30-minute interval data is aggregated by the service based on the specified granularity

## Data Flow

1. **User Action**: User changes time scale or adjusts range sliders
2. **Frontend**: `ConsumptionChartComponent` calculates the actual date range
   - Start: `from + (startRangePercent / 100) * (to - from)`
   - End: `from + (endRangePercent / 100) * (to - from)`
3. **Request**: Frontend calls `DashboardService.getTimeSeriesWithRange()` with:
   - Original range (`from`, `to`)
   - Specific range (`startDate`, `endDate`)
   - Desired granularity
4. **Backend Processing**:
   - Queries raw consumption data (30-min intervals) from database
   - Aggregates to the requested granularity (hour/day)
   - Returns only the data needed for the specified range
5. **Response**: Frontend receives aggregated data
6. **Rendering**: Chart is updated with the new data set

## Performance Benefits

- **Reduced Data Transfer**: Backend only returns aggregated data for the requested range
- **Optimized Queries**: Database queries are filtered by the actual date range, not the full dataset
- **Client-side Efficiency**: No need to filter/aggregate all historical data on the frontend

## Backward Compatibility

The feature is fully backward compatible:
- If `startDate` and `endDate` are not provided, the endpoint behaves as before
- Existing clients continue to work without modification

## Testing Considerations

- Test changing time scales with different ranges
- Test edge cases: min/max range, single day selections
- Verify loading state displays correctly during requests
- Test error handling if backend requests fail
- Verify debouncing prevents excessive requests during rapid slider movements
