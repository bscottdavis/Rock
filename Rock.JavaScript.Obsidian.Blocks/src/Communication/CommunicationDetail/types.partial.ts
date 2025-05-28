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

import { CommunicationType } from "@Obsidian/Enums/Communication/communicationType";
import { Guid } from "@Obsidian/Types";
import { PersonFieldBag } from "@Obsidian/ViewModels/Core/Grid/personFieldBag";

export const enum TabItem {
    Analytics = "Analytics",
    MessageDetails = "Message Details",
    RecipientDetails = "Recipient Details"
}

export const enum PerformanceChartItem {
    Time = "Time",
    Flow = "Flow"
}

export const enum PerformanceChartTimeframe {
    First45Days = "First 45 Days",
    AllTime = "All Time"
}

export type ChartStyles = {
    fontFamily: string;
    fontColor: string;
    fontSize: number;
    fontWeight: string;
    legendBoxSize: number;
};

export const enum PreferenceKey {
    RecipientGridSettings = "recipient-list-settings"
}

export type RecipientGridSettingsOptions = {
    basicColumns: string[];
    additionalColumns: Guid[];
};

export type RecipientGridRow = {
    idKey: string;
    person?: PersonFieldBag | null;
    lastActivityDateTime?: string | null;
    opensCount?: number | null;
    clicksCount?: number | null;
    communicationType?: CommunicationType | null;
    wasDelivered: boolean;
    wasFailedDelivery: boolean;
    openedDateTime?: string | null;
    clickedDateTime?: string | null;
    unsubscribedDateTime?: string | null;
    markedAsSpamDateTime?: string | null;
};
