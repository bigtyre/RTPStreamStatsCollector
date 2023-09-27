using BigTyre.RTPStreamCollector;
using BigTyre.RTPStreamCollector.Publishers;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Globalization;
using MissingFieldException = CsvHelper.MissingFieldException;

// This program is designed to run with Tshark, a command line tool from Wireshark
// tshark -i YOUR_INTERFACE_NAME -Y "rtp" -q  -T fields -e frame.time_epoch -e ip.src -e ip.dst -e udp.srcport -e udp.dstport -e rtp.seq -e rtp.timestamp -e rtp.ssrc -E header=y -E separator=, | RTPStreamStatsCollector
// YOUR_INTERFACE_NAME should be the network interface you want to capture, e.g. ens32
// This will default to running tshark itself but you can also pipe the output from tshark to this program if you are using it in console mode instead.

var configBuilder = new ConfigurationBuilder();
configBuilder.AddJsonFile("appsettings.json", optional: true);
#if DEBUG
configBuilder.AddUserSecrets<Program>();
#endif

var config = configBuilder.Build();
var settings = new AppSettings();
config.Bind(settings);

var mysqlConnectionString = settings.MySQLConnectionString ?? throw new Exception("MySQL connection string not configured.");

var publishThresholdSeconds = settings.PublishThresholdSeconds ?? 30;
if (publishThresholdSeconds < 2) 
    throw new Exception("Configured publish threshold is too low. It must be at least 2 seconds.");
if (publishThresholdSeconds > 60 * 60 * 24) 
    throw new Exception("Configured publish threshold is too high. It must be less than 1 day.");

var publishThreshold = TimeSpan.FromSeconds(publishThresholdSeconds);
var publisher = new MultiRTPStreamStatisticsPublisher(
    new ConsoleRTPStreamStatisticsPublisher(),
    new MySQLRTPStreamStatisticsPublisher(mysqlConnectionString)
);

Console.WriteLine($"RTP stats collection starting.");
Console.WriteLine($"Stream stats will be published after {publishThreshold.TotalSeconds} seconds of inactivity");
var stats = new RTPStatisticsCollector();

Process? tsharkProcess = null;
StreamReader inputStreamReader;

var mode = InputMode.TShark;
#if DEBUG
mode = InputMode.SampleFile;
#endif

switch (mode)
{

    case InputMode.SampleFile:
        {
            var inputStream = File.OpenRead("packets-edited.csv");
            inputStreamReader = new StreamReader(inputStream);
        }
        break;

    case InputMode.TShark:
        {
            var interfaceName = "ens32";
            string tsharkArguments = $"-i {interfaceName} -Y \"rtp\" -q -T fields -e frame.time_epoch -e ip.src -e ip.dst -e udp.srcport -e udp.dstport -e rtp.seq -e rtp.timestamp -e rtp.ssrc -E header=y -E separator=,";

            // Create a new process to run tshark
            var process = new Process();
            process.StartInfo.FileName = "tshark";
            process.StartInfo.Arguments = tsharkArguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            // Start the process
            process.Start();

            Console.WriteLine($"Started TShark packet capture on {interfaceName}");

            // Get the standard output stream of the process
            inputStreamReader = process.StandardOutput;

            tsharkProcess = process;
        }
        break;

    case InputMode.Console:
    default:
        {
            var inputStream = Console.OpenStandardInput();
            inputStreamReader = new StreamReader(inputStream);
        }
        break;
}

using var csvReader = new CsvReader(inputStreamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = false
});

using var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
var publishTask = Task.Run(() => PublishRTPStatsAtInterval(stats, cancellationToken));

async void PublishRTPStatsAtInterval(RTPStatisticsCollector stats, CancellationToken cancellationToken)
{
    try
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            var streamStats = stats.GetStreamStats();
            foreach (var stream in streamStats)
            {
                var lastPacketReceivedAt = stream.LastPacketReceived;
                var timeSinceLastPacket = DateTime.Now - lastPacketReceivedAt;
                if (timeSinceLastPacket < publishThreshold)
                    continue;

                stats.RemoveStream(stream);
                publisher.PublishStreamStats(stream);
            }

            await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Publish task cancelled.");
    }
}

while (csvReader.Read())
{
    try
    {
        var row = csvReader.GetRecord<RTPPacketCaptureRow>();
        if (row is null)
            continue;

        //Console.WriteLine($"Row received. Source IP: {row.SourceIP}. Timestamp: {row.Timestamp}");
        var statsCollection = stats.GetStreamStats(row);
        statsCollection.AddRow(row);
    }
    catch (MissingFieldException)
    {
        Console.WriteLine($"Missing Field Error.");
    }
    catch (TypeConverterException)
    {
        Console.WriteLine("Ignored row. Type conversion errors were encountered.");
        //Console.WriteLine($"Type Converter Error. " + ex.Message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

cancellationTokenSource.Cancel();
await publishTask;

var streamStats = stats.GetStreamStats();
foreach (var stream in streamStats)
{
    publisher.PublishStreamStats(stream);
}

if (tsharkProcess is not null)
{
    tsharkProcess.WaitForExit();
    tsharkProcess.Close();
}

enum InputMode
{
    SampleFile,
    TShark,
    Console
}
