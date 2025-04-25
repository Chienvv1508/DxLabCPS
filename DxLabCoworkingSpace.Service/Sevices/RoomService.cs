using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
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
            return await _unitOfWork.RoomRepository.GetWithInclude(expression, x => x.Images, x => x.Areas);
        }
        public async Task<IEnumerable<Room>> GetAll()
        {
            return await _unitOfWork.RoomRepository.GetAllWithInclude(x => x.Images, x => x.Areas);
        }

        public async Task<IEnumerable<Room>> GetAll(Expression<Func<Room, bool>> expression)
        {
            var x = await _unitOfWork.RoomRepository.GetAllWithInclude(x => x.Images, x => x.Areas);
            return x.AsQueryable().Where(expression);
        }

        public Task<IEnumerable<Room>> GetAllWithInclude(params Expression<Func<Room, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task GetAreaDisctinctFaci(Func<Room, bool> value)
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

        public async Task<Room> GetWithInclude(Expression<Func<Room, bool>> expression, params Expression<Func<Room, object>>[] includes)
        {
            return await _unitOfWork.RoomRepository.GetWithInclude(expression, includes);
        }

        public async Task<ResponseDTO<Room>> PatchRoom(int id, JsonPatchDocument<Room> patchDoc)
        {
            Tuple<bool, string, Room> checkInputAndGetRoom = await CheckInputAndGetRoom(id, patchDoc);
            if (checkInputAndGetRoom.Item1 == false)
            {
                return new ResponseDTO<Room>(400, checkInputAndGetRoom.Item2, null);
            }
            var roomFromDb = checkInputAndGetRoom.Item3;

            Tuple<bool, string> updateRoom = await UpdateRoom(patchDoc, roomFromDb);
            if (updateRoom.Item1 == false)
            {
                return new ResponseDTO<Room>(400, updateRoom.Item2, null);
            }

            return new ResponseDTO<Room>(200, updateRoom.Item2, null);

            //if (patchDoc == null)
            //{
            //    var response = new ResponseDTO<object>(400, "Bạn chưa truyền dữ liệu vào", null);
            //    return BadRequest(response);
            //}
            //var roomNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("roomName", StringComparison.OrdinalIgnoreCase));
            //if (roomNameOp != null)
            //{
            //    var existedRoom = await _roomService.Get(x => x.RoomName == roomNameOp.value.ToString() && x.Status != 2);
            //    if (existedRoom != null)
            //    {
            //        var response = new ResponseDTO<object>(400, $"Tên loại phòng {existedRoom} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
            //        return BadRequest(response);
            //    }
            //}

            //var roomFromDb = await _roomService.Get(r => r.RoomId == id && r.Status != 2);
            //if (roomFromDb == null)
            //{
            //    var response = new ResponseDTO<object>(404, $"Không tìm thấy phòng có id {id}!", null);
            //    return NotFound(response);
            //}


            //var allowedPaths = new HashSet<string>
            // {
            //"roomName",
            // "roomDescription"
            //};
            //foreach (var operation in patchDoc.Operations)
            //{
            //    if (!allowedPaths.Contains(operation.path))
            //    {
            //        var response1 = new ResponseDTO<object>(400, $"Không thể cập nhật trường: {operation.path}", null);
            //        return BadRequest(response1);
            //    }
            //}


            //patchDoc.ApplyTo(roomFromDb, ModelState);

            //if (!ModelState.IsValid)
            //{
            //    var allErrors = ModelState
            //    .SelectMany(ms => ms.Value.Errors
            //    .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
            //    .ToList();
            //    string errorString = string.Join(" | ", allErrors);
            //    var response = new ResponseDTO<object>(400, errorString, null);
            //    return BadRequest(response);
            //}

            //var roomDTO = _mapper.Map<RoomDTO>(roomFromDb);

            //bool isValid = TryValidateModel(roomDTO);
            //if (!isValid)
            //{
            //    var allErrors = ModelState
            //    .SelectMany(ms => ms.Value.Errors
            //    .Select(err => $"{ms.Key}: {err.ErrorMessage}"))
            //    .ToList();

            //    string errorString = string.Join(" | ", allErrors);
            //    var response = new ResponseDTO<object>(404, errorString, null);
            //    return BadRequest(response);
            //}
            //await _roomService.Update(roomFromDb);
            //var response2 = new ResponseDTO<object>(200, $"Cập nhập thành công phòng {id}!", null);
            //return Ok(response2);
        }

        private async Task<Tuple<bool, string>> UpdateRoom(JsonPatchDocument<Room> patchDoc, Room roomFromDb)
        {
            try
            {
                if (roomFromDb == null || patchDoc == null)
                {
                    return new Tuple<bool, string>(false, "Không được nhập thông tin cập nhập trống!");
                }
                patchDoc.ApplyTo(roomFromDb);

                IMapper mapper = GenerateMapper.GenerateMapperForService();


                var roomDTO = mapper.Map<RoomDTO>(roomFromDb);

                var isValid = ValidationModel<RoomDTO>.ValidateModel(roomDTO);
                if (isValid.Item1 == false)
                {
                    return new Tuple<bool, string>(false, isValid.Item2);
                }
                await _unitOfWork.RoomRepository.Update(roomFromDb);
                await _unitOfWork.CommitAsync();

                return new Tuple<bool, string>(true, "Cập nhập phòng thành công!");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new Tuple<bool, string>(false, "Cập nhập phòng thất bại!");
            }
        }

        private async Task<Tuple<bool, string, Room>> CheckInputAndGetRoom(int id, JsonPatchDocument<Room> patchDoc)
        {
            try
            {
                if (patchDoc == null)
                {
                    return new Tuple<bool, string, Room>(false, "Bạn chưa truyền dữ liệu vào", null);
                }
                var allowedPaths = new HashSet<string>
                     {
                     "roomName",
                     "roomDescription"
                     };
                foreach (var operation in patchDoc.Operations)
                {
                    if (!allowedPaths.Contains(operation.path))
                    {
                        return new Tuple<bool, string, Room>(false, $"Không thể cập nhật trường: {operation.path}", null);
                    }
                }
                var roomFromDb = await _unitOfWork.RoomRepository.Get(r => r.RoomId == id && r.Status != 2);
                if (roomFromDb == null)
                {
                    return new Tuple<bool, string, Room>(false, $"Không tìm thấy phòng có id {id}!", null);
                }
                var roomNameOp = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("roomName", StringComparison.OrdinalIgnoreCase));
                if (roomNameOp != null)
                {
                    var existedRoom = await _unitOfWork.RoomRepository.Get(x => x.RoomName == roomNameOp.value.ToString() && x.Status != 2);
                    if (existedRoom != null)
                    {
                        return new Tuple<bool, string, Room>(false, $"Tên loại phòng {existedRoom.RoomName} đã tồn tại. Vui lòng nhập tên loại phòng khác!", null);
                    }
                }
                return new Tuple<bool, string, Room>(true, "", roomFromDb);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, Room>(false, "Lỗi cập nhập phòng", null);
            }
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

        public async Task UpdateImage(Room roomFromDb, List<string> images)
        {
            try
            {
                var listImage = await _unitOfWork.ImageRepository.GetAll(x => x.RoomId == roomFromDb.RoomId);
                if (images == null)
                    throw new ArgumentNullException();
                foreach (var item in images)
                {
                    var x = listImage.FirstOrDefault(x => x.ImageUrl == item);
                    if (x == null) throw new Exception("Ảnh nhập vào không phù hợp");
                    await _unitOfWork.ImageRepository.Delete(x.ImageId);

                }
                await _unitOfWork.RoomRepository.Update(roomFromDb);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public async Task<ResponseDTO<Room>> AddImages(int id, List<IFormFile> images)
        {
            try
            {
                var roomFromDb = await _unitOfWork.RoomRepository.Get(x => x.RoomId == id && x.Status != 2);
                if (roomFromDb == null)
                    return new ResponseDTO<Room>(400, "Không tìm thấy phòng này!", null);
                if (images == null)
                    return new ResponseDTO<Room>(400, "Bắt buộc nhập ảnh", null);
                var result = await ImageSerive.AddImage(images);
                if (result.Item1 == true)
                {
                    foreach (var i in result.Item2)
                    {
                        roomFromDb.Images.Add(new Image() { ImageUrl = i });

                    }
                }
                else
                    return new ResponseDTO<Room>(400, "Cập nhập lỗi!", null);

                await _unitOfWork.RoomRepository.Update(roomFromDb);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<Room>(200, "Cập nhập thành công", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<Room>(400, "Cập nhập lỗi!", null);
            }
        }

        public async Task<ResponseDTO<Room>> RemoveImages(int id, List<string> images)
        {
            try
            {
                var roomFromDb = await _unitOfWork.RoomRepository.GetWithInclude(x => x.RoomId == id && x.Status != 2, x => x.Images);
                if (roomFromDb == null)
                    return new ResponseDTO<Room>(400, "Không tìm thấy phòng này!", null);
                if (images == null)
                    return new ResponseDTO<Room>(400, "Bắt buộc nhập ảnh", null);
                var imageList = roomFromDb.Images;
                foreach (var image in images)
                {
                    var item = imageList.FirstOrDefault(x => x.ImageUrl == $"{image}");
                    if (item == null)
                        return new ResponseDTO<Room>(400, "Ảnh không tồn tại trong phòng!", null);
                    roomFromDb.Images.Remove(item);

                }
                await UpdateImage(roomFromDb, images);


                foreach (var image in images)
                {
                    ImageSerive.RemoveImage(image);
                }
                return new ResponseDTO<Room>(200, "Cập nhập thành công!", null);

            }
            catch (Exception ex)
            {
                return new ResponseDTO<Room>(400, "Cập nhập lỗi!", null);
            }
        }
    }
}
