using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public interface IFacilityService : IGenericService<Facility>
    {
        Task AddFacilityFromExcel(List<Facility> facilities);
    }
}
