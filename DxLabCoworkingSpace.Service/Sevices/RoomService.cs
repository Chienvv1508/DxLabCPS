using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace { 
    public class RoomService : IRoomService
{
    private IUnitOfWork _unitOfWork;

    public RoomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Add(Room entity)
    {
        await _unitOfWork.RoomRepository.Add(entity);
         await _unitOfWork.CommitAsync();
        
    }

    public Task Delete(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<Room> Get(Expression<Func<Room, bool>> expression)
    {
      return  await _unitOfWork.RoomRepository.GetWithInclude(expression, x => x.Images,x => x.Areas);
    }
    public async Task<IEnumerable<Room>> GetAll()
    {
            return await _unitOfWork.RoomRepository.GetAllWithInclude(x => x.Images , x => x.Areas);
    }

    public async Task<IEnumerable<Room>> GetAll(Expression<Func<Room, bool>> expression)
    {
            return await _unitOfWork.RoomRepository.GetAll(expression);
    }

     public Task<IEnumerable<Room>> GetAllWithInclude(params Expression<Func<Room, object>>[] includes)
     {
            throw new NotImplementedException();
     }

    public Task<Room> GetById(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<Room> GetRoomWithAllInClude(Expression<Func<Room, bool>> expression)
     {
         return await _unitOfWork.RoomRepository.GetWithInclude(expression, x => x.Images, x => x.Areas);
     }

     public async Task<Room> GetRoomWithAraeAnAreaType(Expression<Func<Room, bool>> expression)
      {
            var rooms = await _unitOfWork.RoomRepository.GetAll();
            var fRooms = (IQueryable<Room>)rooms;
            var result = fRooms.Include(x => x.Areas).ThenInclude(y => y.AreaType);
            if (expression != null)
            {
                return result.FirstOrDefault(expression);
            }
            return null;
           
            
        }

        public Task<Room> GetWithInclude(Expression<Func<Room, bool>> expression, params Expression<Func<Room, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> PatchRoomAsync(int id, JsonPatchDocument<Room> patchDoc)
     {
            var room = await _unitOfWork.RoomRepository.GetById(id);
            if (room == null) return false;
            patchDoc.ApplyTo(room);
            await _unitOfWork.CommitAsync();
            return true;
     }

        public async Task Update(Room entity)
    {
             await _unitOfWork.RoomRepository.Update(entity);
            await _unitOfWork.CommitAsync();

    }
}
}
