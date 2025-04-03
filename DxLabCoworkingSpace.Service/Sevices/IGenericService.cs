using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IGenericeService<T> where T : class
    {
        Task<T> Get(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> GetAll();
        Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>> expression);
        Task<T> GetById(int id);
        Task<IEnumerable<T>> GetAllWithInclude(params Expression<Func<T, object>>[] includes);
        Task<T> GetWithInclude(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes);
        Task Add(T entity);
        Task Update(T entity);
        Task Delete(int id);


    }
}