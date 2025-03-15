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
                var deletedAccountDtos = _mapper.Map<IEnumerable<AccountDTO>>(deletedUsers);
                return Ok(new ResponseDTO<IEnumerable<AccountDTO>>("Danh sách tài khoản bị xóa tạm thời đã được lấy ra thành công!", deletedAccountDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi lấy tài khoản bị xóa: {ex.Message}", null));
            }
        }

        // Restore Soft Deleted Account
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> RestoreAccount(int id)
        {
            try
            {
                await _accountService.Restore(id);
                return Ok(new ResponseDTO<object>($"Tài khoản với ID: {id} đã được phục hồi!", null));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ResponseDTO<object>(ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi phục hồi tài khoản: {ex.Message}", null));
            }
        }

        // Hard Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDeleteAccount(int id)
        {
            try
            {
                await _accountService.Delete(id);
                return Ok(new ResponseDTO<object>($"Tài khoản với ID: {id} đã được xóa vĩnh viễn!", null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ResponseDTO<object>(ex.Message, null)); // Từ chối nếu là Admin
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>(ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>($"Lỗi khi xóa vĩnh viễn tài khoản: {ex.Message}", null));
            }
        }
    }
}