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
using NBitcoin;
using NBitcoin.Protocol;

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
                    return BadRequest(new ResponseDTO<object>("Không có file nào được tải lên!", null));
                }
                if (!file.FileName.EndsWith(".xlsx"))
                {
                    return BadRequest(new ResponseDTO<object>("Chỉ có Excel files (.xlsx) là được hỗ trợ!", null));
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var users = new List<User>();
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var roleName = worksheet.Cells[row, 3].Value?.ToString();
                            var role = (await _unitOfWork.RoleRepository.GetAll())
                                .FirstOrDefault(r => r.RoleName == roleName);

                            if (string.IsNullOrEmpty(roleName) || role == null)
                            {
                                return Conflict(new ResponseDTO<object>("RoleName không hợp lệ, phải là Student hoặc Staff!", null));
                            }

                            users.Add(new User
                            {
                                Email = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                                FullName = worksheet.Cells[row, 2].Value?.ToString() ?? "",
                                RoleId = role.RoleId,
                                Status = bool.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out bool status) ? status : true,
                            });
                        }
                    }
                }

                await _accountService.AddFromExcel(users);
                var accountDtos = _mapper.Map<IEnumerable<AccountDTO>>(users);
                return Created("", new ResponseDTO<IEnumerable<AccountDTO>>($"{users.Count} tài khoản đã được thêm thành công!", accountDtos));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>(ex.Message, null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu cố thêm Admin
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi xử lý file: {ex.Message}", null));
            }
        }

        // Get All Account
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            try
            {
                var users = (await _accountService.GetAll()).ToList();
                var accountDtos = _mapper.Map<IEnumerable<AccountDTO>>(users);
                return Ok(new ResponseDTO<IEnumerable<AccountDTO>>("Danh sách tài khoản được lấy thành công!", accountDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi truy xuất tài khoản: {ex.Message}", null));
            }
        }

        // Get Account By UserId
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            try
            {
                var user = await _accountService.GetById(id);
                if (user == null)
                {
                    return NotFound(new ResponseDTO<object>($"Người dùng với ID: {id} không tìm thấy!", null));
                }

                var accountDto = _mapper.Map<AccountDTO>(user);
                return Ok(new ResponseDTO<AccountDTO>("Tài khoản được lấy thành công!", accountDto));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu cố thêm Admin
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi truy xuất tài khoản: {ex.Message}", null));
            }
        }

        // Get All Account By Role Name
        [HttpGet("role/{roleName}")]
        public async Task<IActionResult> GetUsersByRoleName(string roleName)
        {
            try
            {
                var users = (await _accountService.GetUsersByRoleName(roleName)).ToList();
                var accountDtos = _mapper.Map<IEnumerable<AccountDTO>>(users);
                return Ok(new ResponseDTO<IEnumerable<AccountDTO>>($"Người dùng với RoleName: {roleName} được lấy thành công!", accountDtos));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu cố thêm Admin
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>(ex.Message, null)); // Role không tồn tại hoặc không hợp lệ
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi truy xuất tài khoản: {ex.Message}", null));
            }
        }

        public class UpdateRoleRequest
        {
            public string RoleName { get; set; } = null!;
        }
        // Update Account's Role 
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateAccountRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.RoleName))
                {
                    return BadRequest(new ResponseDTO<object>("RoleName là bắt buộc và không để trống!", null));
                }

                var existingUser = await _accountService.GetById(id);
                if (existingUser == null)
                {
                    return NotFound(new ResponseDTO<object>($"Người dùng với ID: {id} không tìm thấy!", null));
                }

                var validRole = new[] { "Student", "Staff" };
                if (!validRole.Contains(request.RoleName))
                {
                    return BadRequest(new ResponseDTO<object>("RoleName phải là 'Student' hoặc 'Staff'!", null));
                }

                var role = (await _unitOfWork.RoleRepository.GetAll()).FirstOrDefault(r => r.RoleName == request.RoleName);
                if (role == null)
                {
                    return BadRequest(new ResponseDTO<object>($"Role với tên: {request.RoleName} không tìm thấy!", null));
                }

                existingUser.RoleId = role.RoleId;
                existingUser.Role = role;
                await _accountService.Update(existingUser);

                var updatedUser = await _accountService.GetById(id);
                var updatedDto = _mapper.Map<AccountDTO>(updatedUser);
                return Ok(new ResponseDTO<AccountDTO>("Role của người dùng đã được cập nhật thành công!", updatedDto));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu user là Admin
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>(ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi cập nhật người dùng: {ex.Message}", null));
            }
        }

        // Soft Delete
        [HttpPatch("{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteAccount(int id)
        {
            try
            {
                await _accountService.SoftDelete(id);
                return Ok(new ResponseDTO<object>($"Tài khoản với ID: {id} đã được lưu vào Bin Storage!", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu là Admin
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ResponseDTO<object>(ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi xóa tạm thời tài khoản: {ex.Message}", null));
            }
        }
    }
}