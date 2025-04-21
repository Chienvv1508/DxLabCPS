using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class ReportRequestDTO
    {
        public int? BookingDetailId { get; set; }
        public string ReportDescription { get; set; } = null!;
        public int? FacilityQuantity { get; set; }
    }
}
