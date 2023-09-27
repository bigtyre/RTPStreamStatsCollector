namespace BigTyre.RTPStreamCollector.Publishers
{
    public class ConsoleRTPStreamStatisticsPublisher : IRTPStreamStatisticsPublisher
    {
        public void PublishStreamStats(RTPStreamStatistics stream)
        {
            Console.WriteLine();
            Console.WriteLine($"RTP Stream stats - {stream.SourceIP}:{stream.SourcePort} to {stream.DestinationIP}:{stream.DestinationPort}, SSRC {stream.SSRC}");
            var jitter = stream.GetJitterStats();
            if (jitter is not null) { 
                var minJitter = jitter.Min;
                var meanJitter = jitter.Average;
                var maxJitter = jitter.Max;

                Console.WriteLine($"Jitter (ms) - min: {minJitter:f3}, mean: {meanJitter:f3}, max: {maxJitter:f3}");
            }

            var delta = stream.GetDeltaStats();
            if (delta is not null)
            {
                double minDelta = delta.Min;
                double meanDelta = delta.Average;
                double maxDelta = delta.Max;
                Console.WriteLine($"Delta (ms) - min: {minDelta:f3}ms, mean: {meanDelta:f3}ms, max: {maxDelta:f3}ms");
            }
        }
    }
}