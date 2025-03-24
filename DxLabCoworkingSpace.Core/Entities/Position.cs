using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Position
    {
        public Position()
        {
            BookingDetails = new HashSet<BookingDetail>();
        }

        public int PositionId { get; set; }
        public int AreaId { get; set; }
        public int PositionNumber { get; set; }
        public bool Status { get; set; }

        public virtual Area Area { get; set; } = null!;
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
    }
}
