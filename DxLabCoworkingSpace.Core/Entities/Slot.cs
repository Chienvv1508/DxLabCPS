using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Slot
    {
        public Slot(ICollection<BookingDetail> bookingDetails)
        {
            BookingDetails = bookingDetails;
        }

        public Slot()
        {
        }

        public int SlotId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int Status { get; set; }
        public int SlotNumber { get; set; }

        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
    }
}
