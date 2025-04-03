using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IFaciStatusService : IGenericeService<FacilitiesStatus>
    {
        public Task<IEnumerable<FacilitiesStatus>> GetAllWithInclude(Expression<Func<FacilitiesStatus, bool>> expression, params Expression<Func<FacilitiesStatus, object>>[] includes);
    }
}
