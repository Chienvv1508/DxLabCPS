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
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly AccountController _controller;

        public AccountControllerTest()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockMapper = new Mock<IMapper>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new AccountController(_mockAccountService.Object, _mockRoleService.Object, _mockMapper.Object, _mockUnitOfWork.Object);
        }

        // Helper method to create a mock Excel file with data
        private MemoryStream CreateExcelFile(string roleName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                // Add headers
                worksheet.Cells[1, 1].Value = "Email";
                worksheet.Cells[1, 2].Value = "FullName";
                worksheet.Cells[1, 3].Value = "RoleName";
                worksheet.Cells[1, 4].Value = "Status";

                // Add data
                worksheet.Cells[2, 1].Value = "ducnguyen11042002@gmail.com";
                worksheet.Cells[2, 2].Value = "Đức Staff";
                worksheet.Cells[2, 3].Value = roleName; // RoleName will be passed as a parameter
                worksheet.Cells[2, 4].Value = "true";

                // Convert ExcelPackage to MemoryStream
                var stream = new MemoryStream(package.GetAsByteArray());
                return stream;
            }
        }

        // Tests cho API AddFromExcel

        [Fact]
        public async Task AddFromExcel_ValidFile_ReturnsCreated()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var stream = CreateExcelFile("Staff"); // Create Excel file with valid RoleName = "Staff"
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("test.xlsx");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => stream.CopyTo(s));

            var role = new Role { RoleId = 2, RoleName = "Staff" };
            var users = new List<User>
            {
                new User { Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 1, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Staff", Status = true }
            };

            _mockRoleService.Setup(s => s.GetAll()).ReturnsAsync(new List<Role> { role });
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
            var stream = CreateExcelFile("Staff"); // Create Excel file (even though extension is wrong)
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("Account.xls"); // Wrong extension
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => stream.CopyTo(s));

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
            var stream = CreateExcelFile("InvalidRole"); // Create Excel file with invalid RoleName
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("WrongRole.xlsx");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => stream.CopyTo(s));

            _mockRoleService.Setup(s => s.GetAll()).ReturnsAsync(new List<Role>()); // No roles available

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
            var stream = CreateExcelFile("Staff"); // Create Excel file with valid RoleName = "Staff"
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.FileName).Returns("Account.xlsx");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => stream.CopyTo(s));

            var role = new Role { RoleId = 2, RoleName = "Staff" };
            _mockRoleService.Setup(s => s.GetAll()).ReturnsAsync(new List<Role> { role });
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
                new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Staff", Status = true }
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
            var user = new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true };
            var accountDto = new AccountDTO { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Staff", Status = true };
            _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<AccountDTO>(user)).Returns(accountDto);

            // Act
            var result = await _controller.GetAccountById(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<AccountDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản được lấy thành công!", response.Message);
            Assert.Equal(2, response.Data.UserId);
        }

        [Fact]
        public async Task GetAccountById_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetById(8)).ReturnsAsync((User)null);

            // Act 
            var result = await _controller.GetAccountById(8);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 8 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task GetAccountById_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetById(2)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAccountById(2);

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
                new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true }
            };
            var accountDtos = new List<AccountDTO>
            {
                new AccountDTO { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Staff", Status = true }
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
            var user = new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true };
            var updatedUser = new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 1, Status = true };
            var updatedDto = new AccountDTO { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleName = "Student", Status = true };

            _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync(user).Callback<int>(id =>
            {
                if (id == 2) _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync(updatedUser);
            });
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<AccountDTO>(updatedUser)).Returns(updatedDto);

            // Act
            var result = await _controller.UpdateAccountRole(2, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<AccountDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Role của người dùng đã được cập nhật thành công!", response.Message);
            Assert.Equal("Student", response.Data.RoleName);
        }

        [Fact]
        public async Task UpdateAccountRole_NotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "Staff" };
            _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.UpdateAccountRole(2, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 2 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task UpdateAccountRole_InvalidRoleName_ReturnsBadRequest()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "" }; // RoleName rỗng, không hợp lệ
            var user = new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true };
            _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync(user);
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).ThrowsAsync(new InvalidOperationException("RoleName không hợp lệ!"));

            // Act
            var result = await _controller.UpdateAccountRole(2, request);

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
            var user = new User { UserId = 2, Email = "ducnguyen11042002@gmail.com", FullName = "Đức Staff", RoleId = 2, Status = true };
            _mockAccountService.Setup(s => s.GetById(2)).ReturnsAsync(user);
            _mockAccountService.Setup(s => s.Update(It.IsAny<User>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAccountRole(2, request);

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
            _mockAccountService.Setup(s => s.SoftDelete(2)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SoftDeleteAccount(2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Tài khoản với ID: 2 đã được lưu vào Bin Storage!", response.Message);
        }

        [Fact]
        public async Task SoftDeleteAccount_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAccountService.Setup(s => s.SoftDelete(2))
                .ThrowsAsync(new InvalidOperationException("Người dùng với ID: 2 không tìm thấy!"));

            // Act
            var result = await _controller.SoftDeleteAccount(2);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Người dùng với ID: 2 không tìm thấy!", response.Message);
        }

        [Fact]
        public async Task SoftDeleteAccount_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockAccountService.Setup(s => s.SoftDelete(2)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.SoftDeleteAccount(2);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi xóa tạm thời tài khoản: Database error", response.Message);
        }
    }
}