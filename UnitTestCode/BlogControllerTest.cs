using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations; 
namespace UnitTestCode
{
    public class BlogControllerTest
    {
        private readonly Mock<IBlogService> _mockBlogService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHubContext<BlogHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly BlogController _controller;

        public BlogControllerTest()
        {
            _mockBlogService = new Mock<IBlogService>();
            _mockMapper = new Mock<IMapper>();
            _mockHubContext = new Mock<IHubContext<BlogHub>>();
            _mockHubClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            // Setup SignalR mock (chỉ cần mock Clients và Group, không mock SendAsync)
            _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
            _mockHubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _controller = new BlogController(_mockBlogService.Object, _mockMapper.Object, _mockHubContext.Object);
        }

        private void SetupUserClaims(int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("UserId", userId.ToString())
            }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void SetupNoUserClaims()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity()); // Không có claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for POST api/blog (Create)

        [Fact]
        public async Task Create_ValidRequest_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg") 
                }

            };
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 1 };
            var savedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 1, User = new User { FullName = "Đức Staff" } };
            var blogDto = new BlogDTO
            {
                BlogId = 1,
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                BlogCreatedDate = DateTime.UtcNow.ToString(),
                Status = BlogDTO.BlogStatus.Pending,
                UserName = "Đức Staff",
                Images = new List<string> { "image1.jpg" }
            };

            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);
            _mockBlogService.Setup(s => s.Add(blog)).Returns(Task.CompletedTask);
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(savedBlog);
            _mockMapper.Setup(m => m.Map<BlogDTO>(savedBlog)).Returns(blogDto);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Blog đã được tạo thành công!", response.Message);

            // Deserialize response.Data thành JObject để truy cập thuộc tính
            var responseData = JObject.FromObject(response.Data);
            Assert.Equal(1, responseData["BlogId"]?.Value<int>());
            Assert.Equal("Khu vực cá nhân", responseData["BlogTitle"]?.Value<string>());
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "helo", 
                BlogContent = "oke nhé",    
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            _controller.ModelState.AddModelError("BlogTitle", "Tiêu đề blog phải từ 5 đến 50 ký tự");
            _controller.ModelState.AddModelError("BlogContent", "Nội dung blog phải ít nhất 10 ký tự");

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Dữ liệu không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task Create_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            SetupNoUserClaims(); // Setup user không có claims
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };

            // Act
            var result = await _controller.Create(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result); // Sửa từ UnauthorizedObjectResult thành ObjectResult
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
            Assert.Equal(401, response.StatusCode);
            Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task Create_InvalidImageFormat_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "image.txt", "image.txt")
                }
            };
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 1 };
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Chỉ chấp nhận file .jpg hoặc .png!", response.Message);
        }

        [Fact]
        public async Task Create_TooLargeImage_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(new byte[6 * 1024 * 1024]), 0, 6 * 1024 * 1024, "image.jpg", "image.jpg")
                }
            };
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 1 };
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("File quá lớn, tối đa 5MB!", response.Message);
        }

        [Fact]
        public async Task Create_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }

            };
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 1 };

            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);
            _mockBlogService.Setup(s => s.Add(blog)).ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal("Lỗi khi xử lý yêu cầu: Lỗi hệ thống", response.Message);
        }

        [Fact]
        public async Task Create_BlogTitleNull_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = null, // BlogTitle là null
                BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            _controller.ModelState.Clear(); // Xóa trạng thái ModelState trước khi validate
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(request, validationContext, validationResults, true);
            foreach (var validationResult in validationResults)
            {
                _controller.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage);
            }

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Dữ liệu không hợp lệ!", response.Message);
            Assert.True(_controller.ModelState.ContainsKey("BlogTitle"));
            Assert.Equal("Tiêu đề blog là bắt buộc", _controller.ModelState["BlogTitle"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Create_BlogContentNull_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực cá nhân",
                BlogContent = null,
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            _controller.ModelState.Clear();
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(request, validationContext, validationResults, true);
            foreach (var validationResult in validationResults)
            {
                _controller.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage);
            }

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Dữ liệu không hợp lệ!", response.Message);
            Assert.True(_controller.ModelState.ContainsKey("BlogContent"));
            Assert.Equal("Nội dung blog là bắt buộc", _controller.ModelState["BlogContent"].Errors[0].ErrorMessage);
        }

        // Tests for PUT api/blog/edit-cancelled/{id}

        [Fact]
        public async Task EditCancelledBlog_ValidRequest_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm",
                BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image2.jpg", "image2.jpg")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực cá nhân", BlogContent = "Đây là khu vực cho những bạn thích yên tĩnh!", Status = 0, User = new User { FullName = "Đức Staff" } };
            var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 1 };
            var savedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 1, User = new User { FullName = "Đức Staff" } };
            var blogDto = new BlogDTO
            {
                BlogId = 1,
                BlogTitle = "Khu vực nhóm",
                BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!",
                BlogCreatedDate = DateTime.UtcNow.ToString(),
                Status = BlogDTO.BlogStatus.Pending,
                UserName = "Đức Staff",
                Images = new List<string> { "image2.jpg" }
            };

            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);
            _mockBlogService.Setup(s => s.EditCancelledBlog(1, updatedBlog)).Returns(Task.CompletedTask);
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(savedBlog);
            _mockMapper.Setup(m => m.Map<BlogDTO>(savedBlog)).Returns(blogDto);

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Blog đã được chỉnh sửa thành công!", response.Message);

            // Deserialize response.Data thành JObject để truy cập thuộc tính
            var responseData = JObject.FromObject(response.Data);
            Assert.Equal(1, responseData["BlogId"]?.Value<int>());
            Assert.Equal("Khu vực nhóm", responseData["BlogTitle"]?.Value<string>());
        }

        [Fact]
        public async Task EditCancelledBlog_BlogNotCancelled_ThrowsException()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm 4",   
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 1, User = new User { FullName = "Đức Staff" } };
            var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1 };

            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);
            _mockBlogService.Setup(s => s.EditCancelledBlog(1, updatedBlog)).ThrowsAsync(new Exception("Blog không ở trạng thái cancelled!"));

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Lỗi khi chỉnh sửa blog: Blog không ở trạng thái cancelled!", response.Message);
        }

        [Fact]
        public async Task EditCancelledBlog_InvalidImageFormat_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm 4",
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                ImageFiles = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "image2.txt", "image2.txt")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 0, User = new User { FullName = "Đức Staff" } };
            var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1 };

            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Chỉ chấp nhận file .jpg hoặc .png!", response.Message);
        }

        [Fact]
        public async Task EditCancelledBlog_TooLargeImage_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm 4",
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                ImageFiles = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(new byte[6 * 1024 * 1024]), 0, 6 * 1024 * 1024, "image2.jpg", "image2.jpg")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 0, User = new User { FullName = "Đức Staff" } };
            var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1 };

            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("File quá lớn, tối đa 5MB!", response.Message);
        }

        [Fact]
        public async Task EditCancelledBlog_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            SetupNoUserClaims(); // Setup user không có claims
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm 4",
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 0, User = new User { FullName = "Đức Staff" } };
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog); // Setup để vượt qua kiểm tra blog tồn tại

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
            Assert.Equal(401, response.StatusCode);
            Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task EditCancelledBlog_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            SetupUserClaims(1);
            var request = new BlogRequestDTO
            {
                BlogTitle = "Khu vực nhóm 4",
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                ImageFiles = new List<IFormFile>
                {
                new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "image1.jpg", "image1.jpg")
                }
            };
            var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm", BlogContent = "Khu vực tuyệt vời cho những nhóm đồ án!", Status = 0, User = new User { FullName = "Đức Staff" } };
            var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1 };

            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
            _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);
            _mockBlogService.Setup(s => s.EditCancelledBlog(1, updatedBlog)).ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.EditCancelledBlog(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Lỗi khi chỉnh sửa blog: Lỗi hệ thống", response.Message);
        }

        // Tests for GET api/blog/{id}

        [Fact]
        public async Task GetById_ValidId_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(1);
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1, User = new User { FullName = "Đức Staff" } };
            var blogDto = new BlogDTO
            {
                BlogId = 1,
                BlogTitle = "Khu vực nhóm 4",
                BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!",
                BlogCreatedDate = DateTime.UtcNow.ToString(),
                Status = BlogDTO.BlogStatus.Pending,
                UserName = "Đức Staff",
                Images = new List<string>()
            };
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(blog);
            _mockMapper.Setup(m => m.Map<BlogDTO>(blog)).Returns(blogDto);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy thông tin blog thành công!", response.Message);

            // Deserialize response.Data thành JObject để truy cập thuộc tính
            var responseData = JObject.FromObject(response.Data);
            Assert.Equal(1, responseData["BlogId"]?.Value<int>());
            Assert.Equal("Khu vực nhóm 4", responseData["BlogTitle"]?.Value<string>());
        }

        [Fact]
        public async Task GetById_BlogNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserClaims(1);
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync((Blog)null);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Blog với ID: 1 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task GetById_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            SetupNoUserClaims(); // Setup user không có claims
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Khu vực nhóm 4", BlogContent = "Khu vực tuyệt vời cho những nhóm 4 người!", Status = 1 };
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(blog);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result); // Sửa từ UnauthorizedObjectResult thành ObjectResult
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
            Assert.Equal(401, response.StatusCode);
            Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task GetById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserClaims(1);
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal("Lỗi khi lấy blog: Lỗi hệ thống", response.Message);
        }
    }
}