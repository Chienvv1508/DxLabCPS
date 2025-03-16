using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Core.DTOs;
using DxLabCoworkingSpace.Service.Sevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/approvalblog")]
    [ApiController]
    public class ApprovalBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IMapper _mapper;

        public ApprovalBlogController(IBlogService blogService, IMapper mapper)
        {
            _blogService = blogService;
            _mapper = mapper;
        }

        [HttpGet("pending")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Pending);
                var blogList = blogs.ToList();

                // Đảm bảo load Images từ DB nếu cần
                foreach (var blog in blogList)
                {
                    if (blog.Images == null || !blog.Images.Any())
                    {
                        blog.Images = (await _blogService.GetById(blog.BlogId)).Images;
                    }
                }

                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogList);
                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>("Không tìm thấy blog nào đang chờ duyệt!", null));
                }

                return Ok(new ResponseDTO<IEnumerable<BlogDTO>>("Danh sách blog đang chờ duyệt đã được lấy thành công!", blogDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpGet("approved")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetApprovedBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Approve);
                var blogList = blogs.ToList();

                // Đảm bảo load Images từ DB nếu cần
                foreach (var blog in blogList)
                {
                    if (blog.Images == null || !blog.Images.Any())
                    {
                        blog.Images = (await _blogService.GetById(blog.BlogId)).Images;
                    }
                }

                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogList);
                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>("Không tìm thấy blog nào đã được duyệt!", null));
                }

                return Ok(new ResponseDTO<IEnumerable<BlogDTO>>("Danh sách blog đã được duyệt đã được lấy thành công!", blogDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpPut("approve/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveBlog(int id)
        {
            try
            {
                await _blogService.ApproveBlog(id);
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>($"Blog với ID: {id} không tìm thấy!", null));
                }

                // Đảm bảo load Images từ DB nếu cần
                if (blog.Images == null || !blog.Images.Any())
                {
                    blog.Images = (await _blogService.GetById(id)).Images;
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);
                return Ok(new ResponseDTO<BlogDTO>("Blog đã được duyệt thành công!", blogDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>($"Lỗi khi duyệt blog: {ex.Message}", null));
            }
        }

        [HttpPut("cancel/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelBlog(int id)
        {
            try
            {
                await _blogService.CancelBlog(id);
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>($"Blog với ID: {id} không tìm thấy!", null));
                }

                // Đảm bảo load Images từ DB nếu cần
                if (blog.Images == null || !blog.Images.Any())
                {
                    blog.Images = (await _blogService.GetById(id)).Images;
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);
                return Ok(new ResponseDTO<BlogDTO>("Blog đã được hủy thành công!", blogDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>($"Lỗi khi hủy blog: {ex.Message}", null));
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var blog = await _blogService.GetByIdWithUser(id);
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>($"Blog với ID: {id} không tìm thấy!", null));
                }

                // Đảm bảo load Images từ DB nếu cần
                if (blog.Images == null || !blog.Images.Any())
                {
                    blog.Images = (await _blogService.GetById(id)).Images;
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);
                return Ok(new ResponseDTO<BlogDTO>("Lấy thông tin blog thành công!", blogDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi lấy blog: {ex.Message}", null));
            }
        }
    }
}