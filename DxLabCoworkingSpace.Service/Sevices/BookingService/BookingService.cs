using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class BookingService : IBookingService
    {
        public Task Add(Booking entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Booking> Get(Expression<Func<Booking, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Booking>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Booking>> GetAll(Expression<Func<Booking, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<Booking> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task Update(Booking entity)
        {
            throw new NotImplementedException();
        }
    }
}
