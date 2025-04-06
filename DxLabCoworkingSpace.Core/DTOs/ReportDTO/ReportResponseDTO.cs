using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class ReportResponseDTO
    {
        public int ReportId { get; set; }
        public int? BookingDetailId { get; set; }
        public string Position { get; set; }
        public string AreaName { get; set; }
        public string AreaTypeName { get; set; }
        public string RoomName { get; set; }
        public string CreatedDate { get; set; }
        public string StaffName { get; set; }
    }
}
