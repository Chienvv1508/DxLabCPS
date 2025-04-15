using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
            await Clients.User(userId).SendAsync("ReceiveBlogStatus", blog); // Blog owner (Staff)
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
            await Clients.User(userId).SendAsync("ReceiveBlogDeleted", blogId); // Blog owner (Staff)
            await Clients.Group("Admins").SendAsync("ReceiveBlogDeleted", blogId); // Admin
            await Clients.Group("Students").SendAsync("ReceiveBlogDeleted", blogId); // Student
        }

        public override async Task OnConnectedAsync()
        {
            var roleClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            if (roleClaim != null)
            {
                switch (roleClaim.Value)
                {
                    case "Admin":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                        break;
                    case "Student":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Students");
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
