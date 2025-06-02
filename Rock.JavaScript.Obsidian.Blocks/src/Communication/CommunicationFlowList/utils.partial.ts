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

import { RockColor } from "@Obsidian/Core/Utilities/rockColor";
import { Chart, BarElement, ChartMeta, ChartType, Plugin, BarProps } from "@Obsidian/Libs/chart";

export function isEnumValue<T extends Record<string, number | string>>(enumObject: T, value: unknown): value is T[keyof T] {
    return Object.values(enumObject).includes(value as T[keyof T]);
}

export const BarDatasetLabelsPlugin: Plugin<"bar"> = {
    id: "barDatasetLabels",
    afterDatasetDraw(chart: Chart<"bar">, { index: datasetIndex, meta: _meta }: { index: number; meta: ChartMeta<"bar"> }) {
        const { ctx, data } = chart;

        ctx.save();
        chart.getDatasetMeta(datasetIndex).data.forEach((datapoint, _datapointIndex) => {
            ctx.font = "normal 14px sans-serif";
            ctx.fillStyle = getCssValue("--color-interface-medium");
            ctx.textAlign = "center";
            const label = `${data.datasets[datasetIndex].label}`;
            ctx.fillText(label, datapoint.x, datapoint.y - 7);
        });
    }
};

const contrastColors: Record<string, string> = {};

// Define custom plugin options
interface IBarValueLabelsPluginOptions {
    formatter?: (value: string) => string;
}

// Extend the Chart.js type definitions
declare module "@Obsidian/Libs/chart" {
    // eslint-disable-next-line @typescript-eslint/naming-convention, @typescript-eslint/no-unused-vars
    interface PluginOptionsByType<TType extends ChartType> {
        barValueLabels?: IBarValueLabelsPluginOptions;
    }
}

export const BarValueLabelsPlugin: Plugin<"bar"> = {
    id: "barValueLabels",
    afterDatasetDraw(chart: Chart<"bar">, { index: datasetIndex, meta }: { index: number; meta: ChartMeta<"bar"> }, options?: IBarValueLabelsPluginOptions) {
        const { ctx, data } = chart;

        ctx.save();
        meta.data.forEach((dataElement, dataIndex) => {
            if (dataElement instanceof BarElement === false) {
                console.warn("BarValueLabelsPlugin can only be used with BarElement data elements.", dataElement);
                return;
            }

            const barProps: (keyof BarProps)[] = ["base"];
            const { base } = dataElement.getProps(barProps);

            const fontSize = 12;
            const minimumSpaceBetweenTextAndBorder = 4;
            const desiredSpaceBetweenTextAndBar = 8;
            ctx.font = `normal ${fontSize}px sans-serif`;
            ctx.textAlign = "center";

            // Because the value is printed on top of the bar,
            // we need to figure out whether white or black text is more readable.
            const barColor = data.datasets[datasetIndex].backgroundColor?.[dataIndex] || data.datasets[datasetIndex].borderColor?.[dataIndex] || "";

            if (typeof barColor !== "string") {
                console.warn("Bar color must be a string to determine contrast color to use the BarValueLabelsPlugin.", barColor);
                return;
            }

            if (!contrastColors[barColor]) {
                // Only calculate the contrast color once per bar color.
                const color = new RockColor(barColor);
                contrastColors[barColor] = color.luminosity >= .5 ? "black" : "white";
            }

            // Use the contrast color and fallback to the CSS contrast color if not defined.
            ctx.fillStyle = contrastColors[barColor] || getCssValue("--color-interface-contrast");

            const value = `${data.datasets[datasetIndex].data[dataIndex]}`;
            const formatted = options?.formatter ? options.formatter(value) : value;

            let textOffset = (fontSize / 2) + minimumSpaceBetweenTextAndBorder;
            if (base - dataElement.y > (fontSize / 2) + desiredSpaceBetweenTextAndBar + minimumSpaceBetweenTextAndBorder) {
                textOffset = (fontSize / 2) + desiredSpaceBetweenTextAndBar;
            }

            ctx.fillText(formatted, dataElement.x, dataElement.y + textOffset);
        });
    }
};

function getCssValue(variableName: string): string {
    return getComputedStyle(document.documentElement).getPropertyValue(variableName).trim();
}