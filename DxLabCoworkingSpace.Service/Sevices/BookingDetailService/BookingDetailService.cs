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
        private IUnitOfWork _unitOfWork;

        public BookingDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(BookingDetail entity)
        {

            await _unitOfWork.BookingDetailRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<BookingDetail> Get(Expression<Func<BookingDetail, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<BookingDetail>> GetAll()
        {
            return await _unitOfWork.BookingDetailRepository.GetAll();
        }

        public async Task<IEnumerable<BookingDetail>> GetAll(Expression<Func<BookingDetail, bool>> expression)
        {
            return await _unitOfWork.BookingDetailRepository.GetAll(expression);
        }

      

        public async Task<IEnumerable<BookingDetail>> GetAllWithInclude(params Expression<Func<BookingDetail, object>>[] includes)
        {
                return await _unitOfWork.BookingDetailRepository.GetAllWithInclude(includes);
            
        }

        public async Task<BookingDetail> GetById(int id)
        {
            return await _unitOfWork.BookingDetailRepository.GetById(id);


        }

       

        public Task<BookingDetail> GetWithInclude(Expression<Func<BookingDetail, bool>> expression, params Expression<Func<BookingDetail, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public async Task Update(BookingDetail entity)
        {
            await _unitOfWork.BookingDetailRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
    }
}
