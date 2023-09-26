using BigTyre.RTPStreamCollector;

public class ConsoleRTPStreamStatisticsPublisher : IRTPStreamStatisticsPublisher
{
    public void PublishStreamStats(RTPStreamStatistics stream)
    {
        Console.WriteLine();
        Console.WriteLine($"RTP Stream stats - {stream.SourceIP}:{stream.SourcePort} to {stream.DestinationIP}:{stream.DestinationPort}, SSRC {stream.SSRC}");
        var jitterValues = stream.CalculateJitter().Select(s => s.TotalMilliseconds).ToList();
        if (jitterValues.Count < 2)
            return;

        var minJitter = jitterValues.Min();
        var meanJitter = jitterValues.Average();
        var maxJitter = jitterValues.Max();

        Console.WriteLine($"Jitter (ms) - min: {minJitter:f3}, mean: {meanJitter:f3}, max: {maxJitter:f3}");

        var deltas = stream.CalculateDeltas().Select(s => s.TotalMilliseconds).Where(s => s > 0).ToList();
        if (deltas.Any())
        {
            double minDelta = deltas.Min();
            double meanDelta = deltas.Average();
            double maxDelta = deltas.Max();
            Console.WriteLine($"Delta (ms) - min: {minDelta:f3}ms, mean: {meanDelta:f3}ms, max: {maxDelta:f3}ms");
        }
    }
}