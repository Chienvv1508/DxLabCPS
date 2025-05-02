using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IBookingService : IGenericeService<Booking>
    {
        Task<ResponseDTO<object>> Cancel(int bookingId, int userId);
        Task<ResponseDTO<object>> CreateBooking(BookingDTO bookingDTO, int userId);
        Task<ResponseDTO<object>> GetAvailableSlot(AvailableSlotRequestDTO availableSlotRequestDTO);
        Task Remove(Booking entity); // Thêm phương thức Remove
    }
}
