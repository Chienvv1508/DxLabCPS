using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Notification
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; }
        public int? UserId { get; set; }
        public int? BookingId { get; set; }

        public virtual Booking? Booking { get; set; }
        public virtual User? User { get; set; }
    }
}
