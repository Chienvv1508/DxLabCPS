using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DXLAB_Coworking_Space_Booking_System.Hubs
{
    [Authorize]
    public class BlogHub : Hub
    {
        // Thông báo blog mới tới Admin
        public async Task NotifyNewBlog(object blog)
        {
            await Clients.Group("Admins").SendAsync("ReceiveNewBlog", blog);
        }

        // Thông báo blog được chỉnh sửa tới Admin
        public async Task NotifyEditedBlog(object blog)
        {
            await Clients.Group("Admins").SendAsync("ReceiveEditedBlog", blog);
        }

        // Thông báo trạng thái blog tới Staff, Admin và Student
        public async Task NotifyBlogStatus(string userId, object blog)
        {
            await Clients.Group("Staff").SendAsync("ReceiveBlogStatus", blog); // Blog owner (Staff)
            await Clients.Group("Admins").SendAsync("ReceiveBlogStatus", blog); // Admin
            // Gửi tới Student nếu blog được duyệt
            if (blog.GetType().GetProperty("Status")?.GetValue(blog)?.ToString() == BlogDTO.BlogStatus.Approve.ToString())
            {
                await Clients.Group("Students").SendAsync("ReceiveBlogStatus", blog);
            }
        }

        // Thông báo blog bị xóa tới Staff và Admin
        public async Task NotifyBlogDeleted(string userId, int blogId)
        {
            await Clients.Group("Staff").SendAsync("ReceiveBlogDeleted", blogId); // Blog owner (Staff)
            await Clients.Group("Admins").SendAsync("ReceiveBlogDeleted", blogId); // Admin
            await Clients.Group("Students").SendAsync("ReceiveBlogDeleted", blogId); // Student
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
