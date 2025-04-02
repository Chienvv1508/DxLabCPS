using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{


    public class UsingFacilityService : IUsingFacilytyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaciStatusService _facilityStatusService;

        public UsingFacilityService(IUnitOfWork unitOfWork, IFaciStatusService facilityStatusService)
        {
            _unitOfWork = unitOfWork;
            _facilityStatusService = facilityStatusService;
        }

        public async Task Add(UsingFacility entity)
        {
          await _unitOfWork.UsingFacilityRepository.Add(entity);
            
            await _unitOfWork.CommitAsync();  
        }
        public async Task Add(UsingFacility entity, int status)
        {
            await _unitOfWork.UsingFacilityRepository.Add(entity);
            var updateFaciStatus = await _facilityStatusService.Get(x => x.FacilityId == entity.FacilityId && x.BatchNumber == entity.BatchNumber
                && x.ImportDate == entity.ImportDate && x.Status == status);
            if(updateFaciStatus == null)
            {
                throw new Exception("Lỗi khi cập nhập trạng thái của thiết bị");
            }
            updateFaciStatus.Quantity -= entity.Quantity;

            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UsingFacility> Get(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.Get(expression);
        }

        public async Task<IEnumerable<UsingFacility>> GetAll()
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll();
        }

        public async Task<IEnumerable<UsingFacility>> GetAll(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll(expression);
        }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);
        }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {
            // Lấy danh sách chưa lọc
            var data = await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);

            // Chuyển đổi sang IQueryable để có thể áp dụng Where trước khi thực thi
            var query = data.AsQueryable().Where(expression);

            // Thực thi truy vấn
            return query.ToList();
        }

        public Task<UsingFacility> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UsingFacility> GetWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetWithInclude(expression, includes);
        }

        public async Task Update(UsingFacility entity)
        {
            await _unitOfWork.UsingFacilityRepository.Update(entity);
        }
    }
}
