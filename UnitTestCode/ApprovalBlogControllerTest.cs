using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DXLAB_Coworking_Space_Booking_System.Hubs;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestCode
{
    public class ApprovalBlogControllerTest
    {
        private readonly Mock<IBlogService> _mockBlogService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHubContext<BlogHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly ApprovalBlogController _controller;

        public ApprovalBlogControllerTest()
        {
            _mockBlogService = new Mock<IBlogService>();
            _mockMapper = new Mock<IMapper>();
            _mockHubContext = new Mock<IHubContext<BlogHub>>();
            _mockHubClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            // Setup SignalR mock
            _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
            _mockHubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            // Khởi tạo ApprovalBlogController (sửa từ BlogController)
            _controller = new ApprovalBlogController(_mockBlogService.Object, _mockMapper.Object, _mockHubContext.Object);
        }

        private void SetupUserClaims(int userId, string role = "Admin")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for DELETE api/approvalblog/{id}

        [Fact]
        public async Task DeleteApprovedBlog_ValidId_ReturnsOk()
        {
            // Arrange
            int blogId = 1;
            SetupUserClaims(1, "Admin");
            var blog = new Blog { BlogId = blogId, UserId = 1, BlogTitle = "Test Blog", BlogContent = "Test Content", Status = (int)BlogDTO.BlogStatus.Approve, User = new User { FullName = "Test User" } };

            _mockBlogService.Setup(s => s.GetByIdWithUser(blogId)).ReturnsAsync(blog);
            _mockBlogService.Setup(s => s.Delete(blogId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteApprovedBlog(blogId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal($"Blog với ID: {blogId} đã được xóa thành công!", response.Message);

            // Verify SignalR notifications
            _mockHubClients.Verify(c => c.User("1"), Times.Once()); // Gửi tới owner
            _mockHubClients.Verify(c => c.Group("Admins"), Times.Once()); // Gửi tới nhóm Admins
            _mockHubClients.Verify(c => c.Group("Students"), Times.Once()); // Gửi tới nhóm Students
        }

        [Fact]
        public async Task DeleteApprovedBlog_BlogNotFound_ReturnsNotFound()
        {
            // Arrange
            int blogId = 1;
            SetupUserClaims(1, "Admin");

            _mockBlogService.Setup(s => s.GetByIdWithUser(blogId)).ReturnsAsync((Blog)null);

            // Act
            var result = await _controller.DeleteApprovedBlog(blogId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal($"Blog với ID: {blogId} không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task DeleteApprovedBlog_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            int blogId = 1;
            SetupUserClaims(1, "Admin");
            var blog = new Blog { BlogId = blogId, UserId = 1, BlogTitle = "Test Blog", BlogContent = "Test Content", Status = (int)BlogDTO.BlogStatus.Approve, User = new User { FullName = "Test User" } };

            _mockBlogService.Setup(s => s.GetByIdWithUser(blogId)).ReturnsAsync(blog);
            _mockBlogService.Setup(s => s.Delete(blogId)).ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.DeleteApprovedBlog(blogId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Lỗi khi xóa blog: Lỗi hệ thống", response.Message);
        }
    }
}