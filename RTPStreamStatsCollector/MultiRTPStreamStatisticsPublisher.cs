using BigTyre.RTPStreamCollector;

public class MultiRTPStreamStatisticsPublisher : IRTPStreamStatisticsPublisher
{
    public MultiRTPStreamStatisticsPublisher(params IRTPStreamStatisticsPublisher[] publishers)
    {
        Publishers.AddRange(publishers);
    }

    public List<IRTPStreamStatisticsPublisher> Publishers { get; } = new();

    public void PublishStreamStats(RTPStreamStatistics stream)
    {
        foreach (var publisher in Publishers)
        {
            publisher.PublishStreamStats(stream);
        }
    }
}
