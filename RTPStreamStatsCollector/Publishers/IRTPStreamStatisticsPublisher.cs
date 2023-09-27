using BigTyre.RTPStreamCollector;

namespace BigTyre.RTPStreamCollector.Publishers
{
    public interface IRTPStreamStatisticsPublisher
    {
        void PublishStreamStats(RTPStreamStatistics stream);
    }
}