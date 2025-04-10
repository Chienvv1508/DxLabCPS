using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        public  ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Add(Report entity)
        {
            await _unitOfWork.ReportRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Report> Get(Expression<Func<Report, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Report>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Report>> GetAll(Expression<Func<Report, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Report>> GetAllWithInclude(params Expression<Func<Report, object>>[] includes)
        {
            return await _unitOfWork.ReportRepository.GetAllWithInclude(includes);
        }

        public Task<Report> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Report> GetWithInclude(Expression<Func<Report, bool>> expression, params Expression<Func<Report, object>>[] includes)
        {
            return await _unitOfWork.ReportRepository.GetWithInclude(expression, includes);
        }

        public Task Update(Report entity)
        {
            throw new NotImplementedException();
        }
    }
}
