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
        public UnitOfWork(DxLabCoworkingSpaceContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public IGenericRepository<Role> RoleRepository => _roleRepository ?? new GenericRepository<Role>(_dbContext);
        public IGenericRepository<Slot> SlotRepository => _slotRepository ?? new GenericRepository<Slot>(_dbContext);
        public IGenericRepository<User> UserRepository => _userRepository ?? new GenericRepository<User>(_dbContext);
        public DbContext Context => _dbContext;
        public void Commit()
        {
            _dbContext.SaveChanges();
        }

        public async Task CommitAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public void Rollback()
        {                                               
            _dbContext.Dispose();
        }

        public async Task RollbackAsync()
        {
            await _dbContext.DisposeAsync();
        }
    }
}
