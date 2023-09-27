using Microsoft.VisualBasic;
using System.IO;

namespace BigTyre.RTPStreamCollector;

public class RTPStreamStatistics
{
    private readonly List<RTPStreamPacket> _rows = new();

    public int NumberOfPacketsReceived { get; private set; }

    public string SourceIP { get; }
    public string DestinationIP { get; }
    public long SourcePort { get; }
    public long DestinationPort { get; }
    public string SSRC { get; }

    public DateTime CreationTime { get; private set; }
    public DateTime LastPacketReceived { get; private set; }

    public RTPStreamStatistics(
        string sourceIP,
        string destinationIP,
        long sourcePort,
        long destinationPort,
        string ssrc
    )
    {
        SourceIP = sourceIP;
        DestinationIP = destinationIP;
        SourcePort = sourcePort;
        DestinationPort = destinationPort;
        SSRC = ssrc;
        CreationTime = DateTime.Now;
        LastPacketReceived = DateTime.Now;
    }

    public void AddRow(RTPPacketCaptureRow row)
    {
        var record = new RTPStreamPacket(row.Timestamp, row.RTPSequenceNumber, row.RTPTimestamp);
        _rows.Add(record);
        NumberOfPacketsReceived++;
        LastPacketReceived = DateTime.Now;
    }

    public IEnumerable<TimeSpan> CalculateJitter()
    {
        if (_rows.Count < 2) 
            yield break;

        var rtpRate = 1.0 / 8000.0;

        TimeSpan jitter = TimeSpan.Zero;

        for (int i = 1; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var previousRow = _rows[i - 1];

            var arrivalTime = row.Timestamp;
            var previousArrivalTime = previousRow.Timestamp;

            var diff = (arrivalTime - previousArrivalTime);

            var rtpTimestamp = row.RTPTimestamp;
            var previousRtpTimestamp = previousRow.RTPTimestamp;

            var rtpDifference = (rtpTimestamp - previousRtpTimestamp) * rtpRate;

            var differenceInRelativeTransitTimes = Math.Abs(diff - rtpDifference);

            jitter += TimeSpan.FromSeconds((differenceInRelativeTransitTimes - jitter.TotalSeconds) / 16);

            yield return jitter;
        }
    }

    public IEnumerable<TimeSpan> CalculateDeltas()
    {
        if (_rows.Count < 2)
            yield break;

        for (int i = 1; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var previousRow = _rows[i-1];

            var arrivalTime = row.Timestamp;
            var previousArrivalTime = previousRow.Timestamp;

            var difference = arrivalTime - previousArrivalTime;

            yield return TimeSpan.FromSeconds(difference);
        }
    }

    internal StatsSummary<double>? GetDeltaStats()
    {
        var deltas = CalculateDeltas().Select(s => s.TotalMilliseconds).Where(s => s > 0).ToList();
        if (deltas.Any() is false) 
            return null;

        double minDelta = deltas.Min();
        double meanDelta = deltas.Average();
        double maxDelta = deltas.Max();

        return new(minDelta, meanDelta, maxDelta);
    }

    internal StatsSummary<double>? GetJitterStats()
    {
        var jitterValues = CalculateJitter().Select(s => s.TotalMilliseconds).ToList();
        if (jitterValues.Count < 2)
            return null;

        var minJitter = jitterValues.Min();
        var meanJitter = jitterValues.Average();
        var maxJitter = jitterValues.Max();

        return new(minJitter, meanJitter, maxJitter);
    }
}

public record StatsSummary<T>(T Min, T Average, T Max);
