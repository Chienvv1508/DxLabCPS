using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/approvalblog")]
    [ApiController]
    public class ApprovalBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IMapper _mapper;
        private readonly IHubContext<BlogHub> _hubContext;

        public ApprovalBlogController(IBlogService blogService, IMapper mapper, IHubContext<BlogHub> hubContext)
        {
            _blogService = blogService;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Pending);
                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);

                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy blog nào đang chờ duyệt!", null));
                }

                var responseDtos = blogDtos.Select(dto => new
                {
                    BlogId = dto.BlogId,
                    BlogTitle = dto.BlogTitle,
                    BlogContent = dto.BlogContent,
                    BlogCreatedDate = dto.BlogCreatedDate,
                    Status = dto.Status,
                    UserName = dto.UserName,
                    Images = dto.Images
                });

                return Ok(new ResponseDTO<IEnumerable<object>>(200, "Danh sách blog đang chờ duyệt đã được lấy thành công!", responseDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Approve);
                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);

                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy blog nào đã được duyệt!", null));
                }

                var responseDtos = blogDtos.Select(dto => new
                {
                    BlogId = dto.BlogId,
                    BlogTitle = dto.BlogTitle,
                    BlogContent = dto.BlogContent,
                    BlogCreatedDate = dto.BlogCreatedDate,
                    Status = dto.Status,
                    UserName = dto.UserName,
                    Images = dto.Images
                });

                return Ok(new ResponseDTO<IEnumerable<object>>(200, "Danh sách blog đã được duyệt đã được lấy thành công!", responseDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpPut("approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveBlog(int id)
        {
            try
            {
                await _blogService.ApproveBlog(id);
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Blog với ID: {id} không tìm thấy!", null));
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);

                var blogData = new
                {
                    BlogId = blogDto.BlogId,
                    BlogTitle = blogDto.BlogTitle,
                    BlogContent = blogDto.BlogContent,
                    BlogCreatedDate = blogDto.BlogCreatedDate,
                    Status = blogDto.Status,
                    UserName = blogDto.UserName,
                    Images = blogDto.Images
                };
                await _hubContext.Clients.Group("Staff").SendAsync("ReceiveBlogStatus", blogData);
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveBlogStatus", blogData);

                var responseDto = new
                {
                    BlogId = blogDto.BlogId,
                    BlogTitle = blogDto.BlogTitle,
                    BlogContent = blogDto.BlogContent,
                    BlogCreatedDate = blogDto.BlogCreatedDate,
                    Status = blogDto.Status,
                    UserName = blogDto.UserName,
                    Images = blogDto.Images
                };

                return Ok(new ResponseDTO<object>(200, "Blog đã được duyệt thành công!", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi khi duyệt blog: {ex.Message}", null));
            }
        }

        [HttpPut("cancel/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelBlog(int id)
        {
            try
            {
                await _blogService.CancelBlog(id);
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Blog với ID: {id} không tìm thấy!", null));
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);

                var blogData = new
                {
                    BlogId = blogDto.BlogId,
                    BlogTitle = blogDto.BlogTitle,
                    BlogContent = blogDto.BlogContent,
                    BlogCreatedDate = blogDto.BlogCreatedDate,
                    Status = blogDto.Status,
                    UserName = blogDto.UserName,
                    Images = blogDto.Images
                };
                await _hubContext.Clients.Group("Staff").SendAsync("ReceiveBlogStatus", blogData);
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveBlogStatus", blogData);

                var responseDto = new
                {
                    BlogId = blogDto.BlogId,
                    BlogTitle = blogDto.BlogTitle,
                    BlogContent = blogDto.BlogContent,
                    BlogCreatedDate = blogDto.BlogCreatedDate,
                    Status = blogDto.Status,
                    UserName = blogDto.UserName,
                    Images = blogDto.Images
                };

                return Ok(new ResponseDTO<object>(200, "Blog đã được hủy thành công!", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi khi hủy blog: {ex.Message}", null));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Blog với ID: {id} không tìm thấy!", null));
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);

                var responseDto = new
                {
                    BlogId = blogDto.BlogId,
                    BlogTitle = blogDto.BlogTitle,
                    BlogContent = blogDto.BlogContent,
                    BlogCreatedDate = blogDto.BlogCreatedDate,
                    Status = blogDto.Status,
                    UserName = blogDto.UserName,
                    Images = blogDto.Images
                };

                return Ok(new ResponseDTO<object>(200, "Lấy thông tin blog thành công!", responseDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy blog: {ex.Message}", null));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteApprovedBlog(int id)
        {
            try
            {
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Blog với ID: {id} không tìm thấy!", null));
                }

                // Gửi thông báo real-time tới nhóm Staff và Admins
                await _hubContext.Clients.Group("Staff").SendAsync("ReceiveBlogDeleted", id);
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveBlogDeleted", id);

                await _blogService.Delete(id);
                return Ok(new ResponseDTO<object>(200, $"Blog với ID: {id} đã được xóa thành công!", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi khi xóa blog: {ex.Message}", null));
            }
        }
    }
}