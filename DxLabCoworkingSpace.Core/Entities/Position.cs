using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Position
    {
        public int PositionId { get; set; }
        public int? AreaId { get; set; }
        public int? RoomId { get; set; }
        public string PositionName { get; set; } = null!;
        public int Status { get; set; }
        public int? BookingDetailId { get; set; }
        public string? UsingFacilityId { get; set; }

        public virtual Area? Area { get; set; }
        public virtual BookingDetail? BookingDetail { get; set; }
    }
}
