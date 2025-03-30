using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypeService : IAreaTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AreaTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(AreaType entity)
        {
            await _unitOfWork.AreaTypeRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AreaType> Get(Expression<Func<AreaType, bool>> expression)
        {
            return await _unitOfWork.AreaTypeRepository.GetWithInclude(expression, x => x.Images);

        }

        public async Task<IEnumerable<AreaType>> GetAll()
        {
            return await _unitOfWork.AreaTypeRepository.GetAllWithInclude( x => x.Images);
        }

        public async Task<IEnumerable<AreaType>> GetAll(Expression<Func<AreaType, bool>> expression)
        {
            return await _unitOfWork.AreaTypeRepository.GetAll(expression);
        }

        public async Task<AreaType> GetById(int id)
        {
            return await _unitOfWork.AreaTypeRepository.GetById(id);
        }

        public async Task Update(AreaType entity)
        {
            await _unitOfWork.AreaTypeRepository.Update(entity);
        }
        public async Task<object> GetAreaTypeForAddRoom()
        {
            var listAreaType = await _unitOfWork.AreaTypeRepository.GetAll();
            var listAreaTypeResult = listAreaType.Select(x => new AreaAddDTO() { AreaTypeId = x.AreaTypeId, AreaTypeName = x.AreaTypeName, Size = x.Size });
            return listAreaTypeResult;
        }

        public async Task<IEnumerable<AreaType>> GetAllWithInclude(params Expression<Func<AreaType, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeRepository.GetAllWithInclude(includes);
        }

        public async Task<AreaType> GetWithInclude(Expression<Func<AreaType, bool>> expression, params Expression<Func<AreaType, object>>[] includes)
        {
            return await _unitOfWork.AreaTypeRepository.GetWithInclude(expression, includes);
        }
    }
}
