using Microsoft.VisualBasic;

namespace BigTyre.RTPStreamCollector;

public class RTPStreamStatistics
{
    private readonly List<RTPStreamPacket> _rows = new();

    public int NumPackets { get; private set; }

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
        NumPackets++;
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

    public IEnumerable<TimeSpan> CalculateDifferences()
    {
        if (_rows.Count < 2)
            yield break;

        var rtpRate = 1f / 8000;

        var first = _rows[0];
        var firstTimestamp = first.Timestamp;
        var rtpTimeStart = first.RTPTimestamp;

        for (int i = 1; i < _rows.Count; i++)
        {
            var row = _rows[i];

            var arrivalTime = row.Timestamp;
            var rtpTimestamp = row.RTPTimestamp;

            var diff = rtpTimestamp - rtpTimeStart;
            double expectedTime = diff * rtpRate;

            expectedTime += firstTimestamp;

            var difference = arrivalTime - expectedTime;

            yield return TimeSpan.FromSeconds(difference);
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
}
