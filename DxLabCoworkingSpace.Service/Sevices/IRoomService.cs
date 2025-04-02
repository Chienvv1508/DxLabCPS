using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IRoomService : IGenericeService<Room>
    {
        Task<bool> PatchRoomAsync(int id, JsonPatchDocument<Room> patchDoc);
        Task<Room> GetRoomWithAllInClude(Expression<Func<Room, bool>> expression);
    }
}
