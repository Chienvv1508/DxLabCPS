using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class BookingDetail
    {
        public int BookingDetailId { get; set; }
        public int Status { get; set; }
        public DateTime CheckinTime { get; set; }
        public DateTime CheckoutTime { get; set; }
        public int? BookingId { get; set; }
        public int? SlotId { get; set; }

        public virtual Booking? Booking { get; set; }
        public virtual Slot? Slot { get; set; }
        public virtual Position? Position { get; set; }
        public virtual Report? Report { get; set; }
    }
}
