using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class RoleService : IRoleSevice
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void Add(Role entity)
        {
            throw new NotImplementedException();
        }

        public Role Get(Expression<Func<Role, bool>> expression)
        {
            return _unitOfWork.RoleRepository.Get(expression);
        }

        public IEnumerable<Role> GetAll()
        {
            return _unitOfWork.RoleRepository.GetAll();
        }

        public IEnumerable<Role> GetAll(Expression<Func<Role, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public void Remove(Role entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Role entity)
        {
            throw new NotImplementedException();
        }
    }
}
