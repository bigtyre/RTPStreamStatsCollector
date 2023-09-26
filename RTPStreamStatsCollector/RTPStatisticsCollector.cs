using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigTyre.RTPStreamCollector;

public class RTPStatisticsCollector
{
    private Dictionary<string, RTPStreamStatistics> StreamStatistics { get; } = new();

    public IReadOnlyList<RTPStreamStatistics> GetStreamStats()
    {
        return StreamStatistics.Values.ToList().AsReadOnly();
    }
    public RTPStreamStatistics GetStreamStats(RTPPacketCaptureRow row)
    {
        var streamId = row.CalculateStreamIdentifier();

        RTPStreamStatistics streamStatistics;
        if (StreamStatistics.TryGetValue(streamId, out var stats))
        {
            streamStatistics = stats;
        }
        else
        {
            streamStatistics = new(
                sourceIP: row.SourceIP, 
                destinationIP: row.DestinationIP, 
                sourcePort: row.SourcePort, 
                destinationPort: row.DestinationPort, 
                ssrc: row.SSRC
            );
            StreamStatistics[streamId] = streamStatistics;
        }

        return streamStatistics;
    }

    internal void RemoveStream(RTPStreamStatistics stream)
    {
        var keys = StreamStatistics.Where(s => s.Value == stream).Select(s => s.Key).ToList();
        foreach ( var key in keys)
        {
            StreamStatistics.Remove(key);
        }
    }
}
