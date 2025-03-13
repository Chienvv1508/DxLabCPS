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
        IGenericRepository<Blog> BlogRepository { get; }
        DbContext Context { get; }
        Task CommitAsync();
        Task RollbackAsync();
    }
}
