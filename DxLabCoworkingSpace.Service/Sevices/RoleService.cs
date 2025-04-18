using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;   
        }

        public async Task Add(Role entity)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Role>> GetAll()
        {
            return await _unitOfWork.RoleRepository.GetAll(r => r.RoleId == 2 || r.RoleId ==3);
        }

        public async Task<Role> GetById(int id)
        {
            return await _unitOfWork.RoleRepository.GetById(id);
        }
        public async Task<IEnumerable<Role>> GetAllWithInclude(params Expression<Func<Role, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task<Role> GetWithInclude(Expression<Func<Role, bool>> expression, params Expression<Func<Role, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task Update(Role entity)
        {
            throw new NotImplementedException();
        }
        public async Task<Role> Get(Expression<Func<Role, bool>> expression)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<Role>> GetAll(Expression<Func<Role, bool>> expression)
        {
            throw new NotImplementedException();
        }
        public async Task Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
