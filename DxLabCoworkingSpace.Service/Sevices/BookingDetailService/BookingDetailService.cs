using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class BookingDetailService : IBookingDetailService
    {
        public Task Add(BookingDetail entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<BookingDetail> Get(Expression<Func<BookingDetail, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BookingDetail>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BookingDetail>> GetAll(Expression<Func<BookingDetail, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Area>> GetAllWithInclude( params Expression<Func<Area, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BookingDetail>> GetAllWithInclude(params Expression<Func<BookingDetail, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<BookingDetail> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Area> GetWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<BookingDetail> GetWithInclude(Expression<Func<BookingDetail, bool>> expression, params Expression<Func<BookingDetail, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(BookingDetail entity)
        {
            throw new NotImplementedException();
        }
    }
}
