using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IUsingFacilytyService:IFaciStatusService<UsingFacility>
    {

        public Task<IEnumerable<UsingFacility>> GetAllWithInclude(Expression<Func<UsingFacility, bool>> expression , params Expression<Func<UsingFacility, object>>[] includes);
        
    }
}
