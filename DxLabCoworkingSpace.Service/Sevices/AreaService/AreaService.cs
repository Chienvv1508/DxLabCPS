using AutoMapper;
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
        private readonly IMapper _mapper;

        public AreaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = GenerateMapper.GenerateMapperForService();
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

        public async Task<ResponseDTO<object>> AddFaciToArea(int areaid, int status, FaciAddDTO faciAddDTO)
        {
            try
            {
                var isValid =  ValidationModel<FaciAddDTO>.ValidateModel(faciAddDTO);
                if(isValid.Item1 == false)
                {
                   return new ResponseDTO<object>(400, isValid.Item2, null);
                }

                // item 1: check item2: lỗi, item3: areaInroom
                Tuple<bool, string, Area> checkAreaAndStatus = await CheckExistedAreaAndStatus(areaid, status);
                if (checkAreaAndStatus.Item1 == false)
                {
                    return new ResponseDTO<object>(400, checkAreaAndStatus.Item2, null);
                }
                var areaInRoom = checkAreaAndStatus.Item3;

                var fullInfoOfFaci = await _unitOfWork.FacilityRepository.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate);
                if (fullInfoOfFaci == null)
                {
                    return new ResponseDTO<object>(400, "Thông tin thiết bị nhập sai. Vui lòng nhập lại", null);

                }
                // item1: addable, item2: isfull, item3: mã lỗi
                Tuple<bool, bool, string> checkIsAddable = await checkIsAddableAndStatusAfterAdd(fullInfoOfFaci, areaInRoom, faciAddDTO);
                if (checkIsAddable.Item1 == false)
                {
                    return new ResponseDTO<object>(400, checkIsAddable.Item3, null);
                }

                //check lượng đang có trong status

                Tuple<bool, string> checkIsAvailFaciForAdd = await CheckIsAvailFaciForAdd(faciAddDTO, status);
                if (checkIsAvailFaciForAdd.Item1 == false)
                {
                   
                    return new ResponseDTO<object>(400, checkIsAvailFaciForAdd.Item2, null);
                }

                //Thêm đoạn check xem trong phòng có using này chưa. Nếu có thì cộng nếu không thì thêm mới

                var existedFaciInArea = await _unitOfWork.UsingFacilityRepository.Get(x => x.AreaId == areaid && x.BatchNumber == faciAddDTO.BatchNumber

                && x.FacilityId == faciAddDTO.FacilityId && x.ImportDate == faciAddDTO.ImportDate);
                bool statusOfArea = checkIsAddable.Item2;
                if (existedFaciInArea == null)
                {
                    var newUsingFacility = new UsingFacility
                    {
                        AreaId = areaid,
                        BatchNumber = faciAddDTO.BatchNumber,
                        FacilityId = faciAddDTO.FacilityId,

                        Quantity = faciAddDTO.Quantity,
                        ImportDate = faciAddDTO.ImportDate
                    };

                    await Add(newUsingFacility, status, statusOfArea);
                }
                else
                {
                    existedFaciInArea.Quantity += faciAddDTO.Quantity;
                    await Update(existedFaciInArea, status, statusOfArea);
                }

                return new ResponseDTO<object>(200, "Thêm thiết bị thành công", null);
            }
            catch (Exception ex)
            {
               
                return new ResponseDTO<object> (400, "Thêm thiết bị lỗi", null);
            }
        }
        private async Task Update(UsingFacility existedFaciInArea, int status, bool isAvail)
        {
            try
            {
                await _unitOfWork.UsingFacilityRepository.Update(existedFaciInArea);
                var faciInArea = await _unitOfWork.UsingFacilityRepository.Get(x => x.FacilityId == existedFaciInArea.FacilityId &&
                x.BatchNumber == existedFaciInArea.BatchNumber && x.ImportDate == existedFaciInArea.ImportDate
                );
                int removeQuantity = 0;
                if (faciInArea != null)
                {
                    removeQuantity = existedFaciInArea.Quantity - faciInArea.Quantity;
                }
                var updateFaciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == existedFaciInArea.FacilityId && x.BatchNumber == existedFaciInArea.BatchNumber
                    && x.ImportDate == existedFaciInArea.ImportDate && x.Status == status);
                if (updateFaciStatus == null)
                {
                    throw new Exception("Lỗi khi cập nhập trạng thái của thiết bị");
                }
                updateFaciStatus.Quantity -= removeQuantity;
                await _unitOfWork.FacilitiesStatusRepository.Update(updateFaciStatus);
                if (isAvail)
                {
                    var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == existedFaciInArea.AreaId);
                    if (area != null)
                    {
                        area.Status = 1;
                        await _unitOfWork.AreaRepository.Update(area);
                    }
                    var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == area.RoomId);
                    if (room.Status == 0)
                    {
                        room.Status = 1;
                    }
                    await _unitOfWork.RoomRepository.Update(room);
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }

        }

        private async Task Add(UsingFacility entity, int status, bool isAvail)
        {
            try
            {
                await _unitOfWork.UsingFacilityRepository.Add(entity);

                var updateFaciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == entity.FacilityId && x.BatchNumber == entity.BatchNumber
                    && x.ImportDate == entity.ImportDate && x.Status == status);
                if (updateFaciStatus == null)
                {
                    throw new Exception("Lỗi khi cập nhập trạng thái của thiết bị");
                }
                updateFaciStatus.Quantity -= entity.Quantity;
                await _unitOfWork.FacilitiesStatusRepository.Update(updateFaciStatus);
                if (isAvail)
                {
                    var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == entity.AreaId);
                    if (area != null)
                    {
                        area.Status = 1;
                        var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == area.RoomId);
                        if (room.Status == 0)
                        {
                            room.Status = 1;
                        }
                        await _unitOfWork.RoomRepository.Update(room);
                        await _unitOfWork.AreaRepository.Update(area);
                    }
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }

        }

        private async Task<Tuple<bool, string>> CheckIsAvailFaciForAdd(FaciAddDTO faciAddDTO, int status)
        {
            try
            {
                var faciInStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == faciAddDTO.FacilityId && x.BatchNumber == faciAddDTO.BatchNumber
                && x.ImportDate == faciAddDTO.ImportDate && x.Status == status);

                if (faciInStatus == null)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    return new Tuple<bool, string>(false, $"Với trạng thái {tt} hiện không có thiết bị này");
                }
                if (faciInStatus.Quantity < faciAddDTO.Quantity)
                {
                    string tt = status == 0 ? "Mới" : "Đã sử dụng";
                    return new Tuple<bool, string>(false, $"Với trạng thái {tt} hiện không có đủ {faciAddDTO.Quantity} thiết bị");
                }
                return new Tuple<bool, string>(true, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "");
            }
        }

        private async Task<Tuple<bool, bool, string>> checkIsAddableAndStatusAfterAdd(Facility fullInfoOfFaci, Area areaInRoom, FaciAddDTO faciAddDTO)
        {
            try
            {
                var usingFacilities = await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(x => x.Facility);
                usingFacilities = usingFacilities.Where(x => x.AreaId == areaInRoom.AreaId);
                int numberOfPositionT = 0;
                int numberOfPositionCh = 0;  // thay doi
                foreach (var faci in usingFacilities)
                {
                    if (faci.Facility.FacilityCategory == 1)
                    {
                        numberOfPositionT += faci.Facility.Size * faci.Quantity; // sửa
                    }
                    else
                        numberOfPositionCh += faci.Quantity;// sửa
                }
                
                bool isFullT = false;
                bool isFullCh = false;

                if (fullInfoOfFaci.FacilityCategory == 1)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionT < faciAddDTO.Quantity * fullInfoOfFaci.Size)
                    {
                        return new Tuple<bool, bool, string>(false, false, "Bạn đã nhập quá số lượng bàn cho phép của phòng");
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionT == faciAddDTO.Quantity * fullInfoOfFaci.Size)
                        isFullT = true;
                    if (areaInRoom.AreaType.Size == numberOfPositionCh)
                        isFullCh = true;
                }
                // thêm đoạn này
                if (fullInfoOfFaci.FacilityCategory == 0)
                {
                    if (areaInRoom.AreaType.Size - numberOfPositionCh < faciAddDTO.Quantity)
                    {
                        return new Tuple<bool, bool, string>(false, false, "Bạn đã nhập quá số lượng ghế cho phép của phòng");
                    }
                    if (areaInRoom.AreaType.Size - numberOfPositionCh == faciAddDTO.Quantity)
                        isFullCh = true;
                    if (areaInRoom.AreaType.Size == numberOfPositionT)
                        isFullT = true;
                }

                return new Tuple<bool, bool, string>(true, isFullT && isFullCh, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, bool, string>(false, false, "");
            }
        }

        private async Task<Tuple<bool, string, Area>> CheckExistedAreaAndStatus(int areaid, int status)
        {
            try
            {
                var areaInRoom = await _unitOfWork.AreaRepository.GetWithInclude(x => x.AreaId == areaid && x.ExpiredDate.Date > DateTime.Now.Date, x => x.AreaType);
                if (areaInRoom == null)
                {
                    return new Tuple<bool, string, Area>(false, "Không tìm thấy khu vực. Vui lòng nhập lại khu vực", null);
                }
                if (areaInRoom.Status == 1)
                {
                    return new Tuple<bool, string, Area>(false, "Khu vực đã đầy thiết bị. Không thêm được vào phòng!", null);
                }
                //0-mơi 1-dasudung 2--hong
                if (status < 0 && status > 1)
                {
                    return new Tuple<bool, string, Area>(false, "Thiết bị được nhập phải mới hoặc vẫn sử dụng được", null);
                }
                return new Tuple<bool, string, Area>(true, "", areaInRoom);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, Area>(false, ex.Message, null);
            }

        }

        public async Task<ResponseDTO<object>> RemoveFaciFromArea(RemoveFaciDTO removedFaciDTO)
        {
            try
            {
                var isValid = ValidationModel<RemoveFaciDTO>.ValidateModel(removedFaciDTO);
                if(isValid.Item1 == false)
                {
                    return new ResponseDTO<object>(400, isValid.Item2, null);
                }
                var existedFaciInArea = await _unitOfWork.UsingFacilityRepository.Get(x => x.FacilityId == removedFaciDTO.FacilityId &&
                 x.AreaId == removedFaciDTO.AreaId && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate
                 );
                if (existedFaciInArea == null)
                {
                   
                    return new ResponseDTO<object>(400, "Không thấy thiết bị hoặc phòng tương ứng. Vui lòng nhập lại", null);
                }

                await Update(removedFaciDTO);

                return new ResponseDTO<object>(200, "Cập nhập thành công", null);

            }
            catch (Exception ex)
            {
                return  new ResponseDTO<object>(500, "Lỗi cơ sở dữ liệu", null);
            }
        }
        private async Task Update(RemoveFaciDTO removedFaciDTO)
        {
            try
            {


                if (removedFaciDTO.Quantity <= 0)
                {
                    throw new ArgumentException("Số lượng thiết bị cần thay đổi phải  > 0");
                }
                
                var usingfaci = await _unitOfWork.UsingFacilityRepository.Get(x => x.FacilityId == removedFaciDTO.FacilityId &&
                x.AreaId == removedFaciDTO.AreaId && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate);
                if (usingfaci == null)
                {
                    throw new Exception("Không tìm thấy thông tin thiết bị cần xóa!");

                }


                if (removedFaciDTO.Quantity > usingfaci.Quantity)
                {
                    throw new Exception("Số lượng nhập quá với số lượng hiện tại!");
                }

              
                if (usingfaci.Quantity == removedFaciDTO.Quantity)
                {
                    _unitOfWork.UsingFacilityRepository.Delete(usingfaci);
                }
                else
                {
                    usingfaci.Quantity -= removedFaciDTO.Quantity;
                    _unitOfWork.UsingFacilityRepository.Update(usingfaci);
                }


                var existedfaciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == removedFaciDTO.FacilityId
                       && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate && x.Status == removedFaciDTO.Status);
                if (existedfaciStatus != null)
                {
                    existedfaciStatus.Quantity += removedFaciDTO.Quantity;
                    await _unitOfWork.FacilitiesStatusRepository.Update(existedfaciStatus);
                }
                else
                {
                    var newFaciStatus = new FacilitiesStatus()
                    {
                        FacilityId = removedFaciDTO.FacilityId,
                        BatchNumber = removedFaciDTO.BatchNumber,
                        ImportDate = removedFaciDTO.ImportDate,
                        Quantity = removedFaciDTO.Quantity,
                        Status = removedFaciDTO.Status
                    };
                    await _unitOfWork.FacilitiesStatusRepository.Add(newFaciStatus);
                }

                var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == removedFaciDTO.AreaId);
                area.Status = 0;
                var availAreaInRoom = await _unitOfWork.AreaRepository.GetAll(x => x.RoomId == area.RoomId && x.Status == 1);
                if (availAreaInRoom.Any())
                {
                    if (availAreaInRoom.Count() == 1 && availAreaInRoom.First().AreaId == area.AreaId)
                    {
                        var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == area.RoomId);
                        room.Status = 0;
                        await _unitOfWork.RoomRepository.Update(room);
                    }
                }

                //    await _unitOfWork.AreaRepository.Update(area);



                await _unitOfWork.CommitAsync();
                //var listFaciInArea = await _unitOfWork.UsingFacilityRepository.GetAll(x => x.FacilityId == removedFaciDTO.FacilityId &&
                // x.AreaId == removedFaciDTO.AreaId && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate);

                //int quantity = 0;
                //foreach (var faci in listFaciInArea)
                //{
                //    quantity += faci.Quantity;
                //}

                //if (quantity < removedFaciDTO.Quantity )
                //{
                //    throw new ArgumentException("Số lượng cần thay đổi lớn hơn số lượng thiết bị hiện có!");
                //}

                //listFaciInArea = listFaciInArea.OrderBy(x => x.ImportDate);
                //List<FacilitiesStatus> newFaciStatusList = new List<FacilitiesStatus>();
                //FacilitiesStatus faciStatus = null;
                //foreach (var item in listFaciInArea)
                //{
                //    if (removedFaciDTO.Quantity == 0)
                //    {
                //        break;
                //    }

                //    if (item.Quantity <= removedFaciDTO.Quantity)
                //    {
                //        _unitOfWork.UsingFacilityRepository.Delete(item);

                //        if (faciStatus == null)
                //        {
                //            faciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == item.FacilityId
                //         && x.BatchNumber == item.BatchNumber && x.ImportDate == item.ImportDate && x.Status == removedFaciDTO.Status);
                //        }

                //        if (faciStatus != null)
                //        {
                //            faciStatus.Quantity += item.Quantity;

                //            await _unitOfWork.FacilitiesStatusRepository.Update(faciStatus);
                //        }
                //        else
                //        {
                //            var newFaciStatus = new FacilitiesStatus()
                //            {
                //                FacilityId = item.FacilityId,
                //                BatchNumber = item.BatchNumber,
                //                ImportDate = item.ImportDate,
                //                Quantity = item.Quantity,
                //                Status = removedFaciDTO.Status
                //            };
                //            newFaciStatusList.Add(newFaciStatus);
                //        }
                //        removedFaciDTO.Quantity -= item.Quantity;

                //    }
                //    else
                //    {
                //        item.Quantity -= removedFaciDTO.Quantity;
                //        await _unitOfWork.UsingFacilityRepository.Update(item);
                //        if (faciStatus == null)
                //        {
                //            faciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == item.FacilityId
                //         && x.BatchNumber == item.BatchNumber && x.ImportDate == item.ImportDate && x.Status == removedFaciDTO.Status);
                //        }
                //        if (faciStatus != null)
                //        {
                //            faciStatus.Quantity += removedFaciDTO.Quantity;

                //            await _unitOfWork.FacilitiesStatusRepository.Update(faciStatus);
                //        }
                //        else
                //        {
                //            var newFaciStatus = new FacilitiesStatus()
                //            {
                //                FacilityId = item.FacilityId,
                //                BatchNumber = item.BatchNumber,
                //                ImportDate = item.ImportDate,
                //                Quantity = removedFaciDTO.Quantity,
                //                Status = removedFaciDTO.Status
                //            };
                //            newFaciStatusList.Add(newFaciStatus);
                //        }

                //        removedFaciDTO.Quantity = 0;


                //    }
                //}

                //foreach (var newStatus in newFaciStatusList)
                //{
                //    await _unitOfWork.FacilitiesStatusRepository.Add(newStatus);
                //}


                //var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == removedFaciDTO.AreaId);
                //if (area != null)
                //{
                //    area.Status = 0;

                //    var availAreaInRoom =  await _unitOfWork.AreaRepository.GetAll(x => x.RoomId == area.RoomId && x.Status == 1);
                //    if (availAreaInRoom.Any())
                //    {
                //        if(availAreaInRoom.Count() == 1 && availAreaInRoom.First().AreaId == area.AreaId)
                //        {
                //            var room = await _unitOfWork.RoomRepository.Get(x => x.RoomId == area.RoomId);
                //            room.Status = 0;
                //           await _unitOfWork.RoomRepository.Update(room);
                //        } 
                //    }

                //    await _unitOfWork.AreaRepository.Update(area);
                //}


                //await _unitOfWork.CommitAsync();


            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw ex;
            }
        }

        public async Task<ResponseDTO<Area>> AddNewArea(int roomId, List<AreaAdd> areaAdds)
        {
            try
            {

                var room = await _unitOfWork.RoomRepository.GetWithInclude(x => x.RoomId == roomId && x.Status != 2, x => x.Areas, x => x.Images);
                if (room == null)
                {
                    return new ResponseDTO<Area>(400, "Lỗi phòng không tồn tại!", null);
                }

                var areaTypeList = await _unitOfWork.AreaTypeRepository.GetAll(x => x.Status == 1);

                Tuple<bool, int, Area, string> totalSizeAndIndividualArea = GetToTalSizeAndIndividual(room, areaTypeList);
                if (totalSizeAndIndividualArea.Item1 == false)
                {

                    return new ResponseDTO<Area>(400, totalSizeAndIndividualArea.Item4, null);
                }

                int totalSize = totalSizeAndIndividualArea.Item2;
                Area individualArea = totalSizeAndIndividualArea.Item3 as Area;

                if (totalSize == room.Capacity)
                {

                    return new ResponseDTO<Area>(400, "Phòng đã đầy sức chứa. Không thể thêm khu vực", null);
                }


                Tuple<bool, List<AreaType>, int, string> checkValidAreaInput = CheckValidAreaInput(areaAdds, areaTypeList);
                if (checkValidAreaInput.Item1 == false)
                {

                    return new ResponseDTO<Area>(400, checkValidAreaInput.Item4, null);
                }
                List<AreaType> areaTypeInputs = checkValidAreaInput.Item2;
                totalSize += checkValidAreaInput.Item3;
                if (totalSize > room.Capacity)
                {
                    return new ResponseDTO<Area>(400, "Bạn đã nhập quá sức chứa của phòng", null);
                }

                if (areaTypeInputs.FirstOrDefault(x => x.AreaCategory == 1) != null && individualArea != null)
                {
                    return new ResponseDTO<Area>(400, "Trong phòng đã có loại cá nhân không thêm được loại cá nhân nữa!", null);

                }

                Tuple<bool, string> checkDuplicateNameOfArea = CheckDuplicateNameOfArea(room, areaAdds);
                if (checkDuplicateNameOfArea.Item1 == false)
                {

                    return new ResponseDTO<Area>(400, checkDuplicateNameOfArea.Item2, null);
                }

                Tuple<bool, string> addArea = AddArea(room, areaAdds, individualArea);
                if (addArea.Item1 == false)
                {

                    return new ResponseDTO<Area>(400, addArea.Item2, null);

                }
                await _unitOfWork.RoomRepository.Update(room);
                await _unitOfWork.CommitAsync();
                return new ResponseDTO<Area>(200, "Thêm thành công khu vực!", null);
            }               
            
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return  new ResponseDTO<Area>(500, "Lỗi thêm khu vực!", null);
            }
        }

        private Tuple<bool, string> AddArea(Room room, List<AreaAdd> areaAdds, Area individualArea)
        {
            try
            {
                if (room == null && areaAdds == null)
                    return new Tuple<bool, string>(false, "Lỗi nhập dữ liệu đầu vào!");
                var areas = _mapper.Map<List<Area>>(areaAdds);
                foreach (var area in areas)
                {
                    area.ExpiredDate = new DateTime(3000, 1, 1);
                    room.Areas.Add(area);
                }

                if (individualArea != null)
                {
                    var xr = room.Areas.FirstOrDefault(x => x.AreaTypeId == individualArea.AreaTypeId && x.ExpiredDate.Date > DateTime.Now.Date);
                    if (xr != null)
                    {
                        int[] position = Enumerable.Range(1, individualArea.AreaType.Size).ToArray();
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
                return new Tuple<bool, string>(true, "");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "Lỗi thêm khu vực vào phòng!");
            }

        }

        private Tuple<bool, string> CheckDuplicateNameOfArea(Room room, List<AreaAdd> areaAdds)
        {
            try
            {
                if (room == null && areaAdds == null)
                    return new Tuple<bool, string>(false, "Lỗi nhập dữ liệu đầu vào!");
                var areaNameList = room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date).Select(x => x.AreaName).ToList();
                foreach (var area in areaAdds)
                {
                    if (areaNameList.Contains(area.AreaName))
                    {

                        return new Tuple<bool, string>(false, "Tên khu vực đang nhập trùng nhau hoặc đã tồn tại trong database");
                    }
                    areaNameList.Add(area.AreaName);
                }
                return new Tuple<bool, string>(true, "");

            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, "Lỗi kiểm tra trùng tên của dữ liệu nhập vào");
            }
        }

        private Tuple<bool, List<AreaType>, int, string> CheckValidAreaInput(List<AreaAdd> areaAdds, IEnumerable<AreaType> areaTypeList)
        {
            try
            {
                if (areaAdds == null || areaTypeList == null)
                {
                    return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Lỗi nhập thông tin thêm khu vực!");
                }
                int countIndividual = 0;
                int size = 0;
                List<AreaType> areaTypesInput = new List<AreaType>();
                foreach (var area in areaAdds)
                {
                    var newArea = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId && x.Status == 1);

                    if (newArea == null)
                    {
                        return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Nhập sai id của loại khu vực!");
                    }
                    size += newArea.Size;
                    if (newArea.AreaCategory == 1) countIndividual++;
                    if (countIndividual > 1)
                        return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "Chỉ được nhập 1 loại khu vực cá nhân!");
                    areaTypesInput.Add(newArea);
                }
                return new Tuple<bool, List<AreaType>, int, string>(true, areaTypesInput, size, "");

            }
            catch (Exception ex)
            {
                return new Tuple<bool, List<AreaType>, int, string>(false, null, 0, "");
            }
        }

        private Tuple<bool, int, Area, string> GetToTalSizeAndIndividual(Room room, IEnumerable<AreaType> areaTypeList)
        {
            try
            {
                if (room != null && areaTypeList != null)
                {
                    int areas_totalSize = 0;
                    Area individualArea = null;
                    //int countIndividualAre = 0;


                    foreach (var area in room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date))
                    {
                        var areatype = areaTypeList.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId && x.Status == 1);
                        if (areatype != null)
                        {
                            areas_totalSize += areatype.Size;
                            if (areatype.AreaCategory == 1)
                            {
                                //countIndividualAre++;
                                individualArea = area;
                                individualArea.AreaType = areatype;
                            }
                            area.AreaType = areatype;
                        }

                    }
                    return new Tuple<bool, int, Area, string>(true, areas_totalSize, individualArea, "");
                }
                else
                    return new Tuple<bool, int, Area, string>(false, 0, null, "khu vực và loại khu vực không để bỏ trống");

            }
            catch (
            Exception ex)
            {
                return new Tuple<bool, int, Area, string>(false, 0, null, "Lỗi không thêm được khu vực vào phòng!");


            }
        }

        public async Task<ResponseDTO<Area>> SetExpiredTimeToArea(int areaId)
        {
            try
            {


                var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == areaid && x.Status != 2);
                if (area == null)
                {
                    return new ResponseDTO<Area>(400, "Khu vực không tồn tại", null);
                }

                //if(expiredDate <= DateTime.Now.Date.AddDays(14))
                //    return BadRequest(new ResponseDTO<object>(400, "Phải để ngày hết hạn lớn hơn 14 ngày từ ngày hiện tại!", null));


                DateTime lastDateBookingInArea = await GetLastDateBookingInArea(area);
                area.ExpiredDate = lastDateBookingInArea;
                await _unitOfWork.AreaRepository.Update(area);
                await _unitOfWork.CommitAsync();

                return new ResponseDTO<Area>(200, $"Đặt ngày hết hạn của {area.AreaName} vào ngày: {area.ExpiredDate}", null);
            }
            catch (Exception ex)
            {
                return new ResponseDTO<Area>(500, "Lỗi xóa khu vực!", null);
            }
        }







        public async Task<ResponseDTO<Area>> RemoveArea(int areaid)
        {
            try
            {
                

                var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == areaid && x.Status  != 2);
                if (area == null)
                {
                    return new ResponseDTO<Area>(400, "Khu vực không tồn tại", null);
                }

              
                    if(DateTime.Now.Date < area.ExpiredDate.Date)
                    {
                        return new ResponseDTO<Area>(400, "Khu vực chưa hết hạn không xóa được!", null);
                    }
                    var faciInArea = await _unitOfWork.UsingFacilityRepository.GetAll(x => x.AreaId == areaid);
                    if (faciInArea.Any())
                    {
                        return new ResponseDTO<Area>(400, "Trong phòng đang có thiết bị. Nếu muốn xóa bạn phải xóa hết thiết vị trong phòng", null);
                    }
                    area.Status = 2;
                    await _unitOfWork.AreaRepository.Update(area);
                    await _unitOfWork.CommitAsync();
                    return new ResponseDTO<Area>(200, $"Xóa thành công!", null);

                


                //if(expiredDate <= DateTime.Now.Date.AddDays(14))
                //    return BadRequest(new ResponseDTO<object>(400, "Phải để ngày hết hạn lớn hơn 14 ngày từ ngày hiện tại!", null));

               
            }
            catch (Exception ex)
            {
                return new ResponseDTO<Area>(500, "Lỗi xóa khu vực!", null);
            }
        }

        private async Task<DateTime> GetLastDateBookingInArea(Area area)
        {
            if (area == null) return new DateTime(3000, 1, 1);
            var bookingDetails = await _unitOfWork.BookingDetailRepository.GetAll(x => x.AreaId == area.AreaId && x.CheckinTime >= DateTime.Now.Date);

            var lastBooking = bookingDetails.OrderByDescending(x => x.CheckinTime).FirstOrDefault();
            DateTime expiredDate;
            if (lastBooking == null)
            {
                expiredDate = DateTime.Now.Date;
            }
            else
            {
                expiredDate = lastBooking.CheckinTime.Date.AddDays(1);
            }

            return expiredDate;

        }

        public async Task<ResponseDTO<object>> GetAreasManagementInRoom(int roomId)
        {
            try
            {
                var room = await _unitOfWork.RoomRepository.GetWithInclude(x => x.RoomId == roomId && x.Status != 2, x => x.Areas, x => x.Images);
                if (room == null)
                {
                    return new ResponseDTO<object>(400, "Bạn nhập phòng không tồn tại", null);

                }
                var listAreaType = await _unitOfWork.AreaTypeRepository.GetAll(x => x.Status == 1);
                var listAreaTypeCategory = await _unitOfWork.AreaTypeCategoryRepository.GetAll(x => x.Status == 1);
                List<AreaGetForManagement> areaDTOs = new List<AreaGetForManagement>();
                IEnumerable<UsingFacility> usingFacilities = await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(x => x.Facility);

                foreach (var are in room.Areas.Where(x => x.Status != 2))
                {
                    IEnumerable<UsingFacility> usingFacilities1 = new List<UsingFacility>();
                    if (usingFacilities != null)
                    {
                        usingFacilities1 = usingFacilities.AsQueryable().Where(x => x.AreaId == are.AreaId);
                    }
                    var areaType = listAreaType.FirstOrDefault(x => x.AreaTypeId == are.AreaTypeId);
                    are.AreaType = areaType;
                    var areaTypeCategory = listAreaTypeCategory.FirstOrDefault(x => x.CategoryId == are.AreaType.AreaCategory);
                    are.AreaType.AreaTypeCategory = areaTypeCategory;
                    //if (!usingFacilities1.Any())
                    //{
                    //    int faci = 0;
                    //    int faciCh = 0;
                    //    AreaGetForManagement areaGetForManagement = new AreaGetForManagement()
                    //    {
                    //        AreaId = are.AreaId,
                    //        AreaName = are.AreaName,
                    //        AreaTypeId = are.AreaTypeId,
                    //        AreaTypeName = are.AreaType.AreaTypeName,
                    //        CategoryId = are.AreaType.AreaCategory,
                    //        Title = are.AreaType.AreaTypeCategory.Title,
                    //        FaciAmount = faci,
                    //        FaciAmountCh = faciCh,
                    //        Status = are.Status,
                    //        Size = are.AreaType.Size


                    //    };
                    //    areaDTOs.Add(areaGetForManagement);
                    //}
                    //else
                    //{
                    int faci = 0;
                    int faciCh = 0;
                    foreach (var facility in usingFacilities1)
                    {
                        if (facility.Facility.FacilityCategory == 1)
                            faci += facility.Facility.Size * facility.Quantity;
                        else
                            faciCh += facility.Quantity;
                    }
                    AreaGetForManagement areaGetForManagement = new AreaGetForManagement()
                    {
                        AreaId = are.AreaId,
                        AreaName = are.AreaName,
                        AreaTypeId = are.AreaTypeId,
                        AreaTypeName = are.AreaType.AreaTypeName,
                        CategoryId = are.AreaType.AreaCategory,
                        Title = are.AreaType.AreaTypeCategory.Title,
                        FaciAmount = faci,
                        FaciAmountCh = faciCh,
                        Status = are.Status,
                        Size = are.AreaType.Size,
                        ExpiredDate = are.ExpiredDate



                    };
                    areaDTOs.Add(areaGetForManagement);
                    //}
                }


                
                return new ResponseDTO<object>(200, "Danh sách khu vực", areaDTOs);
            }
            catch (Exception ex)
            {
                
                return new ResponseDTO<object>(500, "Lỗi lấy danh sách khu vực!", null);
            }
        }

       
    }
}
