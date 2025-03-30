using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Booking
    {
        public Booking()
        {
            BookingDetails = new HashSet<BookingDetail>();
            Notifications = new HashSet<Notification>();
        }

        public int BookingId { get; set; }
        public int? UserId { get; set; }
        public DateTime BookingCreatedDate { get; set; }
        public decimal Price { get; set; }
        public virtual User? User { get; set; }
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
    }
}
