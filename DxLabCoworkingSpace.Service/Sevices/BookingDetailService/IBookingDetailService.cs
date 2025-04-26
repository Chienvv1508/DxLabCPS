using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IBookingDetailService : IGenericeService<BookingDetail>
    {
        Task<BookingDetail> GetLastBookingDetail(Expression<Func<BookingDetail, bool>> expression);
        Task UpdateStatus(int bookingDetailId, int status);
    }
}
