using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IAreaService : IGenericeService<Area>
    {
        //Task<IEnumerable<Area>> GetAllWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes);
        //Task<Area> GetWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes);
    }
}
