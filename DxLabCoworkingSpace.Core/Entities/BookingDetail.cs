using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class BookingDetail
    {
        public int BookingDetailId { get; set; }
        public int Status { get; set; }
        public DateTime CheckinTime { get; set; }
        public DateTime? CheckoutTime { get; set; }
        public int? BookingId { get; set; }
        public int SlotId { get; set; }
        public int? AreaId { get; set; }
        public int? PositionId { get; set; }
        public decimal Price { get; set; }
        //public string BookingGenerate { get; set; }
        //public string TransactionHash { get; set; }
        public virtual Area? Area { get; set; }
        public virtual Booking? Booking { get; set; }
        public  Position? Position { get; set; }
        public  Slot? Slot { get; set; }
        public virtual Report? Report { get; set; }

    }
}
