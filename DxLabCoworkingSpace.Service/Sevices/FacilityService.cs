using DxLabCoworkingSpace.Core.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class FacilityService : IFacilityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FacilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Import Facility Form Excel File
        // Import Facility From Excel File
        public async Task AddFacilityFromExcel(List<Facility> facilities)
        {
            if (facilities == null || !facilities.Any())
            {
                throw new ArgumentException("Danh sách facility không được rỗng hoặc null");
            }

            var existingBatchNumbers = (await _unitOfWork.FacilityRepository.GetAll())
                .Select(f => f.BatchNumber.Trim().ToLower())
                .ToHashSet();

            var batchNumbersInFile = facilities.Select(f => f.BatchNumber.Trim().ToLower()).ToList();

            var duplicateBatchNumbersInFile = batchNumbersInFile
                .GroupBy(b => b)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateBatchNumbersInFile.Any())
            {
                throw new InvalidOperationException("BatchNumber bị trùng trong file!");
            }

            var duplicateBatchNumbersInDB = batchNumbersInFile
                .Where(b => existingBatchNumbers.Contains(b))
                .ToList();

            if (duplicateBatchNumbersInDB.Any())
            {
                throw new InvalidOperationException("BatchNumber đã tồn tại trong database!");
            }

            var validationErrors = new List<string>();
            var validFacilities = new List<Facility>();

            for (int i = 0; i < facilities.Count; i++)
            {
                var facility = facilities[i];
                var row = i + 2; // Dòng 2 tương ứng với index 0

                var dto = new FacilitiesDTO
                {
                    BatchNumber = facility.BatchNumber,
                    FacilityDescription = facility.FacilityDescription,
                    Cost = facility.Cost,
                    ExpiredTime = facility.ExpiredTime,
                    Quantity = facility.Quantity,
                    ImportDate = facility.ImportDate
                };

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    validationErrors.AddRange(validationResults.Select(r => $"{r.ErrorMessage}"));
                    continue;
                }

                if (facility.ExpiredTime <= facility.ImportDate)
                {
                    validationErrors.Add("ExpiredTime phải lớn hơn ImportDate!");
                    break;
                }

                validFacilities.Add(facility);
            }

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" ", validationErrors));
            }

            foreach (var facility in validFacilities)
            {
                await _unitOfWork.FacilityRepository.Add(facility);
            }
            await _unitOfWork.CommitAsync();
        }

        // Create New Facility
        public async Task Add(Facility entity)
        {
            if (entity.ExpiredTime <= entity.ImportDate)
            {
                throw new ArgumentException("Ngày hết hạn phải lớn hơn ngày nhập");
            }
            var existingFacility = await _unitOfWork.FacilityRepository.Get(f => f.BatchNumber == entity.BatchNumber);
            if (existingFacility != null)
            {
                throw new InvalidOperationException("BatchNumber đã tồn tại!");
            }

            await _unitOfWork.FacilityRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        // Triển khai Get để tìm Facility theo biểu thức LINQ
        public async Task<Facility> Get(Expression<Func<Facility, bool>> expression)
        {
            var facility = await _unitOfWork.FacilityRepository.Get(expression);
            return facility; // Trả về null nếu không tìm thấy
        }

        public async Task<IEnumerable<Facility>> GetAll()
        {
            return await _unitOfWork.FacilityRepository.GetAll();
        }

        public async Task<IEnumerable<Facility>> GetAll(Expression<Func<Facility, bool>> expression)
        {
            throw new NotImplementedException();
        }
        async Task<Facility> IGenericService<Facility>.GetById(int id)
        {
            return await _unitOfWork.FacilityRepository.GetById(id);
        }

        public async Task Update(Facility entity)
        {
            throw new NotImplementedException();
        }
        async Task IGenericService<Facility>.Delete(int id)
        {
            throw new NotImplementedException();
        }

    }
}
