using AutoMapper;
using DxLabCoworkingSpace.Service.Sevices;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesController : ControllerBase
    {
        private readonly IFacilitiesService _facilitiesService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public FacilitiesController(IFacilitiesService facilitiesService, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _facilitiesService = facilitiesService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFacilities()
        {
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AddFromExcelFile(IFormFile file)
        {
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewFacility()
        {
            return Ok();
        }
    }
}
