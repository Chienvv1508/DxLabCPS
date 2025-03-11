using System;
using System.Collections.Generic;

 namespace DxLabCoworkingSpace

{
    public partial class FacilitiesStatus
    {
        public string FacilityStatusId { get; set; } = null!;
        public int? FailityId { get; set; }
        public string? BatchNumber { get; set; }
        public int? Status { get; set; }
        public int Quantity { get; set; }

        public virtual Facility? Facility { get; set; }
    }
}
