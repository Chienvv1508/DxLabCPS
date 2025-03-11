using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private IRoleSevice _roleSevice;
        private readonly IMapper _mapper;

        public RoleController(IRoleSevice roleSevice, IMapper mapper)
        {
            _roleSevice = roleSevice;
            _mapper = mapper;
        }

        // Get All Student and Staff Role
        [HttpGet("GetRoleByAdmin")]
        public IActionResult GetAllRole()
        {
            var roles = _roleSevice.GetAll();
            var roleDTOs = _mapper.Map<List<RoleDTO>>(roles);
            return Ok(roleDTOs);
        }

        //Get Role By Id
        [HttpGet("{id}")]
        public IActionResult GetRoleById(int id)
        {
            var role = _roleSevice.GetById(id);
            if (role == null)
            {
                return NotFound($"Role với id: {id} không tìm thấy!");
            }
            var roleDTO = _mapper.Map<RoleDTO>(role);
            return Ok(roleDTO);
        }
    }
}