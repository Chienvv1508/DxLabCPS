using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class UsingFacility
    {
        public long UsingFacilityId { get; set; }
        public int? FacilityId { get; set; }
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public int? AreaId { get; set; }
        public DateTime ImportDate { get; set; }

        public virtual Area? Area { get; set; }
        public virtual Facility? Facility { get; set; }
    }
}
