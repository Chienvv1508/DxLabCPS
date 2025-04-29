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
        Task<ResponseDTO<object>> AddFaciToArea(int areaid, int status, FaciAddDTO faciAddDTO);
        Task<ResponseDTO<Area>> AddNewArea(int roomId, List<AreaAdd> areaAdds);
        Task<ResponseDTO<object>> GetAreasManagementInRoom(int roomId);
        Task<ResponseDTO<Area>> RemoveArea(int areaid);
        Task<ResponseDTO<object>> RemoveFaciFromArea(RemoveFaciDTO removedFaciDTO);
    }
}
