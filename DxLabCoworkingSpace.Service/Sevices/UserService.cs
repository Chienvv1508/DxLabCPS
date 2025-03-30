
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
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
            return await _unitOfWork.UserRepository.GetWithInclude(expression, u => u.Role);
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAll(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }
         async Task<User> IFaciStatusService<User>.GetById(int id)
        {
            return await _unitOfWork.UserRepository.GetById(id);    
        }
        public async Task<IEnumerable<User>> GetAllWithInclude(params Expression<Func<User, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task<User> GetWithInclude(Expression<Func<User, bool>> expression, params Expression<Func<User, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task Update(User entity)
        {
            await _unitOfWork.UserRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
        async Task IFaciStatusService<User>.Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
