using AutoMapper;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        [HttpGet]
        public IActionResult Index()
        {
           var roles = _roleSevice.GetAll();

            var roleDTOs = _mapper.Map<List<RoleDto>>(roles);
            return Ok(roleDTOs);
        }
    }
}
