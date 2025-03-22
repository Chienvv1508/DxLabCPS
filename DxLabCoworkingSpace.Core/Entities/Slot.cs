using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Slot
    {
        public int SlotId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int Status { get; set; }
        public int SlotNumber { get; set; }
        public virtual BookingDetail? BookingDetail { get; set; }
    }
}
