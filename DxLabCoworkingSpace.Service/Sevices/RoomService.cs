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
        private IMapper _mapper;

        public RoomService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;

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

        public async Task<ResponseDTO<Room>> AddRoom(RoomForAddDTO roomDto)
        {
            try
            {

                Tuple<bool, string> checkValidDTO = await CheckValidRoomForAddDTO(roomDto);
                if (checkValidDTO.Item1 == false)
                {
                    var response1 = new ResponseDTO<Room>(400, checkValidDTO.Item2, null);
                    return response1;
                }


                // Bản chất hàm này đang đếm tổng areasize, lấy individual area
                // item1: tổng size trong các area sau khi add, item2: individual area, item3: check xem có đủ dk qua bước này ko
                Tuple<int, Area, bool, string> resultAreDTO = await CheckCapacityAndExactAreaData(roomDto);
                if (resultAreDTO.Item3 == false)
                {
                    var response1 = new ResponseDTO<Room>(400, resultAreDTO.Item4, null);
                    return response1;
                }
                // Check area name duplicates
                if (checkDuplicateAreaName(roomDto))
                {
                    var response1 = new ResponseDTO<Room>(400, "Tên khu vực đang nhập trùng nhau", null);
                    return response1;
                }
                // Ánh xạ từ RoomDTO sang Room
                var room = _mapper.Map<Room>(roomDto);
                AssignExpiredDateToRoom(room);

                // Thêm position cho khu vực cá nhân
                if (resultAreDTO.Item2 != null)
                {
                    await AddPositionIntoIndividualArea(resultAreDTO.Item2, room);
                }

                var rs = await ImageSerive.AddImage(roomDto.Images);
                if (rs.Item1 == true)
                {
                    foreach (var i in rs.Item2)
                    {
                        room.Images.Add(new Image() { ImageUrl = i });
                    }
                }
                else
                {
                    var response1 = new ResponseDTO<Room>(500, "Lỗi nhập ảnh!", null);
                    return response1;
                }

                room.Status = 0;
                // Lưu room vào database
                await _unitOfWork.RoomRepository.Add(room);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<Room>(200, "", room);

                //// Tải lại room từ database với Areas, Images và AreaType
                //var savedRoom = await _roomService.GetWithInclude(
                //    r => r.RoomId == room.RoomId,
                //    r => r.Areas // Include Areas
                //);

                //if (savedRoom != null)
                //{
                //    foreach (var area in savedRoom.Areas)
                //    {
                //        var areaWithImages = await _areaService.GetWithInclude(
                //            a => a.AreaId == area.AreaId,
                //            a => a.Images
                //        );
                //        area.Images = areaWithImages.Images.ToList();
                //        area.AreaType = await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId);
                //    }
                //}

                //// Ánh xạ sang RoomDTO để trả về
                //var roomDtoRs = _mapper.Map<RoomDTO>(savedRoom);

                //var response = new ResponseDTO<RoomDTO>(201, "Tạo phòng thành công", roomDtoRs);
                //return CreatedAtAction(nameof(GetRoomById), new { id = room.RoomId }, response);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackAsync();
                var response = new ResponseDTO<Room>(500, "Lỗi cập nhật cơ sở dữ liệu.", null);
                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                var response = new ResponseDTO<Room>(500, "Đã xảy ra lỗi khi tạo phòng.", null);
                return response;
            }
        }

        private async Task<Tuple<bool, string>> CheckValidRoomForAddDTO(RoomForAddDTO roomDto)
        {
            var isValid = ValidationModel<RoomForAddDTO>.ValidateModel(roomDto);
            if (isValid.Item1 == false)
            {
                return new Tuple<bool, string>(false, isValid.Item2);
            }
            var existedRoom = await _unitOfWork.RoomRepository.Get(x => x.RoomName == roomDto.RoomName && x.Status != 2);
            if (existedRoom != null)
            {

                return new Tuple<bool, string>(false, "Tên phòng đã tồn tại!");

            }

            return new Tuple<bool, string>(true, "");


        }

        private void AssignExpiredDateToRoom(Room room)
        {
            if (room == null) throw new ArgumentNullException("phòng nhập null");
            foreach (var area in room.Areas)
            {
                area.ExpiredDate = new DateTime(3000, 1, 1);
            }

        }

        private async Task AddPositionIntoIndividualArea(Area area, Room room)
        {
            if (area != null && room != null)
            {
                var xr = room.Areas.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                if (xr != null)
                {
                    int[] position = Enumerable.Range(1, area.AreaType.Size).ToArray();
                    List<Position> positions = new List<Position>();
                    for (int i = 1; i <= position.Length; i++)
                    {
                        var pos = new Position
                        {
                            PositionNumber = i,
                            Status = true
                        };
                        positions.Add(pos);
                    }
                    xr.Positions = positions;
                }
            }
        }

        private bool checkDuplicateAreaName(RoomForAddDTO roomDto)
        {
            if (roomDto == null) return false;
            var areaNameList = new List<string>();
            foreach (var area in roomDto.Area_DTO)
            {
                if (areaNameList.Contains(area.AreaName))
                {
                    return true;
                }
                areaNameList.Add(area.AreaName);
            }
            return false;
        }

        private async Task<Tuple<int, Area, bool, string>> CheckCapacityAndExactAreaData(RoomForAddDTO roomDto)
        {
            if (roomDto == null)
            {
                return new Tuple<int, Area, bool, string>(0, null, false, "Lỗi nhập tham số đầu vào!");
            }
            try
            {
                int areas_totalSize = 0;
                var areaTypeList = await _unitOfWork.AreaTypeRepository.GetAll(x => x.Status == 1);
                Area individualArea = null;
                int inputIndividual = 0;

                foreach (var area in roomDto.Area_DTO)
                {
                    var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
                    if (areatype != null)
                    {
                        areas_totalSize += areatype.Size;
                        if (areatype.AreaCategory == 1)
                        {
                            inputIndividual++;
                            if (inputIndividual > 1)
                            {
                                return new Tuple<int, Area, bool, string>(0, null, false, "Trong phòng chỉ được có 1 phòng cá nhân");
                            }
                            individualArea = _mapper.Map<Area>(area);
                            individualArea.AreaType = areatype;
                        }
                    }
                    else
                    {
                        return new Tuple<int, Area, bool, string>(0, null, false, $"Mã khu vực {area.AreaTypeId} không tồn tại!");
                    }
                }
                if (roomDto.Capacity < areas_totalSize)
                {
                    return new Tuple<int, Area, bool, string>(0, null, false, "Bạn đã nhập khu vực quá sức chứa của phòng");
                }
                return new Tuple<int, Area, bool, string>(areas_totalSize, individualArea, true, "");

            }
            catch (Exception ex)
            {
                return new Tuple<int, Area, bool, string>(0, null, false, "Lỗi lấy dữ liệu khi kiểm tra các khu vực phù hợp");
            }

        }

        public async Task<ResponseDTO<Room>> InactiveRoom(int roomId)
        {
            try
            {
                var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == roomId && x.Status != 2);
                if (room == null)
                {
                    return new ResponseDTO<Room>(400, "Room không tồn tại", null);
                }
                var areaInRoom = await _unitOfWork.AreaRepository.GetAll(x => x.RoomId == roomId && x.ExpiredDate > DateTime.Now.Date);
                if (areaInRoom.Any())
                {
                    return new ResponseDTO<Room>(400, "Trong phòng đang các khu vực đang hoạt động. Nếu muốn xóa bạn phải xóa hết khu vực trong phòng", null);
                }
                room.Status = 2;
                await _unitOfWork.RoomRepository.Update(room);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<Room>(200, "Xóa thành công!", null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ResponseDTO<Room>(500, "Lỗi xóa phòng!", null);
            }
        }

        public async Task<ResponseDTO<IEnumerable<Room>>> GetAllRoomIncludeAreaAndAreaType()
        {
            try
            {
                // Tải tất cả Room với Areas, Images của Areas và AreaType
                var rooms = await _unitOfWork.RoomRepository.GetAllWithInclude(x => x.Images, x => x.Areas);

                foreach (var r in rooms)
                {
                    r.Areas = r.Areas.Where(x => x.ExpiredDate > DateTime.Now.Date).ToList();
                    foreach (var area in r.Areas)
                    {
                        area.AreaType = await _unitOfWork.AreaTypeRepository.GetWithInclude(x => x.AreaTypeId == area.AreaTypeId, x => x.AreaTypeCategory);
                    }
                }
                return new ResponseDTO<IEnumerable<Room>>(200, "Danh sách phòng", rooms);



            }
            catch (Exception ex)
            {
                return new ResponseDTO<IEnumerable<Room>>(500, "Lỗi khi lấy dữ liệu phòng!", null);
            }
        }
    }
}
