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

        IGenericRepository<Room> RoomRepository { get; }
        IGenericRepository<AreaType> AreaTypeRepository { get; }
        IGenericRepository<Area> AreaRepository { get; }
        IGenericRepository<Booking> BookingRepository { get; }
        IGenericRepository<BookingDetail> BookingDetailRepository { get; }
        DbContext Context { get; }
        Task CommitAsync();
        Task RollbackAsync();
    }
}
