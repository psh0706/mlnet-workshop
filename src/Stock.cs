using Microsoft.ML.Data;

class Stock
{
    [LoadColumn(0)] public DateTime Date = DateTime.MinValue;
    [LoadColumn(1)] public string Name = string.Empty;
    [LoadColumn(2)] public float Open = 0;
    [LoadColumn(3)] public float Close = 0;
    [LoadColumn(4)] public float High = 0;
    [LoadColumn(5)] public float Low = 0;
    [LoadColumn(6)] public float Volume = 0;

    internal Observation ToObservation()
    {
        return new Observation(Date, Close);
    }
}