using AutoMapper;
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
            var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Pending);
            var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);
            return Ok(blogDtos);
        }

        [HttpGet("approved")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetApprovedBlogs()
        {
            var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)BlogDTO.BlogStatus.Approve);
            var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);
            return Ok(blogDtos);
        }

        [HttpPut("approve/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveBlog(int id)
        {
            try
            {
                await _blogService.ApproveBlog(id);
                var blog = await _blogService.GetByIdWithUser(id);
                return Ok(_mapper.Map<BlogDTO>(blog));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
                return Ok(_mapper.Map<BlogDTO>(blog));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var blog = await _blogService.GetByIdWithUser(id);
            if (blog == null) return NotFound("Không tìm thấy blog");
            return Ok(_mapper.Map<BlogDTO>(blog));
        }
    }
}
