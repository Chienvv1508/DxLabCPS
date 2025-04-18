using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestCode
{
    public class AccountStorageTest
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AccountStorageController _controller;

        public AccountStorageTest()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockMapper = new Mock<IMapper>();
            _controller = new AccountStorageController(_mockAccountService.Object, _mockMapper.Object);
        }

        // Tests cho API GetDeletedAccounts

        [Fact]
        public async Task GetDeletedAccounts_ReturnsOkWithAccounts()
        {
            // Arrange
            var deletedUsers = new List<User>
            {
                new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = false } // Status = false vì là tài khoản bị xóa mềm
            };
            var deletedAccountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Staff", Status = false }
            };
            _mockAccountService.Setup(s => s.GetDeletedAccounts()).ReturnsAsync(deletedUsers);
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(deletedUsers)).Returns(deletedAccountDtos);

            // Act
            var result = await _controller.GetDeletedAccounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Danh sách tài khoản bị xóa tạm thời đã được lấy ra thành công!", response.Message);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task GetDeletedAccounts_EmptyList_ReturnsOk()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetDeletedAccounts()).ReturnsAsync(new List<User>());
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(It.IsAny<IEnumerable<User>>())).Returns(new List<AccountDTO>());

            // Act
            var result = await _controller.GetDeletedAccounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Danh sách tài khoản bị xóa tạm thời đã được lấy ra thành công!", response.Message);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task GetDeletedAccounts_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetDeletedAccounts()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDeletedAccounts();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi lấy tài khoản bị xóa: Database error", response.Message);
        }

        // Tests cho API RestoreAccount

        [Fact]
        public async Task RestoreAccount_ValidId_ReturnsOk()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Restore(2)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RestoreAccount(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 2 đã được phục hồi!", response.Message);
        }

        [Fact]
        public async Task RestoreAccount_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Restore(2))
                .ThrowsAsync(new InvalidOperationException("Tài khoản với ID: 2 không tìm thấy!"));

            // Act
            var result = await _controller.RestoreAccount(2);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 2 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task RestoreAccount_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Restore(2)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.RestoreAccount(2);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi phục hồi tài khoản: Database error", response.Message);
        }

        // Tests cho API HardDeleteAccount

        [Fact]
        public async Task HardDeleteAccount_ValidId_ReturnsOk()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Delete(2)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.HardDeleteAccount(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 2 đã được xóa vĩnh viễn!", response.Message);
        }

        [Fact]
        public async Task HardDeleteAccount_Unauthorized_ReturnsForbidden()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Delete(2))
                .ThrowsAsync(new UnauthorizedAccessException("Không có quyền xóa tài khoản!"));

            // Act
            var result = await _controller.HardDeleteAccount(2);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(forbiddenResult.Value);
            Assert.Equal(403, response.StatusCode);
            Assert.Equal("Không có quyền xóa tài khoản!", response.Message);
        }

        [Fact]
        public async Task HardDeleteAccount_NotFound_ReturnsBadRequest()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Delete(2))
                .ThrowsAsync(new InvalidOperationException("Tài khoản với ID: 2 không tìm thấy!"));

            // Act
            var result = await _controller.HardDeleteAccount(2);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 2 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task HardDeleteAccount_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.Delete(2)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.HardDeleteAccount(2);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi xóa vĩnh viễn tài khoản: Database error", response.Message);
        }
    }
}