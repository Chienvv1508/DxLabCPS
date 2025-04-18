using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Area
    {
        public Area()
        {
            BookingDetails = new HashSet<BookingDetail>();
            Positions = new HashSet<Position>();
            UsingFacilities = new HashSet<UsingFacility>();
        }

        public int AreaId { get; set; }
        public int RoomId { get; set; }
        public int AreaTypeId { get; set; }
        public string AreaName { get; set; } = null!;
        public string? AreaDescription { get; set; }
        public virtual AreaType AreaType { get; set; } = null!;

        public int Status { get; set; }
        public virtual Room Room { get; set; } = null!;
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<Position> Positions { get; set; }
        public virtual ICollection<UsingFacility> UsingFacilities { get; set; }
        public virtual ICollection<Image> Images { get; set; }
    }
}
