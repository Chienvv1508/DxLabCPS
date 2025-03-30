using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class ServiceTypeRevenueDTO
    {
        public decimal TotalRevenue { get; set; }
        public List<ServiceTypeDetailDTO> ServiceTypes { get; set; }
    }
}
