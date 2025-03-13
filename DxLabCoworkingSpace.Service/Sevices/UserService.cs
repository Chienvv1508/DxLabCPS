using DXLAB_Coworking_Space_Booking_System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(User entity)
        {
            await _unitOfWork.UserRepository.Add(entity);
            _unitOfWork.CommitAsync();
        }

        public async Task<User> Get(Expression<Func<User, bool>> expression)
        {
            return await _unitOfWork.UserRepository.Get(expression);
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAll(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }
         async Task<User> IGenericService<User>.GetById(int id)
        {
            return await _unitOfWork.UserRepository.GetById(id);
        }

        public async Task Update(User entity)
        {
            throw new NotImplementedException();
        }
        async Task IGenericService<User>.Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
