# Step 6: Evaluation

Before we see what ML.NET has to offer for time series forecasting, let's think about how to evaluate the quality of a forecast.
ML.NET has some built-in functions to help with this.

## Add Forecast Scoring

Add a new **ForecastScorer.cs** file with this class definition:

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;

class ForecastScorer
{
    public static RegressionMetrics Evaluate(Observation[] actual, Observation[] forecast)
    {
        IEnumerable<Comparison> comparisons = BuildComparisons(actual, forecast);

        var context = new MLContext();
        IDataView predictions = context.Data.LoadFromEnumerable(comparisons);

        RegressionMetrics regressionMetrics = context.Regression.Evaluate(
            data: predictions,
            labelColumnName: nameof(Comparison.Actual),
            scoreColumnName: nameof(Comparison.Predicted));

        return regressionMetrics;
    }

    private static IEnumerable<Comparison> BuildComparisons(Observation[] actual, Observation[] forecast)
    {
        for (int i = 0; i < actual.Length; i++)
        {
            var comparison = new Comparison(actual[i].Value, forecast[i].Value);

            yield return comparison;
        }
    }

    record Comparison(float Actual, float Predicted);
}
```

This utility will return a `RegressionMetrics` object with several measures of the quality of the forecast's fit.

## Associate Metrics With Forecasts

We want to keep these metrics with the forecast information.
Update the `ForecastDetails` class by adding a new `RegressionMetrics` property.

```csharp
record ForecastDetails(
    string AlgorithmName,
    IEnumerable<Observation> Forecast,
    RegressionMetrics RegressionMetrics);
```

This requires a new `using` statement.

```csharp
using Microsoft.ML.Data;
```

## Invoke Evaluation

Before it can compile, let's update the `Analyze` method in the **Program.cs** file.
Revise the chunk of code in the `foreach` loop handling forecasting to look like this:

```csharp
var forecasts = new List<ForecastDetails>();
Observation[] linearRegressionForecast = LinearRegressionForecaster
    .Forecast(historical, horizon, timeSeries.Interval);
RegressionMetrics linearRegressionMetrics = ForecastScorer.Evaluate(actual, linearRegressionForecast);
forecasts.Add(new ForecastDetails("Linear Regression", linearRegressionForecast, linearRegressionMetrics));
```

This will require a new `using` statement.

```csharp
using Microsoft.ML.Data;
```

## Building a Histogram for Metrics

We could write those metrics out to the console for each time series, but it'd be nice to get a visualization.
Let's build a histogram of the root-mean-square error (RMSE) for each forecast.

Add a new **HistogramBuilder.cs** file with this class definition:

```csharp
using Microsoft.FSharp.Collections;
using Plotly.NET;

static class HistogramBuilder
{
    public static GenericChart.GenericChart BuildHistogram(string groupName, ICollection<TimeSeriesAnalysis> analysisResults)
    {
        IEnumerable<Trace> traces = BuildTraces(analysisResults);
        FSharpList<Trace> fSharpTraces = ListModule.OfSeq(traces);
        GenericChart.GenericChart chart = BuildChart(groupName, fSharpTraces);
        return chart;
    }

    private static IEnumerable<Trace> BuildTraces(ICollection<TimeSeriesAnalysis> analysisResults)
    {
        ILookup<string, ForecastDetails> forecastsByAlgorithm =
            analysisResults.SelectMany(a => a.Forecasts)
            .ToLookup(o => o.AlgorithmName);

        double max = analysisResults.Max(a => a.Forecasts.Max(f => f.RegressionMetrics.RootMeanSquaredError));
        int binSize = ((int)((max + 1) / 100)) * 10;

        foreach (IGrouping<string, ForecastDetails> forecastGrouping in forecastsByAlgorithm)
        {
            var trace = new Trace("histogram");
            trace.SetValue("name", forecastGrouping.Key);
            trace.SetValue("x", forecastGrouping.Select(f => f.RegressionMetrics.RootMeanSquaredError));
            trace.SetValue("opacity", 0.75);
            trace.SetValue("autobinx", false);

            yield return trace;
        }
    }

    private static GenericChart.GenericChart BuildChart(string groupName, FSharpList<Trace> traces)
    {
        Layout layout = new Layout();
        layout.SetValue("width", 800);
        layout.SetValue("height", 500);
        layout.SetValue("showlegend", true);
        layout.SetValue("barmode", "overlay");

        return Plotly.NET.GenericChart
            .ofTraceObjects(true, traces)
            .WithLayout(layout)
            .WithTitle($"{groupName} RMSE");

    }
}
```

This is ready to group results by algorithm once we have more forecasting algorithms in use.

## Add the Histogram to the Charts

Update the `ShowCharts` method in the **Program.cs** file as follows to invoke the `HistogramBuilder`.

```csharp
static void ShowCharts(TimeSeriesAnalysis[] analysisResults)
{
    var charts = new List<GenericChart.GenericChart>();

    foreach (TimeSeriesAnalysis analysis in analysisResults)
    {
        GenericChart.GenericChart chart = ChartBuilder.BuildChart(analysis);
        charts.Add(chart);
    }

    var histogram = HistogramBuilder.BuildHistogram("Stocks", analysisResults);
    charts.Add(histogram);

    charts.ForEach(chart => chart.Show());
}
```

To avoid multiple enumeration, the parameter was changed from `IEnumerable<TimeSeriesAnalysis>` to `TimeSeriesAnalysis[]`, so the `Main` method must be updated, too.

```csharp
TimeSeriesAnalysis[] analysisResults = Analyze(stockSeries).ToArray();
```

## Test the Histogram

Let's run the program to see our histogram.
We now have the root-mean-square error (RMSE) for the linear regression for several stocks.

![alt text](./images/histogram-initial.png "Histogram of Root-Mean-Square Error")

## Next

Go to [Step 7: ML.NET Forecasting](./Step7.md).
