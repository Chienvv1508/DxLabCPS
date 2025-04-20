using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Report
    {
        public int ReportId { get; set; }
        public string ReportDescription { get; set; } = null!;
        public int? FacilityQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? BookingDetailId { get; set; }
        public int? UserId { get; set; }

        public virtual BookingDetail? BookingDetail { get; set; }
        public virtual User? User { get; set; }
    }
}
