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
            await _unitOfWork.CommitAsync();
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

        public async Task UpdateImage(AreaType areaTypeFromDb, List<string> images)
        {
            try
            {
                var listImage = await _unitOfWork.ImageRepository.GetAll(x => x.AreaTypeId == areaTypeFromDb.AreaTypeId);
                if (images == null)
                    throw new ArgumentNullException();
                foreach (var item in images)
                {
                    var x = listImage.FirstOrDefault(x => x.ImageUrl == item);
                    if (x == null) throw new Exception("Ảnh nhập vào không phù hợp");
                    await _unitOfWork.ImageRepository.Delete(x.ImageId);

                }
                await _unitOfWork.AreaTypeRepository.Update(areaTypeFromDb);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }
    }
}
