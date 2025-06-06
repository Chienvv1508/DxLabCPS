﻿using Microsoft.AspNetCore.Http;
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
        Task GetAreaDisctinctFaci(Func<Room, bool> value);
        Task UpdateImage(Room roomFromDb, List<string> images);
        Task<ResponseDTO<Room>> PatchRoom(int id, JsonPatchDocument<Room> patchDoc);
        Task<ResponseDTO<Room>> AddImages(int id, List<IFormFile> images);
        Task<ResponseDTO<Room>> RemoveImages(int id, List<string> images);
        Task<ResponseDTO<Room>> AddRoom(RoomForAddDTO roomDto);
        Task<ResponseDTO<Room>> InactiveRoom(int roomId);
        Task<ResponseDTO<IEnumerable<Room>>> GetAllRoomIncludeAreaAndAreaType();
        Task<ResponseDTO<object>> GetAllAreaCategoryInRoom(int id);
    }
}
