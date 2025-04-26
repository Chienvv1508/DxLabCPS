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

        public async Task<BookingDetail> GetLastBookingDetail(Expression<Func<BookingDetail, bool>> expression)
        {
            return await _unitOfWork.BookingDetailRepository.GetLast(expression);
        }

        public async Task<BookingDetail> GetWithInclude(Expression<Func<BookingDetail, bool>> expression, params Expression<Func<BookingDetail, object>>[] includes)
        {
            return await _unitOfWork.BookingDetailRepository.GetWithInclude(expression, includes);
        }

        public async Task Update(BookingDetail entity)
        {
            await _unitOfWork.BookingDetailRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task UpdateStatus(int bookingDetailId, int status)
        {
            // Status: 0 = Pending, 1 = CheckedIn, 2 = Completed
            var bookingDetail = await _unitOfWork.BookingDetailRepository.GetById(bookingDetailId);
            if (bookingDetail == null)
            {
                throw new Exception($"Không tìm thấy BookingDetail với ID {bookingDetailId}");
            }

            bookingDetail.Status = status; // Cập nhật trực tiếp giá trị int
            await _unitOfWork.BookingDetailRepository.Update(bookingDetail);
            await _unitOfWork.CommitAsync();
        }
    }
}
