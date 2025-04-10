using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class ImageServiceDb : IImageServiceDb
    {
        private readonly IUnitOfWork _unitOfWork;

        public ImageServiceDb(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task Add(Image entity)
        {
            throw new NotImplementedException();
        }

        public async Task Delete(int id)
        {
            try
            {

                await _unitOfWork.ImageRepository.Delete(id);
                    await _unitOfWork.CommitAsync();
                
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public Task<Image> Get(Expression<Func<Image, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Image>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Image>> GetAll(Expression<Func<Image, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Image>> GetAllWithInclude(params Expression<Func<Image, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<Image> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Image> GetWithInclude(Expression<Func<Image, bool>> expression, params Expression<Func<Image, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task Update(Image entity)
        {
            throw new NotImplementedException();
        }
    }
}
