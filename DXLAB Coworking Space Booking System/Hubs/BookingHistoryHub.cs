using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

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
                await Clients.Group("Student").SendAsync("ReceiveBookingStatus", bookingDetail); // Gửi tới Student (chủ sở hữu booking)
                await Clients.Group("Staff").SendAsync("ReceiveBookingStatus", bookingDetail); // Gửi tới Staff
            }

            public override async Task OnConnectedAsync()
            {
                var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = Context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
                Console.WriteLine($"User connected: ID={userId}, Role={roleClaim}, ConnectionId={Context.ConnectionId}");

                if (roleClaim != null)
                {
                    if (roleClaim == "Staff")
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
                        Console.WriteLine($"Added user {userId} to Staff group");
                    }
                    if (roleClaim == "Student")
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Student");
                        Console.WriteLine($"Added user {userId} to Staff group");
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
}
