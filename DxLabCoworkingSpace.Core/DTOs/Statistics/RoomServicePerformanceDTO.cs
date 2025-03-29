using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class RoomServicePerformanceDTO
    {
        public string RoomName { get; set; }
        public string ServiceType { get; set; }
        public decimal TotalRevenue { get; set; }
        public int BookingCount { get; set; }
        public decimal AverageRevenuePerBooking { get; set; }
    }
}
