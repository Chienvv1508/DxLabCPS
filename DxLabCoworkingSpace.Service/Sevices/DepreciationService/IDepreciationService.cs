using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IDepreciationService : IGenericeService<DepreciationSum>
    {
        public Task<IEnumerable<DepreciationSum>> GetAllWithInclude(Expression<Func<DepreciationSum, bool>> expression, params Expression<Func<DepreciationSum, object>>[] includes);
    }
}
