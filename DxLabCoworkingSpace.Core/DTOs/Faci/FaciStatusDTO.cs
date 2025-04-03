using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace { 
    public class FaciStatusDTO
    {
        public int FacilityStatusId { get; set; }
        public int? FacilityId { get; set; }
        public string? BatchNumber { get; set; }
        public int? Status { get; set; }
        public int Quantity { get; set; }
        public DateTime ImportDate { get; set; }

        public string FacilityName { get; set; }
    }
}
