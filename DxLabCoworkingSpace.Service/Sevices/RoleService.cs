using DXLAB_Coworking_Space_Booking_System;
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

        async Task IGenericService<Role>.Add(Role entity)
        {
            throw new NotImplementedException();
        }

        async Task<IEnumerable<Role>> IGenericService<Role>.GetAll()
        {
            return await _unitOfWork.RoleRepository.GetAll(r => r.RoleId == 2 || r.RoleId ==3);
        }

        async Task<Role> IGenericService<Role>.GetById(int id)
        {
            return await _unitOfWork.RoleRepository.GetById(id);
        }

        async Task IGenericService<Role>.Update(Role entity)
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
        async Task IGenericService<Role>.Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
