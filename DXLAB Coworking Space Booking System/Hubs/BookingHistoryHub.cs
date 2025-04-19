using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DXLAB_Coworking_Space_Booking_System.Hubs
{
    public class BookingHistoryHub : Hub
    {
        [Authorize]
        public class BookingHub : Hub
        {
            // Thông báo trạng thái booking detail thay đổi
            public async Task NotifyBookingStatus(string userId, object bookingDetail)
            {
                await Clients.User(userId).SendAsync("ReceiveBookingStatus", bookingDetail); // Gửi tới Student (chủ sở hữu booking)
                await Clients.Group("Staff").SendAsync("ReceiveBookingStatus", bookingDetail); // Gửi tới Staff
            }

            public override async Task OnConnectedAsync()
            {
                var roleClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                if (roleClaim != null)
                {
                    switch (roleClaim.Value)
                    {
                        case "Student":
                            await Groups.AddToGroupAsync(Context.ConnectionId, "Students");
                            break;
                        case "Staff":
                            await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
                            break;
                    }
                    Console.WriteLine($"User {Context.User.Identity?.Name} connected with role {roleClaim.Value}");
                }
                else
                {
                    Console.WriteLine($"User {Context.User.Identity?.Name} connected without role.");
                }
                await base.OnConnectedAsync();
            }
        }
    }
}
