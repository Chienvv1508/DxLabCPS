using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace UnitTestCode
{
    public class UserControllerTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILabBookingJobService> _mockLabBookingJobService;
        private readonly UserController _controller;

        public UserControllerTest()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockUserService = new Mock<IUserService>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsASecretKeyThatIsAtLeast32BytesLong");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _controller = new UserController(_mockConfig.Object, _mockUserService.Object, _mockLabBookingJobService.Object);
        }

        [Fact]
        public async Task VerifyAccount_ExistingUser_ReturnsOkWithToken() // UTCID01
        {
            // Arrange
            var userDto = new UserDTO { Email = "ducnthe161294@fpt.edu.vn", WalletAddress = "0x123", FullName = "Test User" };
            var existingUser = new User
            {
                UserId = 1,
                Email = "dungthe161294@fpt.edu.vn",
                WalletAddress = "NULL",
                FullName = "Test User",
                RoleId = 3,
                Status = true,
                Role = new Role { RoleName = "Student" }
            };

            _mockUserService.Setup(s => s.GetWithInclude(
                It.Is<Expression<Func<User, bool>>>(expr => expr.Compile()(existingUser)), // Predicate
                It.IsAny<Expression<Func<User, object>>[]>() // Include Role
            )).ReturnsAsync(existingUser);

            _mockUserService.Setup(s => s.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.VerifyAccount(userDto) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var response = result.Value as ResponseDTO<object>;
            Assert.Equal("Người dùng đã được xác thực thành công!", response.Message);
            Assert.NotNull(response.Data.GetType().GetProperty("Token").GetValue(response.Data));
            Assert.Equal("0x123", (response.Data.GetType().GetProperty("User").GetValue(response.Data) as UserDTO).WalletAddress);
        }

        [Fact]
        public async Task VerifyAccount_CannotConnectServer_ReturnsInternalServerError() // UTCID02
        {
            // Arrange
            var userDto = new UserDTO { Email = "dungthe161294@fpt.edu.vn", WalletAddress = "0x123", FullName = "Test User" };
            _mockUserService.Setup(s => s.GetWithInclude(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, object>>[]>()
            )).ThrowsAsync(new Exception("Cannot connect to server"));

            // Act
            var result = await _controller.VerifyAccount(userDto) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            var response = result.Value as ResponseDTO<object>;
            Assert.Contains("Lỗi khi xử lý người dùng: Cannot connect to server", response.Message);
        }

        [Fact]
        public async Task VerifyAccount_InvalidEmail_ReturnsUnauthorized() // UTCID03
        {
            // Arrange
            var userDto = new UserDTO { Email = "ductrungnguyen@gmail.com", WalletAddress = "0x789", FullName = "Test User" };
            _mockUserService.Setup(s => s.GetWithInclude(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Expression<Func<User, object>>[]>()
            )).ReturnsAsync((User)null);

            // Act
            var result = await _controller.VerifyAccount(userDto) as UnauthorizedObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
            var response = result.Value as ResponseDTO<object>;
            Assert.Equal("Email không tồn tại trong hệ thống!", response.Message);
        }

        [Fact]
        public async Task VerifyAccount_NullEmail_ReturnsBadRequest() // UTCID04
        {
            // Arrange
            var userDto = new UserDTO { Email = null, WalletAddress = "0x123", FullName = "Test User" };
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.VerifyAccount(userDto) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            var response = result.Value as ResponseDTO<object>;
            Assert.Equal("Lỗi: Email không được để trống.", response.Message);
        }
    }
}
