using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IRoomService : IGenericService<Room>
    {
        Task<bool> PatchRoomAsync(int id, JsonPatchDocument<Room> patchDoc);
    }
}
