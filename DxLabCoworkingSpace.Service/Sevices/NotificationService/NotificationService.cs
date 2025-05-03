using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Notification entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Notification> Get(Expression<Func<Notification, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Notification>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Notification>> GetAll(Expression<Func<Notification, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Notification>> GetAllWithInclude(params Expression<Func<Notification, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<Notification> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Notification> GetWithInclude(Expression<Func<Notification, bool>> expression, params Expression<Func<Notification, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(Notification entity)
        {
            throw new NotImplementedException();
        }

        async Task<ResponseDTO<object>> INotificationService.Add(Notification noti, int userId)
        {
            try
            {
                var isValid = ValidationModel<Notification>.ValidateModel(noti);
                if(isValid.Item1 == false)
                {
                    return new ResponseDTO<object>(400, isValid.Item2, null);
                }
                var user = await _unitOfWork.UserRepository.Get(x => x.UserId == userId);
                if(user == null)
                {
                    return new ResponseDTO<object>(400, "Không tìm thấy thông tin người dùng", null);
                }
                noti.UserId = user.UserId;
                noti.Status = false;
                noti.CreatedAt = DateTime.Now;
                _unitOfWork.NotificationRepository.Add(noti);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<object>(200, "Thêm thành công thông báo", null);

            }
            catch(Exception ex)
            {
                return new ResponseDTO<object>(500, "Lỗi tạo thông báo!", null);
            }
        }
    }
}
