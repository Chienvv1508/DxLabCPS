using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Moq;
using Castle.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

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

            // Setup SignalR mock
            _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
            _mockHubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClientProxy.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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

        //// Tests for POST api/blog (Create)

        //[Fact]
        //public async Task Create_ValidRequest_ReturnsOk()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Test Blog",
        //        BlogContent = "This is a test blog content with enough length."
        //    };
        //    var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Test Blog", BlogContent = "This is a test blog content with enough length.", Status = 1 };
        //    var savedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Test Blog", BlogContent = "This is a test blog content with enough length.", Status = 1, User = new User { UserName = "testuser" } };
        //    var blogDto = new BlogDTO
        //    {
        //        BlogId = 1,
        //        BlogTitle = "Test Blog",
        //        BlogContent = "This is a test blog content with enough length.",
        //        BlogCreatedDate = DateTime.UtcNow.ToString(),
        //        Status = BlogDTO.BlogStatus.Pending,
        //        UserName = "testuser",
        //        Images = new List<string>()
        //    };

        //    _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);
        //    _mockBlogService.Setup(s => s.Add(blog)).Returns(Task.CompletedTask);
        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(savedBlog);
        //    _mockMapper.Setup(m => m.Map<BlogDTO>(savedBlog)).Returns(blogDto);

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
        //    Assert.Equal(200, response.StatusCode);
        //    Assert.Equal("Blog đã được tạo thành công!", response.Message);
        //    var responseData = Assert.IsType<dynamic>(response.Data);
        //    Assert.Equal(1, responseData.BlogId);
        //    Assert.Equal("Test Blog", responseData.BlogTitle);
        //}

        //[Fact]
        //public async Task Create_InvalidModel_ReturnsBadRequest()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Test", // Too short
        //        BlogContent = "Short" // Too short
        //    };
        //    _controller.ModelState.AddModelError("BlogTitle", "Tiêu đề blog phải từ 5 đến 50 ký tự");
        //    _controller.ModelState.AddModelError("BlogContent", "Nội dung blog phải ít nhất 10 ký tự");

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
        //    Assert.Equal(400, response.StatusCode);
        //    Assert.Equal("Dữ liệu không hợp lệ!", response.Message);
        //}

        //[Fact]
        //public async Task Create_Unauthorized_ReturnsUnauthorized()
        //{
        //    // Arrange
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Test Blog",
        //        BlogContent = "This is a test blog content with enough length."
        //    };
        //    // No user claims

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
        //    Assert.Equal(401, response.StatusCode);
        //    Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
        //}

        //[Fact]
        //public async Task Create_InvalidImageFormat_ReturnsBadRequest()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Test Blog",
        //        BlogContent = "This is a test blog content with enough length.",
        //        ImageFiles = new List<IFormFile>
        //        {
        //            new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "test.txt", "test.txt")
        //        }
        //    };
        //    var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Test Blog", BlogContent = "This is a test blog content with enough length.", Status = 1 };
        //    _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
        //    Assert.Equal(400, response.StatusCode);
        //    Assert.Equal("Chỉ chấp nhận file .jpg hoặc .png!", response.Message);
        //}

        //[Fact]
        //public async Task Create_TooLargeImage_ReturnsBadRequest()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Test Blog",
        //        BlogContent = "This is a test blog content with enough length.",
        //        ImageFiles = new List<IFormFile>
        //        {
        //            new FormFile(new MemoryStream(new byte[6 * 1024 * 1024]), 0, 6 * 1024 * 1024, "test.jpg", "test.jpg")
        //        }
        //    };
        //    var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Test Blog", BlogContent = "This is a test blog content with enough length.", Status = 1 };
        //    _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(blog);

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
        //    Assert.Equal(400, response.StatusCode);
        //    Assert.Equal("File quá lớn, tối đa 5MB!", response.Message);
        //}

        //// Tests for GET api/blog/list/{status}

        //[Fact]
        //public async Task GetBlogsByStatus_ValidStatus_ReturnsOk()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var blogs = new List<Blog>
        //    {
        //        new Blog { BlogId = 1, UserId = 1, BlogTitle = "Blog 1", BlogContent = "Content 1", Status = 1, User = new User { UserName = "testuser" } }
        //    };
        //    var blogDtos = new List<BlogDTO>
        //    {
        //        new BlogDTO { BlogId = 1, BlogTitle = "Blog 1", BlogContent = "Content 1", BlogCreatedDate = DateTime.UtcNow.ToString(), Status = BlogDTO.BlogStatus.Pending, UserName = "testuser", Images = new List<string>() }
        //    };
        //    _mockBlogService.Setup(s => s.GetAllWithUser(It.IsAny<Func<Blog, bool>>())).ReturnsAsync(blogs);
        //    _mockMapper.Setup(m => m.Map<IEnumerable<BlogDTO>>(blogs)).Returns(blogDtos);

        //    // Act
        //    var result = await _controller.GetBlogsByStatus("Pending");

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<IEnumerable<object>>>(okResult.Value);
        //    Assert.Equal(200, response.StatusCode);
        //    Assert.Equal("Danh sách blog đã được lấy thành công!", response.Message);
        //    Assert.Single(response.Data);
        //}

        //[Fact]
        //public async Task GetBlogsByStatus_InvalidStatus_ReturnsBadRequest()
        //{
        //    // Arrange
        //    SetupUserClaims(1);

        //    // Act
        //    var result = await _controller.GetBlogsByStatus("Invalid");

        //    // Assert
        //    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
        //    Assert.Equal(400, response.StatusCode);
        //    Assert.Equal("Trạng thái không hợp lệ. Sử dụng: Pending, Approve, Cancel", response.Message);
        //}

        //[Fact]
        //public async Task GetBlogsByStatus_NoBlogs_ReturnsNotFound()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    _mockBlogService.Setup(s => s.GetAllWithUser(It.IsAny<Func<Blog, bool>>())).ReturnsAsync(new List<Blog>());
        //    _mockMapper.Setup(m => m.Map<IEnumerable<BlogDTO>>(It.IsAny<IEnumerable<Blog>>())).Returns(new List<BlogDTO>());

        //    // Act
        //    var result = await _controller.GetBlogsByStatus("Pending");

        //    // Assert
        //    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
        //    Assert.Equal(404, response.StatusCode);
        //    Assert.Equal("Không tìm thấy blog nào với trạng thái này!", response.Message);
        //}

        //// Tests for PUT api/blog/edit-cancelled/{id}

        //[Fact]
        //public async Task EditCancelledBlog_ValidRequest_ReturnsOk()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Updated Blog",
        //        BlogContent = "This is an updated blog content with enough length."
        //    };
        //    var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Old Blog", BlogContent = "Old content", Status = 0, User = new User { UserName = "testuser" } };
        //    var updatedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Updated Blog", BlogContent = "This is an updated blog content with enough length.", Status = 1 };
        //    var savedBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Updated Blog", BlogContent = "This is an updated blog content with enough length.", Status = 1, User = new User { UserName = "testuser" } };
        //    var blogDto = new BlogDTO
        //    {
        //        BlogId = 1,
        //        BlogTitle = "Updated Blog",
        //        BlogContent = "This is an updated blog content with enough length.",
        //        BlogCreatedDate = DateTime.UtcNow.ToString(),
        //        Status = BlogDTO.BlogStatus.Pending,
        //        UserName = "testuser",
        //        Images = new List<string>()
        //    };

        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);
        //    _mockMapper.Setup(m => m.Map<Blog>(request)).Returns(updatedBlog);
        //    _mockBlogService.Setup(s => s.EditCancelledBlog(1, updatedBlog)).Returns(Task.CompletedTask);
        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(savedBlog);
        //    _mockMapper.Setup(m => m.Map<BlogDTO>(savedBlog)).Returns(blogDto);

        //    // Act
        //    var result = await _controller.EditCancelledBlog(1, request);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
        //    Assert.Equal(200, response.StatusCode);
        //    Assert.Equal("Blog đã được chỉnh sửa thành công!", response.Message);
        //    var responseData = Assert.IsType<dynamic>(response.Data);
        //    Assert.Equal(1, responseData.BlogId);
        //    Assert.Equal("Updated Blog", responseData.BlogTitle);
        //}

        //[Fact]
        //public async Task EditCancelledBlog_BlogNotFound_ReturnsNotFound()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Updated Blog",
        //        BlogContent = "This is an updated blog content with enough length."
        //    };
        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync((Blog)null);

        //    // Act
        //    var result = await _controller.EditCancelledBlog(1, request);

        //    // Assert
        //    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
        //    Assert.Equal(404, response.StatusCode);
        //    Assert.Equal("Blog với ID: 1 không tìm thấy!", response.Message);
        //}

        //[Fact]
        //public async Task EditCancelledBlog_UnauthorizedUser_ReturnsForbid()
        //{
        //    // Arrange
        //    SetupUserClaims(2); // Different user
        //    var request = new BlogRequestDTO
        //    {
        //        BlogTitle = "Updated Blog",
        //        BlogContent = "This is an updated blog content with enough length."
        //    };
        //    var existingBlog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Old Blog", BlogContent = "Old content", Status = 0 };
        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(existingBlog);

        //    // Act
        //    var result = await _controller.EditCancelledBlog(1, request);

        //    // Assert
        //    Assert.IsType<ForbidResult>(result);
        //}

        // Tests for GET api/blog/{id}

        //[Fact]
        //public async Task GetById_ValidId_ReturnsOk()
        //{
        //    // Arrange
        //    SetupUserClaims(1);
        //    var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Blog 1", BlogContent = "Content 1", Status = 1, User = new User { UserName = "testuser" } };
        //    var blogDto = new BlogDTO
        //    {
        //        BlogId = 1,
        //        BlogTitle = "Blog 1",
        //        BlogContent = "Content 1",
        //        BlogCreatedDate = DateTime.UtcNow.ToString(),
        //        Status = BlogDTO.BlogStatus.Pending,
        //        UserName = "testuser",
        //        Images = new List<string>()
        //    };
        //    _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(blog);
        //    _mockMapper.Setup(m => m.Map<BlogDTO>(blog)).Returns(blogDto);

        //    // Act
        //    var result = await _controller.GetById(1);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
        //    Assert.Equal(200, response.StatusCode);
        //    Assert.Equal("Lấy thông tin blog thành công!", response.Message);
        //    var responseData = Assert.IsType<dynamic>(response.Data);
        //    Assert.Equal(1, responseData.BlogId);
        //    Assert.Equal("Blog 1", responseData.BlogTitle);
        //}

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
        public async Task GetById_UnauthorizedUser_ReturnsForbid()
        {
            // Arrange
            SetupUserClaims(2); // Different user
            var blog = new Blog { BlogId = 1, UserId = 1, BlogTitle = "Blog 1", BlogContent = "Content 1", Status = 1 };
            _mockBlogService.Setup(s => s.GetByIdWithUser(1)).ReturnsAsync(blog);

            // Act
            var result = await _controller.GetById(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}
