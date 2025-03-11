using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _entitySet;

        public GenericRepository(DbContext context)
        {
            _dbContext = context;
            _entitySet = _dbContext.Set<T>();
        }
        public T Get(Expression<Func<T, bool>> expression)
        {
            return _entitySet.FirstOrDefault(expression);
        }

        public IEnumerable<T> GetAll()
        {
            return _entitySet.AsEnumerable();
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>> expression)
        {
            return _entitySet.Where(expression).AsEnumerable();
        }
        public T GetById(int id)
        {
            return _entitySet.Find(id);
        }
        public async Task Add(T entity)
        {
            _entitySet.Add(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(T entity)
        {
            _entitySet.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var entity = await _entitySet.FindAsync(id);
            if (entity != null)
            {
                _entitySet.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
