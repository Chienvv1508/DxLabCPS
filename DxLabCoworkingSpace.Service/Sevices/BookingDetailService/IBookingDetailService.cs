using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IBookingDetailService: IGenericService<BookingDetail>
    {
        Task<IEnumerable<BookingDetail>> GetAllWithInclude( params Expression<Func<BookingDetail, object>>[] includes);
        Task<BookingDetail> GetWithInclude(Expression<Func<BookingDetail, bool>> expression, params Expression<Func<BookingDetail, object>>[] includes);
    }
}
