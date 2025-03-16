using AutoMapper;
using DxLabCoworkingSpace.Core.DTOs;
using DxLabCoworkingSpace.Service.Sevices;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var blog = _mapper.Map<Blog>(blogDto);
                //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                //blog.UserId = userId; // Gán UserId từ token
                await _blogService.Add(blog);
                var resultDto = _mapper.Map<BlogDTO>(blog);
                return CreatedAtAction(nameof(GetById), new { id = blog.BlogId }, resultDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Có lỗi xảy ra: " + ex.Message);
            }
        }

        [HttpGet("list/{status}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetBlogsByStatus(string status)
        {
            if (!Enum.TryParse<BlogDTO.BlogStatus>(status, true, out var blogStatus))
                return BadRequest("Trạng thái không hợp lệ. Sử dụng: Pending, Approve, Cancel");

            //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
   
            var blogs = await _blogService.GetAll(b => b.Status == (int)blogStatus);
            var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);
            return Ok(blogDtos);
        }

        [HttpPut("edit-cancelled/{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> EditCancelledBlog(int id, [FromBody] BlogDTO blogDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var userId = 1; // Hardcode test
                var existingBlog = await _blogService.GetById(id);
                if (existingBlog == null) return NotFound("Không tìm thấy blog");
                //if (existingBlog.UserId != userId) return Forbid("Bạn không có quyền chỉnh sửa blog này");

                var updatedBlog = _mapper.Map<Blog>(blogDto);
                await _blogService.EditCancelledBlog(id, updatedBlog);
                var result = await _blogService.GetById(id);
                return Ok(_mapper.Map<BlogDTO>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            //var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var blog = await _blogService.GetById(id);
            if (blog == null) return NotFound("Không tìm thấy blog");
            //if (blog.UserId != userId) return Forbid("Bạn không có quyền xem blog này");
            return Ok(_mapper.Map<BlogDTO>(blog));
        }
    }
}
