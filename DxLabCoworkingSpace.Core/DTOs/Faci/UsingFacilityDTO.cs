using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UsingFacilityDTO
    {
        public int UsingFacilityId { get; set; }
        public int? FacilityId { get; set; }
        public string? FacilityTitle { get; set; } 
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public string AreaName { get; set; }
        public DateTime ImportDate { get; set; }
    }
}
