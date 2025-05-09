﻿using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/role")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private IRoleService _roleSevice;
        private readonly IMapper _mapper;

        public RoleController(IRoleService roleSevice, IMapper mapper)
        {
            _roleSevice = roleSevice;
            _mapper = mapper;
        }

        // Get All Student and Staff Role
        [HttpGet("rolebyadmin")]
        public async Task<IActionResult> GetRoleByAdmin()
        {
            try
            {
                var roles = await _roleSevice.GetAll();
                if (roles == null || !roles.Any())
                {
                    return NotFound(new ResponseDTO<object>(404, "Không tìm thấy role nào!", null));
                }

                var roleDtos = _mapper.Map<IEnumerable<RoleDTO>>(roles);
                return Ok(new ResponseDTO<IEnumerable<RoleDTO>>(200, "Danh sách role đã được lấy thành công!", roleDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy danh sách role: {ex.Message}", null));
            }
        }

        //Get Role By Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            try
            {
                var role = await _roleSevice.GetById(id);
                if (role == null)
                {
                    return NotFound(new ResponseDTO<object>(404, $"Role với ID: {id} không tìm thấy!", null));
                }

                var roleDto = _mapper.Map<RoleDTO>(role);
                return Ok(new ResponseDTO<RoleDTO>(200, "Lấy thông tin role thành công!", roleDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, $"Lỗi khi lấy role: {ex.Message}", null));
            }
        }
    }
}