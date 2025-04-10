using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IFacilityService : IGenericeService<Facility>
    {
        Task AddFacilityFromExcel(List<Facility> facilities);
        Task Update(IEnumerable<Facility> faciKHList);
    }
}
