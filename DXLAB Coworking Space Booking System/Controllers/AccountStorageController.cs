using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service.Sevices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountStorageController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public AccountStorageController(IAccountService accountService, IMapper mapper)
        {
            _accountService = accountService;
            _mapper = mapper;
        }

        // Get All Deleted Account
        [HttpGet]
        public async Task<IActionResult> GetDeletedAccounts()
        {
            try
            {
                var deletedUsers = await _accountService.GetDeletedAccounts();
                var deletedAccountDtos = _mapper.Map<List<AccountDTO>>(deletedUsers);
                return Ok(new
                {
                    Message = "Danh sách tài khoản bị xóa tạm thời đã được lấy ra thành công",
                    Accounts = deletedAccountDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi lấy tài khoản bị xóa: {ex.Message}" });
            }
        }

        // Restore Soft Deleted Account
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreAccount(int id)
        {
            try
            {
                await _accountService.Restore(id); 
                return Ok(new { Message = $"Tài khoản với ID: {id} đã được phục hồi!" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi phục hồi tài khoản: {ex.Message}" });
            }
        }

        // Hard Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDeleteAccount(int id)
        {
            try
            {
                await _accountService.Delete(id); 
                return Ok(new { Message = $"Tài khoản với ID: {id} đã được xóa vĩnh viễn!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message }); // Từ chối nếu là Admin
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi xóa vĩnh viễn tài khoản: {ex.Message}" });
            }
        }
    }
}