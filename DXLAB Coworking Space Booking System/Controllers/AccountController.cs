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
    [Route("api/account")]
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
        [HttpPost("importexcel")]
        public async Task<IActionResult> AddFromExcel(IFormFile file)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ", ModelState));
                }
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Không có file nào được tải lên!", null));
                }
                if (!file.FileName.EndsWith(".xlsx"))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Chỉ có Excel files (.xlsx) là được hỗ trợ!", null));
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
                                return Conflict(new ResponseDTO<object>(409, "RoleName không hợp lệ, phải là Student hoặc Staff!", null));
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
                return Created("", new ResponseDTO<IEnumerable<AccountDTO>>(200, $"{users.Count} tài khoản đã được thêm thành công!", accountDtos));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, ex.Message, null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(403, ex.Message, null)); // Từ chối nếu cố thêm Admin
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<object>(409, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi xử lý file: {ex.Message}", null));
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
                return Ok(new ResponseDTO<IEnumerable<AccountDTO>>(200, "Danh sách tài khoản được lấy thành công!", accountDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi truy xuất tài khoản: {ex.Message}", null));
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
                    return NotFound(new ResponseDTO<object>(404, $"Người dùng với ID: {id} không tìm thấy!", null));
                }

                var accountDto = _mapper.Map<AccountDTO>(user);
                return Ok(new ResponseDTO<AccountDTO>(200, "Tài khoản được lấy thành công!", accountDto));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(403, ex.Message, null)); // Từ chối nếu cố thêm Admin
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi truy xuất tài khoản: {ex.Message}", null));
            }
        }

        // Get All Account By Role Name
        [HttpGet("role/{rolename}")]
        public async Task<IActionResult> GetUsersByRoleName(string roleName)
        {
            try
            {
                var users = (await _accountService.GetUsersByRoleName(roleName)).ToList();
                var accountDTOs = _mapper.Map<IEnumerable<AccountDTO>>(users);
                return Ok(new ResponseDTO<IEnumerable<AccountDTO>>(200, $"Người dùng với RoleName: {roleName} được lấy thành công!", accountDTOs));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(403, ex.Message, null));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật người dùng: {ex.Message}", null));
            }
        }

        // Update Account's Role 
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateAccountRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseDTO<object>(400, "Dữ liệu không hợp lệ", ModelState));
                }

                // Retrieve the existing user first to preserve other fields
                var existingUser = await _accountService.GetById(id);
                if (existingUser == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Người dùng với ID: {id} không tìm thấy!", null));
                }

                // Update only the role information
                existingUser.Role = new Role { RoleName = request.RoleName };

                await _accountService.Update(existingUser);
                var updatedUser = await _accountService.GetById(id);
                var updatedDto = _mapper.Map<AccountDTO>(updatedUser);
                return Ok(new ResponseDTO<AccountDTO>(200, "Role của người dùng đã được cập nhật thành công!", updatedDto));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(403, ex.Message, null));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>(400, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi cập nhật người dùng: {ex.Message}", null));
            }
        }

        // Soft Delete
        [HttpPatch("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteAccount(int id)
        {
            try
            {
                await _accountService.SoftDelete(id);
                return Ok(new ResponseDTO<object>(200, $"Tài khoản với ID: {id} đã được lưu vào Bin Storage!", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(403, ex.Message, null)); // Từ chối nếu là Admin
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ResponseDTO<object>(404, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi xóa tạm thời tài khoản: {ex.Message}", null));
            }
        }
    }
}