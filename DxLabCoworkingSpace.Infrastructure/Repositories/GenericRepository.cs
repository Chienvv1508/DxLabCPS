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
        public async Task<T> Get(Expression<Func<T, bool>> expression)
        {
            return await _entitySet.FirstOrDefaultAsync(expression);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _entitySet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> expression)
        {
            return await _entitySet.Where(expression).ToListAsync();
        }
        public async Task<T> GetById(int id)
        {
            return await _entitySet.FindAsync(id);
        }
        public async Task<IEnumerable<T>> GetAllWithInclude(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _entitySet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }
        public async Task<T> GetWithInclude(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _entitySet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.FirstOrDefaultAsync(expression);
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

        public void Delete(T entity)
        {
            _entitySet.Remove(entity);
        }
    }
}
