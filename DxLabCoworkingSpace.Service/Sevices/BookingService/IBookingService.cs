using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IBookingService : IGenericeService<Booking>
    {
        Task Remove(Booking entity); // Thêm phương thức Remove
    }
}
