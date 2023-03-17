using Microsoft.ML;

class StockLoader
{
    public static TimeSeries[] Load()
    {
        // Create an ML.NET machine learning context.
        var context = new MLContext();

        // Get the path to the CSV file.
        string? rootFolder = Directory.GetParent(Environment.CurrentDirectory)?.FullName;
        if (rootFolder == null)
        {
            throw new Exception("Could not find root folder.");
        }
        string csvFile = Path.Combine(rootFolder, "data", "big_five_stocks.csv");

        // Load data from the CSV file.
        Console.WriteLine($"Loading stocks from '{csvFile}'...");
        IDataView dataView = context.Data.LoadFromTextFile<Stock>(
            path: csvFile,
            hasHeader: true,
            separatorChar: ',');
        IEnumerable<Stock> stocks = context.Data.CreateEnumerable<Stock>(
            data: dataView,
            reuseRowObject: false);

        // Group rows by stock name.  Create a TimeSeries for each group.
        TimeSeries[] timeSeriesList = stocks
            .ToLookup(stock => stock.Name)
            .Select(group => new TimeSeries(
                Name: group.Key,
                Observations: group.Select(s => s.ToObservation())))
            .ToArray();

        return timeSeriesList;
    }
}