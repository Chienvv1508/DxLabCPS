using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IUsingFacilytyService : IGenericeService<UsingFacility>
    {

        public Task<IEnumerable<UsingFacility>> GetAllWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes);
        public Task Add(UsingFacility entity, int status, bool statusOfArea);
        Task Update(RemoveFaciDTO removedFaciDTO);
        Task Delete(IEnumerable<UsingFacility> faciInArea);
        Task Update(UsingFacility existedFaciInArea, int status, bool statusOfArea);

        Task<ResponseDTO<List<UsingFacility>>> GetAllBrokenFaciFromReport(RemovedFaciDTO removedFaciDTO);
    }
}
