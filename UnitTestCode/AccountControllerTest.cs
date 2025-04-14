using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestCode
{
    public class AccountControllerTest
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly AccountController _controller;

        public AccountControllerTest()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockMapper = new Mock<IMapper>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new AccountController(_mockAccountService.Object, _mockMapper.Object, _mockUnitOfWork.Object);
        }

        // Tests cho API AddFromExcel

        [Fact]
        public async Task AddFromExcel_ValidFile_ReturnsCreated()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = new byte[] { }; // Fake Excel content
            var fileName = "test.xlsx";
            var stream = new MemoryStream(content);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream>(s => stream.CopyTo(s));

            var role = new Role { RoleId = 1, RoleName = "Staff" };
            var users = new List<User>
            {
                new User { Email = "test@example.com", FullName = "Test User", RoleId = 1, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 1, Email = "test@example.com", FullName = "Test User", RoleName = "Staff", Status = true }
            };

            _mockAccountService.Setup(s => s.GetRoles()).ReturnsAsync(new List<Role> { role });
            _mockAccountService.Setup(s => s.AddFromExcel(It.IsAny<List<User>>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(It.IsAny<List<User>>())).Returns(accountDtos);

            // Act
            var result = await _controller.AddFromExcel(fileMock.Object);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(createdResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("1 tài khoản đã được thêm thành công!", response.Message);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task AddFromExcel_NullFile_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.AddFromExcel(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Không có file nào được tải lên!", response.Message);
        }

        [Fact]
        public async Task AddFromExcel_InvalidFileExtension_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = new byte[] { };
            var fileName = "test.txt"; // Sai định dạng
            var stream = new MemoryStream(content);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream>(s => stream.CopyTo(s));

            // Act
            var result = await _controller.AddFromExcel(fileMock.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Chỉ có Excel files (.xlsx) là được hỗ trợ!", response.Message);
        }

        [Fact]
        public async Task AddFromExcel_InvalidRoleName_ReturnsConflict()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = new byte[] { };
            var fileName = "test.xlsx";
            var stream = new MemoryStream(content);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream>(s => stream.CopyTo(s));

            _mockAccountService.Setup(s => s.GetRoles()).ReturnsAsync(new List<Role>()); // Không có role nào

            // Act
            var result = await _controller.AddFromExcel(fileMock.Object);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(conflictResult.Value);
            Assert.Equal(409, response.StatusCode);
            Assert.Equal("RoleName không hợp lệ, phải là Student hoặc Staff!", response.Message);
        }

        [Fact]
        public async Task AddFromExcel_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = new byte[] { };
            var fileName = "test.xlsx";
            var stream = new MemoryStream(content);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Callback<Stream>(s => stream.CopyTo(s));

            var role = new Role { RoleId = 1, RoleName = "Staff" };
            _mockAccountService.Setup(s => s.GetRoles()).ReturnsAsync(new List<Role> { role });
            _mockAccountService.Setup(s => s.AddFromExcel(It.IsAny<List<User>>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddFromExcel(fileMock.Object);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi xử lý file: Database error", response.Message);
        }

        // Tests cho API GetAllAccounts

        [Fact]
        public async Task GetAllAccounts_ReturnsOkWithAccounts()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleName = "Staff", Status = true }
            };
            _mockAccountService.Setup(s => s.GetAll()).ReturnsAsync(users);
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(users)).Returns(accountDtos);

            // Act
            var result = await _controller.GetAllAccounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Danh sách tài khoản được lấy thành công!", response.Message);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task GetAllAccounts_EmptyList_ReturnsOk()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetAll()).ReturnsAsync(new List<User>());
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(It.IsAny<IEnumerable<User>>())).Returns(new List<AccountDTO>());

            // Act
            var result = await _controller.GetAllAccounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Danh sách tài khoản được lấy thành công!", response.Message);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task GetAllAccounts_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetAll()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllAccounts();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi truy xuất tài khoản: Database error", response.Message);
        }

        // Tests cho API GetAccountById

        [Fact]
        public async Task GetAccountById_ValidId_ReturnsOk()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true };
            var accountDto = new AccountDTO { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleName = "Staff", Status = true };
            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<AccountDTO>(user)).Returns(accountDto);

            // Act
            var result = await _controller.GetAccountById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<AccountDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản được lấy thành công!", response.Message);
            Assert.Equal(1, response.Data.UserId);
        }

        [Fact]
        public async Task GetAccountById_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetAccountById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 1 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task GetAccountById_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetById(1)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAccountById(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi truy xuất tài khoản: Database error", response.Message);
        }

        // Tests cho API GetUsersByRoleName

        [Fact]
        public async Task GetUsersByRoleName_ValidRoleName_ReturnsOk()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleName = "Staff", Status = true }
            };
            _mockAccountService.Setup(s => s.GetUsersByRoleName("Staff")).ReturnsAsync(users);
            _mockMapper.Setup(m => m.Map<IEnumerable<AccountDTO>>(users)).Returns(accountDtos);

            // Act
            var result = await _controller.GetUsersByRoleName("Staff");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<AccountDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Người dùng với RoleName: Staff được lấy thành công!", response.Message);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task GetUsersByRoleName_InvalidRoleName_ReturnsBadRequest()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetUsersByRoleName("InvalidRole"))
                .ThrowsAsync(new InvalidOperationException("RoleName không hợp lệ!"));

            // Act
            var result = await _controller.GetUsersByRoleName("InvalidRole");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("RoleName không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task GetUsersByRoleName_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetUsersByRoleName("Staff"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUsersByRoleName("Staff");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi cập nhật người dùng: Database error", response.Message);
        }

        // Tests cho API UpdateAccountRole

        [Fact]
        public async Task UpdateAccountRole_ValidId_ReturnsOk()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "Staff" };
            var user = new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true };
            var updatedUser = new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 2, Status = true };
            var updatedDto = new AccountDTO { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleName = "Staff", Status = true };

            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync(user).Callback<int>(id =>
            {
                if (id == 1) _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync(updatedUser);
            });
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<AccountDTO>(updatedUser)).Returns(updatedDto);

            // Act
            var result = await _controller.UpdateAccountRole(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<AccountDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Role của người dùng đã được cập nhật thành công!", response.Message);
            Assert.Equal("Staff", response.Data.RoleName);
        }

        [Fact]
        public async Task UpdateAccountRole_NotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "Staff" };
            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.UpdateAccountRole(1, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 1 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task UpdateAccountRole_InvalidRoleName_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "" }; // RoleName rỗng, không hợp lệ
            var user = new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true };
            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync(user);
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).ThrowsAsync(new InvalidOperationException("RoleName không hợp lệ!"));

            // Act
            var result = await _controller.UpdateAccountRole(1, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("RoleName không hợp lệ!", response.Message);
        }

        [Fact]
        public async Task UpdateAccountRole_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "Staff" };
            var user = new User { UserId = 1, Email = "user1@example.com", FullName = "User One", RoleId = 1, Status = true };
            _mockAccountService.Setup(s => s.GetById(1)).ReturnsAsync(user);
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAccountRole(1, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi cập nhật người dùng: Database error", response.Message);
        }

        // Tests cho API SoftDeleteAccount

        [Fact]
        public async Task SoftDeleteAccount_ValidId_ReturnsOk()
        {
            // Arrange
            _mockAccountService.Setup(s => s.SoftDelete(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SoftDeleteAccount(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 1 đã được lưu vào Bin Storage!", response.Message);
        }

        [Fact]
        public async Task SoftDeleteAccount_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAccountService.Setup(s => s.SoftDelete(1))
                .ThrowsAsync(new InvalidOperationException("Người dùng với ID: 1 không tìm thấy!"));

            // Act
            var result = await _controller.SoftDeleteAccount(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 1 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task SoftDeleteAccount_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.SoftDelete(1)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.SoftDeleteAccount(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi xóa tạm thời tài khoản: Database error", response.Message);
        }
    }
}