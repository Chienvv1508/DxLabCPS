using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DxLabCoworkingSpaceContext _dbContext;
        private IGenericRepository<Role> _roleRepository;
        private IGenericRepository<Slot> _slotRepository;
        private IGenericRepository<User> _userRepository;
        private IGenericRepository<Blog> _blogRepository;
        private IGenericRepository<Room> _roomRepository;
        public UnitOfWork(DxLabCoworkingSpaceContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public IGenericRepository<Role> RoleRepository => _roleRepository ?? new GenericRepository<Role>(_dbContext);
        public IGenericRepository<Slot> SlotRepository => _slotRepository ?? new GenericRepository<Slot>(_dbContext);
        public IGenericRepository<User> UserRepository => _userRepository ?? new GenericRepository<User>(_dbContext);
        public IGenericRepository<Blog> BlogRepository => _blogRepository ?? new GenericRepository<Blog>(_dbContext);
        public IGenericRepository<Room> RoomRepository => _roomRepository ?? new GenericRepository<Room>(_dbContext);
        public DbContext Context => _dbContext;

        public async Task CommitAsync()
        {

            await _dbContext.SaveChangesAsync();
            //using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            //{
            //    try
            //    {
            //        await _dbContext.SaveChangesAsync();
            //        await transaction.CommitAsync();
            //    }
            //    catch
            //    {
            //        await transaction.RollbackAsync();
            //        throw; // Ném lại exception để controller xử lý
            //    }
            //}
        }
        public async Task RollbackAsync()
        {
            await _dbContext.DisposeAsync();
        }
    }
}
