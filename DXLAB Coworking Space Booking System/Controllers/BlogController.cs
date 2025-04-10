using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.SignalR;
using DXLAB_Coworking_Space_Booking_System.Hubs;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/blog")]
    [ApiController]
     
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IMapper _mapper;
        private readonly IHubContext<BlogHub> _hubContext;
        public BlogController(IBlogService blogService, IMapper mapper, IHubContext<BlogHub> hubContext)
        {
            _blogService = blogService;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create([FromForm] BlogRequestDTO blogRequestDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                var blog = _mapper.Map<Blog>(blogRequestDTO);
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }
                blog.UserId = userId;

                // Đảm bảo thư mục images tồn tại
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                }

                // Xử lý upload ảnh
                if (blogRequestDTO.ImageFiles != null && blogRequestDTO.ImageFiles.Any())
                {
                    blog.Images = new List<Image>();
                    foreach (var file in blogRequestDTO.ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Kiểm tra loại file (chỉ cho phép .jpg, .png)
                            if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "Chỉ chấp nhận file .jpg hoặc .png!", null));
                            }

                            // Kiểm tra kích thước file (giới hạn 5MB)
                            if (file.Length > 5 * 1024 * 1024)
                            {
                                return BadRequest(new ResponseDTO<object>(400, "File quá lớn, tối đa 5MB!", null));
                            }

                            // Lấy tên file gốc để hiển thị
                            var originalFileName = Path.GetFileName(file.FileName); // Tên gốc: "myphoto.jpg"

                            // Tạo tên file duy nhất để lưu trên server
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(imagesDir, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            blog.Images.Add(new Image { ImageUrl = $"/Images/{uniqueFileName}" });
                        }
                    }
                }

                await _blogService.Add(blog);
                var savedBlog = await _blogService.GetByIdWithUser(blog.BlogId);
                var resultDto = _mapper.Map<BlogDTO>(savedBlog);

                // Gửi thông báo real-time tới Admin
                var blogData = new
                {
                    BlogId = resultDto.BlogId,
                    BlogTitle = resultDto.BlogTitle,
                    BlogContent = resultDto.BlogContent,
                    BlogCreatedDate = resultDto.BlogCreatedDate,
                    Status = resultDto.Status,
                    UserName = resultDto.UserName,
                    Images = resultDto.Images
                };
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNewBlog", blogData);

                // Chỉ trả về các trường cần thiết trong response
                var responseDto = new
                {
                    BlogId = resultDto.BlogId,
                    BlogTitle = resultDto.BlogTitle,
                    BlogContent = resultDto.BlogContent,
                    BlogCreatedDate = resultDto.BlogCreatedDate,
                    Status = resultDto.Status,  
                    UserName = resultDto.UserName,
                    Images = resultDto.Images
                };

                return Ok(new ResponseDTO<object>(200, "Blog đã được tạo thành công!", responseDto));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi khi tạo blog: {ex.Message}", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xử lý yêu cầu: {ex.Message}", null));
            }
        }

        [HttpGet("list/{status}")]
        public async Task<IActionResult> GetBlogsByStatus(string status)
        {
            if (!Enum.TryParse<BlogDTO.BlogStatus>(status, true, out var blogStatus))
            {
                return BadRequest(new ResponseDTO<object>(400, "Trạng thái không hợp lệ. Sử dụng: Pending, Approve, Cancel", null));
            }

            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }

                var blogs = await _blogService.GetAllWithUser(b => b.Status == (int)blogStatus && b.UserId == userId);
                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(blogs);

                if (blogDtos == null || !blogDtos.Any())
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy blog nào với trạng thái này!", null));
                }



                // Chỉ trả về các trường cần thiết trong response
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

                return Ok(new ResponseDTO<IEnumerable<object>>(200, "Danh sách blog đã được lấy thành công!", responseDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy danh sách blog: {ex.Message}", null));
            }
        }

        [HttpPut("edit-cancelled/{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> EditCancelledBlog(int id, [FromForm] BlogRequestDTO blogRequestDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ!", ModelState));
            }

            try
            {
                var existingBlog = await _blogService.GetByIdWithUser(id);
                if (existingBlog == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Blog với ID: {id} không tìm thấy!", null));
                }

                // Lấy UserId từ token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }
                var userId = int.Parse(userIdClaim.Value);

                // Kiểm tra nếu blog không thuộc về user hiện tại
                if (existingBlog.UserId != userId)
                {
                    return Forbid();
                }

                var updatedBlog = _mapper.Map<Blog>(blogRequestDTO);
                updatedBlog.BlogId = id;
                updatedBlog.UserId = existingBlog.UserId;

                // Đảm bảo thư mục images tồn tại
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                }

                // Xử lý upload ảnh mới (nếu có)
                if (blogRequestDTO.ImageFiles != null && blogRequestDTO.ImageFiles.Any())
                {
                    updatedBlog.Images = new List<Image>();
                    foreach (var file in blogRequestDTO.ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Kiểm tra loại file (chỉ cho phép .jpg, .png)
                            if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
                            {
                                return BadRequest(new ResponseDTO<object>(400, "Chỉ chấp nhận file .jpg hoặc .png!", null));
                            }

                            // Kiểm tra kích thước file (giới hạn 5MB)
                            if (file.Length > 5 * 1024 * 1024)
                            {
                                return BadRequest(new ResponseDTO<object>(400, "File quá lớn, tối đa 5MB!", null));
                            }

                            // Lấy tên file gốc để hiển thị
                            var originalFileName = Path.GetFileName(file.FileName); // Tên gốc: "myphoto.jpg"

                            // Tạo tên file duy nhất để lưu trên server
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(imagesDir, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            updatedBlog.Images.Add(new Image { ImageUrl = $"/Images/{uniqueFileName}" });
                        }
                    }
                }
                else
                {
                    updatedBlog.Images = existingBlog.Images; // Giữ nguyên ảnh cũ nếu không upload ảnh mới
                }

                await _blogService.EditCancelledBlog(id, updatedBlog);
                var savedBlog = await _blogService.GetByIdWithUser(id);
                var resultDto = _mapper.Map<BlogDTO>(savedBlog);

                // Gửi thông báo real-time tới Admin
                var blogData = new
                {
                    BlogId = resultDto.BlogId,
                    BlogTitle = resultDto.BlogTitle,
                    BlogContent = resultDto.BlogContent,
                    BlogCreatedDate = resultDto.BlogCreatedDate,
                    Status = resultDto.Status,
                    UserName = resultDto.UserName,
                    Images = resultDto.Images
                };
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveEditedBlog", blogData);

                // Chỉ trả về các trường cần thiết trong response
                var responseDto = new
                {
                    BlogId = resultDto.BlogId,
                    BlogTitle = resultDto.BlogTitle,
                    BlogContent = resultDto.BlogContent,
                    BlogCreatedDate = resultDto.BlogCreatedDate,
                    Status = resultDto.Status,
                    UserName = resultDto.UserName,
                    Images = resultDto.Images
                };

                return Ok(new ResponseDTO<object>(200, "Blog đã được chỉnh sửa thành công!", responseDto));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDTO<object>(400, $"Lỗi khi chỉnh sửa blog: {ex.Message}", null));
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

                // Lấy UserId từ token
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }
                var userId = int.Parse(userIdClaim.Value);

                // Kiểm tra nếu blog không thuộc về user hiện tại
                if (blog.UserId != userId)
                {
                    return Forbid();
                }

                var blogDto = _mapper.Map<BlogDTO>(blog);

                // Chỉ trả về các trường cần thiết trong response
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
    }
}