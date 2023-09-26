namespace BigTyre.RTPStreamCollector;

public record RTPStreamPacket(double Timestamp, long RTPSequenceNumber, long RTPTimestamp);

public class RTPPacketCaptureRow
{
    public RTPPacketCaptureRow(
        double timestamp, 
        string sourceIP, 
        string destinationIP, 
        int sourcePort, 
        int destinationPort, 
        long rtpSequenceNumber, 
        long rtpTimestamp, 
        string ssrc
    )
    {
        Timestamp = timestamp;
        SourceIP = sourceIP ?? throw new ArgumentNullException(nameof(sourceIP));
        DestinationIP = destinationIP ?? throw new ArgumentNullException(nameof(destinationIP));
        SourcePort = sourcePort;
        DestinationPort = destinationPort;
        RTPSequenceNumber = rtpSequenceNumber;
        RTPTimestamp = rtpTimestamp;
        SSRC = ssrc ?? throw new ArgumentNullException(nameof(ssrc));
    }

    public double Timestamp { get; set; }
    public string SourceIP { get; set; }
    public string DestinationIP { get; set; }
    public long SourcePort { get; set; }
    public long DestinationPort { get; set; }
    public long RTPSequenceNumber { get; set; }
    public long RTPTimestamp { get; set; }
    public string SSRC { get; set; }

    internal string CalculateStreamIdentifier()
    {
        return $"{DestinationIP}-{DestinationPort}-{SSRC}";
    }

    public long JitterMs => CalculateJitterMs();

    private long CalculateJitterMs()
    {

        return 0;
    }
}