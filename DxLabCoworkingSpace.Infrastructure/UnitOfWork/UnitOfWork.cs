using DxLabCoworkingSpace.Infrastructure.Data;
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
        private IGenericeRepository<Role> _roleRepository;
        private IGenericeRepository<User> _userRepository;

        public UnitOfWork(DxLabCoworkingSpaceContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IGenericeRepository<Role> RoleRepository => _roleRepository ?? new GenericRepository<Role>(_dbContext);
        public IGenericeRepository<User> UserRepository => _userRepository ?? new GenericRepository<User>(_dbContext);

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
