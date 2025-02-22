using DeleteDefect.Models;
using Microsoft.AspNetCore.SignalR;
using DeleteDefect.Hubs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class DefectTableListener
{
    private readonly IHubContext<DefectHub> _hubContext;
    private readonly string _connectionString;
    private DateTime? _lastMaxDateTime;
    private bool _isRunning = false;

    public DefectTableListener(IHubContext<DefectHub> hubContext, string connectionString)
    {
        _hubContext = hubContext;
        _connectionString = connectionString;
    }

    public void StartMonitoring()
    {
        _isRunning = true;
        Task.Run(async () =>
        {
            while (_isRunning)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // 🔹 Cek apakah ada data terbaru berdasarkan MAX(DateTime)
                        using (var checkCommand = new SqlCommand("SELECT MAX(DateTime) FROM Defect_Results", connection))
                        {
                            var result = await checkCommand.ExecuteScalarAsync();
                            if (result != DBNull.Value)
                            {
                                DateTime newMaxDateTime = Convert.ToDateTime(result);

                                if (_lastMaxDateTime == null || newMaxDateTime > _lastMaxDateTime)
                                {
                                    _lastMaxDateTime = newMaxDateTime;

                                    // 🔹 Jika ada perubahan, baru ambil ID & DefectName
                                    using (var detailCommand = new SqlCommand(@"
                                        SELECT TOP 1 dr.Id, d.DefectName
                                        FROM Defect_Results dr
                                        JOIN Defect_Names d ON dr.DefectId = d.Id
                                        WHERE dr.DateTime = @DateTime
                                        ORDER BY dr.DateTime DESC", connection))
                                    {
                                        detailCommand.Parameters.AddWithValue("@DateTime", newMaxDateTime);
                                        using (var reader = await detailCommand.ExecuteReaderAsync())
                                        {
                                            if (reader.Read())
                                            {
                                                int defectId = reader.GetInt32(0);
                                                string defectName = reader.GetString(1);

                                                Console.WriteLine($"🔔 Data baru! ID: {defectId}, DateTime: {newMaxDateTime}, Defect: {defectName}");

                                                // Kirim data ke semua client lewat SignalR
                                                await _hubContext.Clients.All.SendAsync("ReceiveDefect", defectName,newMaxDateTime);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error saat memantau perubahan data: {ex.Message}");
                }

                await Task.Delay(1000); // Cek perubahan setiap 1 detik
            }
        });
    }

    public void StopMonitoring()
    {
        _isRunning = false;
        Console.WriteLine("❌ Monitoring dihentikan.");
    }
}
