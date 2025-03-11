using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public AccountController(IAccountService accountService, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _accountService = accountService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // Add Account For Excel File
        [HttpPost("AddFromExcel")]
        public async Task<IActionResult> AddFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Message = "Không có file nào được tải lên!" });
                }
                if (!file.FileName.EndsWith(".xlsx"))
                {
                    return BadRequest(new { Message = "Chỉ có Excel files (.xlsx) là được hỗ trợ!" });
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var users = new List<User>();
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var roleName = worksheet.Cells[row, 3].Value?.ToString();
                            var role = _unitOfWork.RoleRepository.GetAll()
                                .FirstOrDefault(r => r.RoleName == roleName);

                            if (string.IsNullOrEmpty(roleName) || role == null)
                            {
                                return Conflict(new { Message = $"RoleName không hợp lệ, phải là Student hoặc Staff!" });
                            }

                            users.Add(new User
                            {
                                Email = worksheet.Cells[row, 1].Value?.ToString(),
                                FullName = worksheet.Cells[row, 2].Value?.ToString(),
                                RoleId = role.RoleId,
                                Status = bool.TryParse(worksheet.Cells[row, 4   ].Value?.ToString(), out bool status) ? status : true,
                            });
                        }
                    }
                }

                await _accountService.AddFromExcel(users); 
                var accountDTOs = _mapper.Map<List<AccountDTO>>(users);
                return Created("", new
                {
                    Message = $"{users.Count} tài khoản đã được thêm thành công!",
                    Accounts = accountDTOs
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi xử lý file: {ex.Message}" });
            }
        }

        // Get All Account
        [HttpGet]
        public IActionResult GetAllAccounts()
        {
            try
            {
                var users = _accountService.GetAll().ToList();
                var accountDTOs = _mapper.Map<List<AccountDTO>>(users);
                return Ok(new
                {
                    Message = "Danh sách tài khoản được lấy thành công!",
                    Accounts = accountDTOs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi truy xuất tài khoản: {ex.Message}" });
            }
        }

        // Get Account By UserId
        [HttpGet("{id}")]
        public IActionResult GetAccountById(int id)
        {
            try
            {
                var user = _accountService.GetById(id);
                if (user == null)
                {
                    return NotFound(new { Message = $"Người dùng với ID: {id} không tìm thấy" });
                }
                var accountDto = _mapper.Map<AccountDTO>(user);
                return Ok(new
                {
                    Message = "Tài khoản được lấy thành công!",
                    Account = accountDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi truy xuất tài khoản: {ex.Message}" });
            }
        }

        // Get All Account By Role Name
        [HttpGet("role/{roleName}")]
        public IActionResult GetUsersByRoleName(string roleName)
        {
            try
            {
                var users = _accountService.GetUsersByRoleName(roleName).ToList();
                var accountDTOs = _mapper.Map<List<AccountDTO>>(users);
                return Ok(new
                {
                    Message = $"Người dùng với RoleName: {roleName} được lấy thành công!",
                    Accounts = accountDTOs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi truy xuất tài khoản: {ex.Message}" });
            }
        }

        // Update Account's Role 
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateAccountRole(int id, [FromBody] AccountDTO accountDto)
        {
            try
            {
                if (accountDto == null)
                {
                    return BadRequest(new { Message = "Dữ liệu người dùng là bắt buộc!" });
                }

                if (accountDto.UserId != id)
                {
                    return BadRequest(new { Message = "UserId không khớp với ID cần cập nhật!" });
                }

                var existingUser = _accountService.GetById(id);
                if (existingUser == null)
                {
                    return NotFound(new { Message = $"Người dùng với ID: {id} không tìm thấy!" });
                }

                if ((accountDto.Email != existingUser.Email && !string.IsNullOrEmpty(accountDto.Email)) ||
                    (accountDto.FullName != existingUser.FullName && !string.IsNullOrEmpty(accountDto.FullName)) ||
                    (accountDto.Status != existingUser.Status))
                {
                    return BadRequest(new { Message = "Chỉ có RoleName mới được cập nhật!" });
                }

                if (!string.IsNullOrEmpty(accountDto.RoleName))
                {
                    var validRoles = new[] { "Student", "Staff" };
                    if (!validRoles.Contains(accountDto.RoleName))
                    {
                        return BadRequest(new { Message = "RoleName phải là 'Student' hoặc 'Staff'!" });
                    }

                    var role = _unitOfWork.RoleRepository.GetAll()
                        .FirstOrDefault(r => r.RoleName == accountDto.RoleName);
                    if (role == null)
                    {
                        return BadRequest(new { Message = $"Role với tên: {accountDto.RoleName} không tìm thấy!" });
                    }

                    existingUser.RoleId = role.RoleId;
                    existingUser.Role = role; 
                    await _accountService.Update(existingUser); 

                    var updatedUser = _accountService.GetById(id); 
                    var updatedDto = _mapper.Map<AccountDTO>(updatedUser);
                    return Ok(new
                    {
                        Message = "Role của người dùng được cập nhật!",
                        Account = updatedDto
                    });
                }

                return BadRequest(new { Message = "RoleName phải được nhập để cập nhật!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi cập nhật role của người dùng: {ex.Message}" });
            }
        }

        // Soft Delete
        [HttpPatch("{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteAccount(int id)
        {
            try
            {
                await _accountService.SoftDelete(id); 
                return Ok(new { Message = $"Tài khoản với ID: {id} đã được lưu vào Bin Storage" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi xóa tạm thời tài khoản: {ex.Message}" });
            }
        }
    }
}