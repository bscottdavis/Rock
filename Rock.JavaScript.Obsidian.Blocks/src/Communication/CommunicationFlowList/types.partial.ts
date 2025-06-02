// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

import { ChartData, ChartOptions } from "@Obsidian/Libs/chart";
import { RockCurrency } from "@Obsidian/Utility/rockCurrency";
import { RockDateTime } from "@Obsidian/Utility/rockDateTime";
import { ChartNumericDataPointBag } from "@Obsidian/ViewModels/Reporting/chartNumericDataPointBag";

export const enum NavigationUrlKey {
    DetailPage = "DetailPage",
    PerformancePage = "PerformancePage"
}

export class LineChartDataBuilder {
    private constructor(private dataPoints: ChartNumericDataPointBag[]) { }

    public static createFromData(dataPoints: ChartNumericDataPointBag[]): LineChartDataBuilder {
        return new LineChartDataBuilder(dataPoints);
    }

    public build(): ChartData<"line"> {
        const labels = [...new Set(this.dataPoints.map(d => d.label!))];
        const seriesNames = [...new Set(this.dataPoints.map(d => d.seriesName!))];
        return {
            labels,
            datasets: seriesNames.map(seriesName => {
                const colors = this.dataPoints.filter(d => d.seriesName === seriesName).map(d => d.color!);
                return {
                    label: seriesName,
                    data: this.dataPoints.filter(d => d.seriesName === seriesName).map(d => d.value),
                    backgroundColor: colors,
                    borderColor: colors
                };
            })
        };
    }

    public withISODateLabelsAsLocaleShortDates(): LineChartDataBuilder {
        return new LineChartDataBuilder(this.dataPoints.map(d => {
            const parsedDate = RockDateTime.parseISO(d.label!);
            if (parsedDate) {
                d.label = parsedDate.toLocaleShortDateString();
            }
            return d;
        }));
    }

    public withDecimalValuesConvertedToPercentages(): LineChartDataBuilder {
        return new LineChartDataBuilder(this.dataPoints.map(d => {
            d.value = RockCurrency.create(d.value * 100, { decimalPlaces: 2 }).number;
            return d;
        }));
    }
}

export class BarChartDataBuilder {
    private constructor(private dataPoints: ChartNumericDataPointBag[]) { }

    public static createFromDataPoints(dataPoints: ChartNumericDataPointBag[]): BarChartDataBuilder {
        return new BarChartDataBuilder(dataPoints);
    }

    public build(): ChartData<"bar"> {
        const labels = [...new Set(this.dataPoints.map(d => d.seriesName!))];
        return {
            labels,
            datasets: [...new Set<string>(this.dataPoints.map(d => d.label!))].map(label => (
                {
                    label,
                    data: this.dataPoints.filter(d => d.label === label).map(d => d.value),
                    backgroundColor: this.dataPoints.filter(d => d.label === label).map(d => d.color!),
                    borderColor: this.dataPoints.filter(d => d.label === label).map(d => d.color!),
                    grouped: true
                }))
        };
    }

    public withDecimalValuesConvertedToPercentages(): BarChartDataBuilder {
        return new BarChartDataBuilder(this.dataPoints.map(d => {
            d.value = RockCurrency.create(d.value * 100, { decimalPlaces: 2 }).number;
            return d;
        }));
    }
}

export class BarChartOptionsBuilder {
    private constructor(private options: ChartOptions<"bar">) { }

    public static create(options: ChartOptions<"bar"> = {}): BarChartOptionsBuilder {
        return new BarChartOptionsBuilder(options);
    }

    public build(): ChartOptions<"bar"> {
        return { ...this.options };
    }

    /**
     * Formats the value as a percentage (with a '%' sign) in the Y-axis ticks and tooltips.
     *
     * This assumes the value is already a whole number representing a percentage (e.g., 50 for 50%).
     */
    withValueFormattedAsPercentage(): BarChartOptionsBuilder {
        return new BarChartOptionsBuilder({
            ...this.options,
            scales: {
                ...this.options.scales,
                y: {
                    ...this.options.scales?.y,
                    ticks: {
                        ...this.options.scales?.y?.ticks,
                        callback(tickValue, _index, _ticks) {
                            return `${tickValue}%`;
                        },
                    }
                }
            },
            plugins: {
                ...this.options.plugins,
                tooltip: {
                    ...this.options.plugins?.tooltip,
                    callbacks: {
                        ...this.options.plugins?.tooltip?.callbacks,
                        label: function (context) {
                            let label = context.dataset.label || "";

                            if (label) {
                                label += ": ";
                            }

                            if (context.parsed.y !== null) {
                                label += `${context.parsed.y}%`;
                            }

                            return label;
                        }
                    }
                },

                barValueLabels: {
                    ...this.options.plugins?.barValueLabels,
                    formatter: (value: string) => {
                        return `${value}%`;
                    }
                }
            }
        });
    }

    withoutLegend(): BarChartOptionsBuilder {
        return new BarChartOptionsBuilder({
            ...this.options,
            plugins: {
                ...this.options.plugins,
                legend: {
                    // Disable the legend for the bar chart and don't copy previous settings.
                    display: false
                }
            }
        });
    }

    withResponsiveSizing(): BarChartOptionsBuilder {
        return new BarChartOptionsBuilder({
            ...this.options,
            responsive: true,
            maintainAspectRatio: false
        });
    }

    withoutTooltips(): BarChartOptionsBuilder {
        return new BarChartOptionsBuilder({
            ...this.options,
            plugins: {
                ...this.options.plugins,
                tooltip: {
                    // Disable the tooltip for the bar chart and don't copy previous settings.
                    enabled: false
                }
            }
        });
    }
}

export class LineChartOptionsBuilder {
    private constructor(private options: ChartOptions<"line">) { }

    public static create(): LineChartOptionsBuilder {
        return new LineChartOptionsBuilder({});
    }

    public static createFrom(options: ChartOptions<"line"> = {}): LineChartOptionsBuilder {
        return new LineChartOptionsBuilder(options);
    }

    public build(): ChartOptions<"line"> {
        return { ...this.options };
    }

    /**
     * Formats the value as a percentage (with a '%' sign) in the Y-axis ticks and tooltips.
     *
     * This assumes the value is already a whole number representing a percentage (e.g., 50 for 50%).
     */
    withValueFormattedAsPercentage(): LineChartOptionsBuilder {
        return new LineChartOptionsBuilder({
            ...this.options,
            scales: {
                ...this.options.scales,
                y: {
                    ...this.options.scales?.y,
                    ticks: {
                        ...this.options.scales?.y?.ticks,
                        callback(tickValue, _index, _ticks) {
                            return `${tickValue}%`;
                        },
                    }
                }
            },
            plugins: {
                ...this.options.plugins,
                tooltip: {
                    ...this.options.plugins?.tooltip,
                    callbacks: {
                        ...this.options.plugins?.tooltip?.callbacks,
                        label: function (context) {
                            let label = context.dataset.label || "";

                            if (label) {
                                label += ": ";
                            }

                            if (context.parsed.y !== null) {
                                label += `${context.parsed.y}%`;
                            }

                            return label;
                        }
                    }
                }
            }
        });
    }

    withResponsiveSizing(): LineChartOptionsBuilder {
        return new LineChartOptionsBuilder({
            ...this.options,
            responsive: true,
            maintainAspectRatio: false
        });
    }
}
