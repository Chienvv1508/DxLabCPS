using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Blog entity)
        {
            await _unitOfWork.BlogRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<Blog> Get(Expression<Func<Blog, bool>> expression)
        {
            return await _unitOfWork.BlogRepository.Get(expression);
        }

        public async Task<IEnumerable<Blog>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Blog>> GetAll(Expression<Func<Blog, bool>> expression)
        {
            throw new NotImplementedException();
        }
        async Task<Blog> IGenericService<Blog>.GetById(int id)
        {
            return await _unitOfWork.BlogRepository.GetById(id);
        }

        public async Task Update(Blog entity)
        {
            throw new NotImplementedException();
        }
        async Task IGenericService<Blog>.Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
