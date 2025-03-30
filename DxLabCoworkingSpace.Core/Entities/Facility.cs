using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Facility
    {
        public Facility()
        {
            FacilitiesStatuses = new HashSet<FacilitiesStatus>();
            UsingFacilities = new HashSet<UsingFacility>();
        }

        public int FacilityId { get; set; }
        public string BatchNumber { get; set; } = null!;
        public string? FacilityDescription { get; set; }
        public decimal Cost { get; set; }
        public DateTime ExpiredTime { get; set; }
        public int Quantity { get; set; }
        public DateTime ImportDate { get; set; }
        public int FacilityCategory { get; set; }
        public int Size { get; set; }

        public virtual ICollection<FacilitiesStatus> FacilitiesStatuses { get; set; }
        public virtual ICollection<UsingFacility> UsingFacilities { get; set; }
    }
}
