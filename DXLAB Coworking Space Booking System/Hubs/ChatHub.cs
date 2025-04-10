using Microsoft.AspNetCore.SignalR;

namespace DXLAB_Coworking_Space_Booking_System.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage",user, message);
        }
    }
}
