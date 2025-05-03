using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/notification")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public NotificationController(INotificationService notificationService, IMapper mapper, IUserService userService)
        {
            _notificationService = notificationService;
            _mapper = mapper;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewNotification(NotificationDTOForAdd notification)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
            }
            var user = await _userService.Get(u => u.UserId == userId);
            Notification noti = _mapper.Map<Notification>(notification);
            ResponseDTO<object> result = await _notificationService.Add(noti,userId);
            return StatusCode(result.StatusCode, result.Message);
        }
    }
}
