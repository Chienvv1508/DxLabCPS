using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace

{
    public partial class Area
    {
        public Area()
        {
            Images = new HashSet<Image>();
            Positions = new HashSet<Position>();
            UsingFacilities = new HashSet<UsingFacility>();
        }

        public int AreaId { get; set; }
        public int? RoomId { get; set; }
        public string AreaName { get; set; } = null!;
        public string AreaType { get; set; } = null!;
        public string? AreaDescription { get; set; }
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Room? Room { get; set; }
        public virtual ICollection<Image> Images { get; set; }
        public virtual ICollection<Position> Positions { get; set; }
        public virtual ICollection<UsingFacility> UsingFacilities { get; set; }
    }
}
