using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace

{
    public partial class Area
    {
        public Area()
        {
            Positions = new HashSet<Position>();
            UsingFacilities = new HashSet<UsingFacility>();
        }

        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public int RoomId { get; set; }
        public int AreaTypeId { get; set; }

        public virtual Room? Room { get; set; }
        public virtual AreaType? AreaType { get; set; }

        public virtual ICollection<Position> Positions { get; set; }
        public virtual ICollection<UsingFacility> UsingFacilities { get; set; }
    }
}
