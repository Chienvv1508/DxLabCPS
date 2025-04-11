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

        // Thông báo trạng thái blog tới Staff và Admin
        public async Task NotifyBlogStatus(string staffUserId, object blog)
        {
            await Clients.User(staffUserId).SendAsync("ReceiveBlogStatus", blog);
            await Clients.Group("Admins").SendAsync("ReceiveBlogStatus", blog); // Admin cũng nhận để cập nhật
        }

        // Thông báo blog bị xóa tới Staff và Admin
        public async Task NotifyBlogDeleted(string staffUserId, int blogId)
        {
            await Clients.User(staffUserId).SendAsync("ReceiveBlogDeleted", blogId);
            await Clients.Group("Admins").SendAsync("ReceiveBlogDeleted", blogId); // Admin cũng nhận để cập nhật
        }

        public override async Task OnConnectedAsync()
        {
            var roleClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            if (roleClaim != null && roleClaim.Value == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnConnectedAsync();
        }
    }
}
