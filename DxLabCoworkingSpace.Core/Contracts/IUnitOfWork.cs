using DxLabCoworkingSpace;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IUnitOfWork
    {
        IGenericRepository<Role> RoleRepository { get; }
        IGenericRepository<Slot> SlotRepository { get; }
        IGenericRepository<User> UserRepository { get; }
        DbContext Context { get; }
        void Commit();
        void Rollback();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
