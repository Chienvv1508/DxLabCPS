using AutoMapper;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{


    public class UsingFacilityService : IUsingFacilytyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaciStatusService _facilityStatusService;

        public UsingFacilityService(IUnitOfWork unitOfWork, IFaciStatusService facilityStatusService)
        {
            _unitOfWork = unitOfWork;
            _facilityStatusService = facilityStatusService;
        }

        public async Task Add(UsingFacility entity)
        {
            try
            {
                await _unitOfWork.UsingFacilityRepository.Add(entity);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
            }

        }
        public async Task Add(UsingFacility entity, int status, bool isAvail)
        {
            try
            {
                await _unitOfWork.UsingFacilityRepository.Add(entity);

                var updateFaciStatus = await _facilityStatusService.Get(x => x.FacilityId == entity.FacilityId && x.BatchNumber == entity.BatchNumber
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
                        var room = await  _unitOfWork.RoomRepository.Get(x => x.RoomId == area.RoomId);
                        if(room.Status == 0)
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

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task Delete(IEnumerable<UsingFacility> faciInArea)
        {
            try
            {
                if (faciInArea != null)
                {
                    foreach (var faci in faciInArea)
                    {
                        _unitOfWork.UsingFacilityRepository.Delete(faci);
                        var existedFaciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == faci.FacilityId
                       && x.BatchNumber == faci.BatchNumber && x.ImportDate == faci.ImportDate && x.Status == 1);

                        if (existedFaciStatus != null)
                        {
                            existedFaciStatus.Quantity += faci.Quantity;
                            await _unitOfWork.FacilitiesStatusRepository.Update(existedFaciStatus);
                        }
                        else
                        {
                            var newFaciStatus = new FacilitiesStatus()
                            {
                                FacilityId = faci.FacilityId,
                                BatchNumber = faci.BatchNumber,
                                ImportDate = faci.ImportDate,
                                Quantity = faci.Quantity,
                                Status = 1
                            };
                            await _unitOfWork.FacilitiesStatusRepository.Add(newFaciStatus);
                        }



                    }

                    var areaid = faciInArea.First().AreaId;
                    var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == areaid);
                    if (area != null)
                    {
                        area.Status = 0;
                        await _unitOfWork.AreaRepository.Update(area);
                    }
                   
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

                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
            }
        }

        public async Task<UsingFacility> Get(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.Get(expression);
        }

        public async Task<IEnumerable<UsingFacility>> GetAll()
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll();
        }

        public async Task<IEnumerable<UsingFacility>> GetAll(Expression<Func<UsingFacility, bool>> expression)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAll(expression);
        }

        public async Task<ResponseDTO<List<UsingFacility>>> GetAllBrokenFaciFromReport(BrokernFaciReportDTO removedFaciDTO)
        {
            try
            {
                if (removedFaciDTO.Quantity <= 0)
                {
                    throw new ArgumentException("Số lượng thiết bị cần thay đổi phải  > 0");
                }
                //Check area
                var area = await _unitOfWork.AreaRepository.Get(x => x.AreaId == removedFaciDTO.AreaId && x.Status != 2);
                if(area == null)
                {
                    return new ResponseDTO<List<UsingFacility>>(400, "Không tìm  khu vực", null);
                }
           
                var listFaciInArea = await _unitOfWork.UsingFacilityRepository.GetAllWithInclude( x => x.Area,
                   x => x.Facility);
                listFaciInArea = listFaciInArea.Where(x => x.FacilityId == removedFaciDTO.FacilityId &&
                 x.AreaId == removedFaciDTO.AreaId);

                int quantity = 0;
                foreach (var faci in listFaciInArea)
                {
                    quantity += faci.Quantity;
                }

                if (quantity < removedFaciDTO.Quantity)
                {
                    return new ResponseDTO<List<UsingFacility>>(400, "Số lượng thiết bị cần xóa lớn hơn số lượng hiện có", null);
                }

                listFaciInArea = listFaciInArea.OrderBy(x => x.ImportDate);
                List<UsingFacility> removeUsingFacilityDTOs = new List<UsingFacility>();
                IMapper mapper = GenerateMapper.GenerateMapperForService();
                int removeQuantity = removedFaciDTO.Quantity;
                foreach (var faci in listFaciInArea)
                {
                    if (removeQuantity == 0)
                        break;
                     if(faci.Quantity >= removeQuantity)
                    {
                       faci.Quantity = removeQuantity;
                       removeUsingFacilityDTOs.Add(faci);
                        removeQuantity = 0;
                    }    
                     if(faci.Quantity < removeQuantity)
                    {
                        removeUsingFacilityDTOs.Add(faci);
                        removeQuantity -= faci.Quantity;

                    }
                    
                }
                return new ResponseDTO<List<UsingFacility>>(200, "Danh sách thiết bị cần xóa", removeUsingFacilityDTOs);
            }
            catch(Exception ex)
            {
                return new ResponseDTO<List<UsingFacility>>(500, "Lỗi lấy danh sách thiết bị cần xóa", null);
            }
            }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);
        }

        public async Task<IEnumerable<UsingFacility>> GetAllWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {

            var data = await _unitOfWork.UsingFacilityRepository.GetAllWithInclude(includes);

            var query = data.AsQueryable().Where(expression);

            return query.ToList();
        }

        public Task<UsingFacility> GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UsingFacility> GetWithInclude(Expression<Func<UsingFacility, bool>> expression, params Expression<Func<UsingFacility, object>>[] includes)
        {
            return await _unitOfWork.UsingFacilityRepository.GetWithInclude(expression, includes);
        }

        public async Task Update(UsingFacility entity)
        {
            await _unitOfWork.UsingFacilityRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task Update(RemoveFaciDTO removedFaciDTO)
        {
            try
            {


                if (removedFaciDTO.Quantity <= 0)
                {
                    throw new ArgumentException("Số lượng thiết bị cần thay đổi phải  > 0");
                }

                var usingfaci = await _unitOfWork.UsingFacilityRepository.Get(x => x.FacilityId == removedFaciDTO.FacilityId &&
                x.AreaId == removedFaciDTO.AreaId && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate);
                if(removedFaciDTO.Quantity > usingfaci.Quantity)
                {
                    throw new Exception("Số lượng nhập quá với số lượng hiện tại!");
                }

                if(usingfaci == null)
                {
                    throw new Exception("Không tìm thấy thông tin thiết bị cần xóa!");

                }

                _unitOfWork.UsingFacilityRepository.Delete(usingfaci);

                var existedfaciStatus = await _unitOfWork.FacilitiesStatusRepository.Get(x => x.FacilityId == removedFaciDTO.FacilityId
                       && x.BatchNumber == removedFaciDTO.BatchNumber && x.ImportDate == removedFaciDTO.ImportDate && x.Status == removedFaciDTO.Status);
                if(existedfaciStatus != null)
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

        public async Task Update(UsingFacility existedFaciInArea, int status, bool isAvail)
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
                var updateFaciStatus = await _facilityStatusService.Get(x => x.FacilityId == existedFaciInArea.FacilityId && x.BatchNumber == existedFaciInArea.BatchNumber
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
    }
}
