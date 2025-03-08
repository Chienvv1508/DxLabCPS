using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class GenericRepository<T> : IGenericeRepository<T> where T : class
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _entitySet;

        public GenericRepository(DbContext context)
        {
            _dbContext = context;
            _entitySet = _dbContext.Set<T>();
        }
        public void Add(T entity)
        {
            _dbContext.Add(entity);
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

        public void Remove(T entity)
        {
            _dbContext.Remove(entity);
        }

        public void Update(T entity)
        {
            _dbContext.Update(entity);
        }
    }
}
