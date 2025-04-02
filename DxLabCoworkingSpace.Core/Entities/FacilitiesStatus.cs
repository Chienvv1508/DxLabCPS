using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DxLabCoworkingSpace
{
    public partial class FacilitiesStatus
    {
        
        public int FacilityStatusId { get; set; }
        public int? FacilityId { get; set; }
        public string? BatchNumber { get; set; }
        public int? Status { get; set; }
        public int Quantity { get; set; }
        public DateTime ImportDate { get; set; }

        public virtual Facility? Facility { get; set; }
    }
}
