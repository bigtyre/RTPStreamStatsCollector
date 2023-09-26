using BigTyre.RTPStreamCollector;

public interface IRTPStreamStatisticsPublisher
{
    void PublishStreamStats(RTPStreamStatistics stream);
}
