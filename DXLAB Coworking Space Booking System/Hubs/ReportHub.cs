using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DXLAB_Coworking_Space_Booking_System.Hubs
{
    [Authorize]
    public class ReportHub : Hub
    {
        // Thông báo báo cáo mới tới Admin
        public async Task NotifyNewReport(object report)
        {
            await Clients.Group("Admins").SendAsync("ReceiveNewReport", report);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = Context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            Console.WriteLine($"User connected: ID={userId}, Role={roleClaim}, ConnectionId={Context.ConnectionId}");

            if (roleClaim != null)
            {
                if (roleClaim == "Admin")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                    Console.WriteLine($"Added user {userId} to Admins group");
                }
            }
            else
            {
                Console.WriteLine($"No role claim found for user {userId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
