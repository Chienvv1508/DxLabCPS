using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
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
        private IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Booking entity)
        {
            await _unitOfWork.BookingRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Booking> Get(Expression<Func<Booking, bool>> expression)
        {
           return await _unitOfWork.BookingRepository.Get(expression);
        }

        public async Task<IEnumerable<Booking>> GetAll()
        {
            return await _unitOfWork.BookingRepository.GetAll();
        }

        public Task<IEnumerable<Booking>> GetAll(Expression<Func<Booking, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Booking>> GetAllWithInclude(params Expression<Func<Booking, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public async Task<Booking> GetById(int id)
        {
            return await _unitOfWork.BookingRepository.GetById(id);
        }

        public Task<Booking> GetWithInclude(Expression<Func<Booking, bool>> expression, params Expression<Func<Booking, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public async Task Update(Booking entity)
        {
            await _unitOfWork.BookingRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
    }
}
