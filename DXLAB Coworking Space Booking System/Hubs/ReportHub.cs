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
            var roleClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            var userId = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (roleClaim != null && roleClaim.Value == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }
    }
}
