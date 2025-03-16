using AutoMapper;
using DxLabCoworkingSpace.Core.DTOs;
using DxLabCoworkingSpace.Service.Sevices;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/blog")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IMapper _mapper;

        public BlogController(IBlogService blogService, IMapper mapper)
        {
            _blogService = blogService;
            _mapper = mapper;
        }

        [HttpPost]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create([FromBody] BlogDTO blogDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>("Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                var blog = _mapper.Map<Blog>(blogDto);
                var userId = 2; // Hardcode UserId = 1 để test
                blog.UserId = userId;

                await _blogService.Add(blog); // BlogCreatedDate sẽ được gán trong BlogService
                var savedBlog = await _blogService.GetByIdWithUser(blog.BlogId); // Lấy lại với Images và User
                var resultDto = _mapper.Map<BlogDTO>(savedBlog);
                
                return Ok(new ResponseDTO<BlogDTO>("Blog đã được tạo thành công!", resultDto));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>($"Lỗi khi tạo blog: {ex.Message}", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi xử lý yêu cầu: {ex.Message}", null));
            }
        }

        [HttpGet("list/{status}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetBlogsByStatus(string status)
        {
            if (!Enum.TryParse<BlogDTO.BlogStatus>(status, true, out var blogStatus))
            {
                return BadRequest(new ResponseDTO<object>("Trạng thái không hợp lệ. Sử dụng: Pending, Approve, Cancel", null));
            }

            try
            {
                var userId = 2; // Hardcode UserId = 1 để test
                var blogs = await _blogService.GetAll(b => b.Status == (int)blogStatus && b.UserId == userId);
                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);

                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>("Không tìm thấy blog nào với trạng thái này!", null));
                }

                return Ok(new ResponseDTO<IEnumerable<BlogDTO>>("Danh sách blog đã được lấy thành công!", blogDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpPut("edit-cancelled/{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> EditCancelledBlog(int id, [FromBody] BlogDTO blogDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>("Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                var existingBlog = await _blogService.GetById(id);
                if (existingBlog == null)
                {
                    return NotFound(new ResponseDTO<object>($"Blog với ID: {id} không tìm thấy!", null));
                }

                var updatedBlog = _mapper.Map<Blog>(blogDto);
                updatedBlog.BlogId = id; // Đảm bảo BlogId không bị thay đổi
                await _blogService.EditCancelledBlog(id, updatedBlog);

                var savedBlog = await _blogService.GetByIdWithUser(id); // Lấy lại với Images và User
                var resultDto = _mapper.Map<BlogDTO>(savedBlog);
                return Ok(new ResponseDTO<BlogDTO>("Blog đã được chỉnh sửa thành công!", resultDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>($"Lỗi khi chỉnh sửa blog: {ex.Message}", null));
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var blog = await _blogService.GetByIdWithUser(id); // Dùng GetByIdWithUser để load Images và User
                if (blog == null)
                {
                    return NotFound(new ResponseDTO<object>($"Blog với ID: {id} không tìm thấy!", null));
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