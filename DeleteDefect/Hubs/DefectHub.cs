using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DeleteDefect.Hubs
{
    public class DefectHub : Hub
    {
        // Notifikasi ketika ada Defect baru
        public async Task SendDefect(string defectMessage)
        {
            await Clients.All.SendAsync("ReceiveDefect", defectMessage);
        }
    }
}
