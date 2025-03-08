using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IUnitOfWork
    {

        IGenericeRepository<Role> RoleRepository { get; }
        IGenericeRepository<User> UserRepository { get; }

        void Commit();
        void Rollback();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
