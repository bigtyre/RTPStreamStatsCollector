namespace BigTyre.RTPStreamCollector.Publishers
{
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
                try
                {
                    publisher.PublishStreamStats(stream);
                }
                catch (Exception ex)
                {
                    var publisherName = publisher.GetType().Name;
                    Console.WriteLine($"An error occurred while publishing using {publisherName}: " + ex.ToString());
                }
            }
        }
    }
}