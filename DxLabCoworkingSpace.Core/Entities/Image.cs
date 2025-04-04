using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Image
    {
        public int ImageId { get; set; }
        public string? ImageUrl { get; set; }
        public int? RoomId { get; set; }
        public int? AreaTypeId { get; set; }
        public int? FacilityId { get; set; }
        public int? BlogId { get; set; }
        public int? AreaId { get; set; }

        public virtual Area? Area { get; set; }
        public virtual AreaType? AreaType { get; set; }
        public virtual Blog? Blog { get; set; }
        public virtual Room? Room { get; set; }
    }
}
