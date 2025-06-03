Flows Instance Drop-Down
    - const flowInstanceItems = computed<ListItemBag[] | null | undefined>(() => {
        return config.communicationFlow?.instances?.map( i => ({
            value: i.guid,
            text: RockDateTime.parseISO(i.startDateTime).toLocale("dddd, MMM. d, yyyy")
        }));
    });

Conversion Goal Progress
    - CommunicationFlow.ConversionGoalSettings`custom logic (see communicationFlowDetail.obs)`

Flows Key Metrics
    Conversions
        - selectedInstances.Select( i => i.Conversions.Count ).Sum()
    Average Time Before Conversion
        - foreach (instance in selectedInstances)
        {
            var instanceStartDate = instance.StartDateTime;

            conversionTimes.AddRange( instance.ConversionHistories.Select( ch => TimeSpan.FromTicks( ( ch.Date - instanceStartTime ).Ticks ) ) );
        }

        return conversionTimes.Average();
    Unsubscribes From Flow/All
        - var unsubscribedRecipients = selectedInstances.SelectMany( i => i.Recipients ).Where( r => r.CausedUnsubscribe )

        return new {
            UnsubscribesFromFlow = unsubscribedRecipients.Count( r => r.UnsubscribeLevel == Flow ),
            UnsubscribesFromAll = unsubscribedRecipients.Count( r => r.UnsubscribeLevel == All ),
            UnsubscribesFromOther = unsubscribedRecipients.Count( r => ![Flow, All].Contains( r.UnsubscribeLevel ) )
        }

Total Flow Performance Over Time
    - // Conversion Rate Series
    var dataPoints = new List<NumericChartDataPointBag>();
    var selectedInstancesTotalRecipientCount = selectedInstances.Sum( i => i.Recipients.Count )
     var datesWithConversionHistories = selectedInstances.SelectMany( i => i.ConversionHistories ).OrderBy( ch => ch.Date ).GroupBy( ch => ch.Date );
    var accummulatedCount = 0;
    foreach ( var grouping in datesWithConversionHistories ) {
        var date = grouping.Key;

        var conversionsOnDate = grouping.Count();
        accummulatedCount += converstionOnDate;
        dataPoints.Add( new NumericChartDataPointBag
        {
            Value = ( (decimal) accummulatedCount / (decimal) selectedInstancesTotalRecipientCount ) * 100,
            Label = date.ToISODateString(),
            SeriesName: "Conversion Rate"
            Color = "#68D391"
        })
    }

    // Unsubscribes Series

    function getUnsubscribeRates( instances )
    {
        var instancesTotalRecipientsCount = ( decimal )instances.Sum( i => i.Recipients.Count );
        var dataPoints = new List<NumericChartDataPointBag>();
        var datesWithUnsubscribes = instances.SelectMany( i => i.Recipients ).Where( r => r.CausedUnsubscribe ).OrderBy( r => r.UnsubscribeDateTime ).GroupBy( r => r.UnsubscribeDateTime );
        var accummulatedCount = 0m;
        foreach ( var grouping in datesWithUnsubscribes ) {
            var date = grouping.Key;

            var unsubscribesOnDate = grouping.Count();
            accummulatedCount += unsubscribesOnDate;
            dataPoints.Add( new NumericChartDataPointBag
            {
                Value = ( accummulatedCount / instancesTotalRecipientsCount ) * 100,
                Label = date.ToISODateString(),
                SeriesName: "Conversion Rate"
                Color = "#68D391"
            })
        }
    }

    dataPoints.AddRange( getUnsubscribeRates( selectedInstances ) );

Instance vs. Flow Performance Average
    - // NOT SPECIFIC TO SELECTED INSTANCE(S) AND ONLY VISIBLE FOR NON-ONE-TIME FLOWS
    var dataPoints = new List<NumericChartDataPointBag>();

    // Instance(s) Series
    function getSeriesName(instances, index) {
        if (instances.length === 1) {
            return "Instance";
        }
        else {
            return `Instance ${instances[0].startDateTime}`;
        }
    }

    selectedInstances.forEach((instance, i) => {
        const recipientCount = instance.recipients?.length;
        const conversionCount = instance.conversions?.length;
        const unsubscribeCount = instance.recipients && Enumerable.from(instance.recipients).where(r => r.causedUnsubscribe).count();
        const seriesName = selectedInstances.length === 1 ? "Instance" : RockDateTime.parseISO(instance.startDateTime)?.toLocaleShortDateString();

        const conversionRateDataPoint: NumericChartDataPointBag = {
            seriesName,
            label: "Conversion Rate",
            value: 0,
            color: "#38A169"
        }

        if (recipientCount && conversionCount) {
            conversionRateDataPoint.value = conversionCount / recipientCount * 100
        }

        dataPoints.push(conversionRateDataPoint);

        const unsubscribeRateDataPoint: NumericChartDataPointBag = {
            seriesName,
            label: "Unsubscribe Rate",
            value: 0,
            color: "#E53E3E"
        };

        if (recipientCount && unsubscribeCount) {
            unsubscribeRateDataPoint.value = unsubscribeCount / recipientCount * 100;
        }

        dataPoints.push(unsubscribeRateDataPoint);
    });

    allInstances.forEach((instance, i) => {
        const recipientCount = instance.recipients?.length;
        const conversionCount = instance.conversions?.length;
        const unsubscribeCount = instance.recipients && Enumerable.from(instance.recipients).where(r => r.causedUnsubscribe).count();
        const seriesName = selectedInstances.length === 1 ? "Instance" : RockDateTime.parseISO(instance.startDateTime)?.toLocaleShortDateString();

        const conversionRateDataPoint: NumericChartDataPointBag = {
            seriesName,
            label: "Conversion Rate",
            value: 0,
            color: "#38A169"
        }

        if (recipientCount && conversionCount) {
            conversionRateDataPoint.value = conversionCount / recipientCount * 100
        }

        dataPoints.push(conversionRateDataPoint);

        const unsubscribeRateDataPoint: NumericChartDataPointBag = {
            seriesName,
            label: "Unsubscribe Rate",
            value: 0,
            color: "#E53E3E"
        };

        if (recipientCount && unsubscribeCount) {
            unsubscribeRateDataPoint.value = unsubscribeCount / recipientCount * 100;
        }

        dataPoints.push(unsubscribeRateDataPoint);
    });

    var selectedInstancesTotalRecipientsCount = ( decimal ) selectedInstances.Sum( i => i.Recipients.Count );
    var selectedInstancesTotalConversionCount = ( decimal ) selectedInstances.Sum( i => i.ConversionHistories.Count );
    dataPoints.Add( new NumericChartDataPointBag {
        SeriesName = "Instance"
        Label = "Conversion Rate",
        Value = ( selectedInstancesTotalConversionCount / selectedInstancesTotalRecipientsCount ) * 100,
        Color = "",
    });

    var selectedInstancesTotalUnsubscribeCount = ( decimal ) selectedInstances.Sum( i => i.Recipients.Where( r => r.CausedUnsubscribe ).Count() );
    dataPoints.Add( new NumericChartDataPointBag {
        SeriesName: "Instance",
        Label: "Unsubscribe Rate",
        Value: ( selectedInstancesTotalUnsubscribeCount / selectedInstancesTotalRecipientsCount ) * 100,
        Color: "#E53E3E"
    });

    // Average Series
    var totalRecipientsCount = ( decimal ) flow.Instances.Sum( i => i.Recipients.Count );
    var selectedInstancesTotalConversionCount = ( decimal ) selectedInstances.Sum( i => i.ConversionHistories.Count );
    dataPoints.Add( new NumericChartDataPointBag {
        SeriesName = "Average"
        Label = "Conversion Rate",
        Value = ( selectedInstancesTotalConversionCount / totalRecipientsCount ) * 100,
        Color = "",
    });

    var selectedInstancesTotalUnsubscribeCount = ( decimal ) selectedInstances.Sum( i => i.Recipients.Where( r => r.CausedUnsubscribe ).Count() );
    dataPoints.Add( new NumericChartDataPointBag {
        SeriesName: "Instance",
        Label: "Unsubscribe Rate",
        Value: ( selectedInstancesTotalUnsubscribeCount / selectedInstancesTotalRecipientsCount ) * 100,
        Color: "#E53E3E"
    });

    // Average Series