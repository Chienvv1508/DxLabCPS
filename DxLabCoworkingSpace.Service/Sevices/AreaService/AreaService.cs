using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaService : IAreaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AreaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task Add(Area entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Area> Get(Expression<Func<Area, bool>> expression)
        {
            return await _unitOfWork.AreaRepository.Get(expression);
        }

        public async Task<IEnumerable<Area>> GetAll()
        {
            return await _unitOfWork.AreaRepository.GetAll();
        }

        public async Task<IEnumerable<Area>> GetAll(Expression<Func<Area, bool>> expression)
        {
            return await _unitOfWork.AreaRepository.GetAll(expression);
        }

        public async Task<IEnumerable<Area>> GetAllWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes)
        {
            var x = (IQueryable<Area>)(await _unitOfWork.AreaRepository.GetAllWithInclude(includes));
            return x.Where(expression);
        }

        public async Task<Area> GetById(int id)
        {
            return await _unitOfWork.AreaRepository.GetById(id);
        }
        public async Task<IEnumerable<Area>> GetAllWithInclude(params Expression<Func<Area, object>>[] includes)
        {
            return await _unitOfWork.AreaRepository.GetAllWithInclude(includes);
        }
        //public async Task<Area> GetWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<Area> GetWithInclude(Expression<Func<Area, bool>> expression, params Expression<Func<Area, object>>[] includes)
        {
            var x = await _unitOfWork.AreaRepository.GetAllWithInclude(includes);
            return x.FirstOrDefault(expression.Compile());
        }

        public async Task Update(Area entity)
        {
            try
            {
                await _unitOfWork.AreaRepository.Update(entity);
                var areaInRoom = await _unitOfWork.AreaRepository.GetAll(x => x.RoomId == entity.RoomId && x.Status == 1);
                if (areaInRoom != null)
                {
                    if (areaInRoom.Count() == 1 && areaInRoom.FirstOrDefault().AreaId == entity.AreaId)
                    {
                        var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == entity.RoomId && x.Status != 2);
                        room.Status = 0;
                        await _unitOfWork.RoomRepository.Update(room);
                    }
                }

                await _unitOfWork.CommitAsync();

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }
    }
}
