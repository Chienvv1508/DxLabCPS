using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaTypeController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateAreaType([FromBody] AreaTypeDTO areTypeDto)
        {
            
            return Ok();
        }
    }
}
