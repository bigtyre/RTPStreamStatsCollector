using MySql.Data.MySqlClient;

namespace BigTyre.RTPStreamCollector.Publishers;

internal class MySQLRTPStreamStatisticsPublisher : IRTPStreamStatisticsPublisher
{
    private string ConnectionString { get; }

    public MySQLRTPStreamStatisticsPublisher(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace.", nameof(connectionString));
        }

        ConnectionString = connectionString;
    }

    public void PublishStreamStats(RTPStreamStatistics stream)
    {
        Console.WriteLine("Saving to database");

        var jitter = stream.GetJitterStats();
        if (jitter is null) return;

        var delta = stream.GetDeltaStats();
        if (delta is null) return;

        string query = @"
            INSERT INTO `rtp_streams` (
                `start_time`,
                `finish_time`,
                `source_ip`,
                `source_port`,
                `destination_ip`,
                `destination_port`,
                `min_jitter_ms`,
                `avg_jitter_ms`,
                `max_jitter_ms`,
                `min_delta_ms`,
                `avg_delta_ms`,
                `max_delta_ms`,
                `num_packets`
            )
            VALUES
            (
                @start_time,
                @finish_time,
                @source_ip,
                @source_port,
                @destination_ip,
                @destination_port,
                @min_jitter_ms,
                @avg_jitter_ms,
                @max_jitter_ms,
                @min_delta_ms,
                @avg_delta_ms,
                @max_delta_ms,
                @num_packets
            );";

        using var connection = new MySqlConnection(ConnectionString);
        using var command = new MySqlCommand(query, connection);
        
        connection.Open();

        // Define parameters and set their values
        command.Parameters.AddWithValue("@start_time", stream.CreationTime);
        command.Parameters.AddWithValue("@finish_time", stream.LastPacketReceived);
        command.Parameters.AddWithValue("@source_ip", stream.SourceIP);
        command.Parameters.AddWithValue("@source_port", stream.SourcePort);
        command.Parameters.AddWithValue("@destination_ip", stream.DestinationIP);
        command.Parameters.AddWithValue("@destination_port", stream.DestinationPort);
        command.Parameters.AddWithValue("@min_jitter_ms", jitter.Min);
        command.Parameters.AddWithValue("@avg_jitter_ms", jitter.Average);
        command.Parameters.AddWithValue("@max_jitter_ms", jitter.Max);
        command.Parameters.AddWithValue("@min_delta_ms", delta.Min);
        command.Parameters.AddWithValue("@avg_delta_ms", delta.Average);
        command.Parameters.AddWithValue("@max_delta_ms", delta.Max);
        command.Parameters.AddWithValue("@num_packets", stream.NumberOfPacketsReceived);

        // Execute the INSERT query
        int rowsAffected = command.ExecuteNonQuery();

        Console.WriteLine("Saved to database successfully.");
    }
}