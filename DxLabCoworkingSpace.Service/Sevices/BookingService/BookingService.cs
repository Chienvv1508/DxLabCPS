using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class BookingService : IBookingService
    {
        private IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Booking entity)
        {
            await _unitOfWork.BookingRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task<ResponseDTO<object>> CreateBooking(BookingDTO bookingDTO)
        {
            return new ResponseDTO<object>(200, "", null);

            //try
            //{
            //    var isValid = ValidationModel<BookingDTO>.ValidateModel(bookingDTO);
            //    if(isValid.Item1 == false)
            //    {
            //        return new ResponseDTO<object>(400, isValid.Item2, null);
            //    }

            //    Tuple<bool, string> checkBookingTime = await CheckBookingTime(bookingDTO.bookingTimes);

            //   if(checkBookingTime.Item1 == false)
            //    {
            //        return new ResponseDTO<object>(400, checkBookingTime.Item2, null);
            //    }

            //    //Get Information
            //    var roomId = bookingDTO.RoomId;
            //    var areaTypeId = bookingDTO.AreaTypeId;
            //    var bookingDates = bookingDTO.bookingTimes;

            //    //Check RoomId
            //    Room room = await _roomService.GetRoomWithAllInClude(x => x.RoomId == bookingDTO.RoomId && x.Status == 1);
            //    if (room == null)
            //    {
            //        var reponse = new ResponseDTO<object>(400, "Không tìm thấy phòng cần đặt!", null);
            //        return BadRequest(reponse);
            //    }
            //    //Check AraeTypeId
            //    var areasInRoom = room.Areas.Where(x => x.AreaTypeId == areaTypeId && x.Status == 1 && x.ExpiredDate.Date > DateTime.Now.Date);
            //    if (!areasInRoom.Any())
            //    {
            //        var reponse = new ResponseDTO<object>(400, "Trong phòng không có khu vực nào có loại khu vực như đã nhập!", null);
            //        return BadRequest(reponse);
            //    }
            //    //Check Ngày
              
            //    List<Area> newAreaInRoom = new List<Area>();
            //    //Get All Information
            //    foreach (var ar in areasInRoom)
            //    {
            //        var x = await _areaService.GetWithInclude(x => x.AreaId == ar.AreaId, x => x.AreaType, x => x.Positions);
            //        newAreaInRoom.Add(x);
            //    }
            //    areasInRoom = newAreaInRoom;

            //    // Lấy UserId từ token
            //    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            //    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            //    {
            //        return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
            //    }

            //    // Lấy WalletAddress của user
            //    var user = await _userService.Get(u => u.UserId == userId);
            //    if (user == null || string.IsNullOrEmpty(user.WalletAddress))
            //    {
            //        return BadRequest(new ResponseDTO<object>(400, "Người dùng không có địa chỉ ví blockchain!", null));
            //    }
            //    var userWalletAddress = user.WalletAddress;

            //    Booking booking = new Booking();
            //    List<BookingDetail> bookingDetails = new List<BookingDetail>();

            //    // Load dữ liệu cần thiết một lần trước vòng lặp
            //    var allSlots = await _slotService.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
            //    var allAreasWithDetails = await _areaService.GetAllWithInclude(a => a.Room, a => a.AreaType, a => a.Positions);
            //    var filteredAreas = allAreasWithDetails.Where(a => areasInRoom.Select(ar => ar.AreaId).Contains(a.AreaId)).ToList();
            //    var areaTypeIds = filteredAreas.Select(a => a.AreaTypeId).Distinct().ToList();
            //    var areaTypes = await _areaTypeService.GetAll(at => areaTypeIds.Contains(at.AreaTypeId));

            //    // Tạo lookup để tra cứu nhanh
            //    var areaLookup = filteredAreas.ToDictionary(a => a.AreaId, a => a);
            //    var areaTypeLookup = areaTypes.ToDictionary(at => at.AreaTypeId, at => at);

            //    foreach (var dte in bookingDates)
            //    {
            //        booking.UserId = userId;
            //        booking.BookingCreatedDate = DateTime.Now;

            //        // Tạo ma trận
            //        Dictionary<int, int[]> searchMatrix = await CreateSearchMatrix(areasInRoom, dte.BookingDate.Date);
            //        if (searchMatrix.ContainsKey(-1))
            //        {
            //            return BadRequest(new ResponseDTO<object>(400, "Đã có phòng bị hết hạn bạn không thể đặt được", null));
            //        }
            //        int[] slotArray = new int[dte.SlotId.Count];
            //        for (int i = 0; i < slotArray.Length; i++)
            //        {
            //            var x = await _slotService.Get(x => x.SlotId == dte.SlotId[i] && x.ExpiredTime.Date > DateTime.Now.Date);
            //            if (x == null)
            //                return BadRequest(new ResponseDTO<object>(400, "Slot không có hoặc đã bị xóa", null));
            //            slotArray[i] = x.SlotNumber;
            //        }
            //        int[][] slotJaggedMatrix = CreateSlotJaggedMatrix(slotArray);
            //        Tuple<bool, string, List<KeyValuePair<int, int[]>>> findPositionResult = findPosition(slotJaggedMatrix, searchMatrix);
            //        if (findPositionResult.Item1 == false)
            //        {
            //            var reponse = new ResponseDTO<object>(400, findPositionResult.Item2, null);
            //            return BadRequest(reponse);
            //        }
            //        else
            //        {
            //            var allSlot = await _slotService.GetAll();
            //            if (areasInRoom.First().AreaType.AreaCategory == 1)
            //            {
            //                foreach (var item in findPositionResult.Item3)
            //                {
            //                    for (int i = 0; i < item.Value.Length; i++)
            //                    {
            //                        var bookingDetail = new BookingDetail();
            //                        int id = item.Key;

            //                        bookingDetail.PositionId = id;
            //                        var slot = allSlot.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
            //                        bookingDetail.SlotId = slot.SlotId;
            //                        bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
            //                        if (i == item.Value.Length - 1)
            //                        {
            //                            bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
            //                        }
            //                        else
            //                            //bookingDetail.CheckoutTime = null;

            //                            bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
            //                        var areaBooks = await _areaService.GetAllWithInclude(x => x.AreaType, x => x.Positions);
            //                        var areaBook = areaBooks.FirstOrDefault(x => x.Positions.FirstOrDefault(x => x.PositionId == id) != null);
            //                        bookingDetail.Price = areaBook.AreaType.Price;
            //                        bookingDetail.AreaId = areaBook.AreaId;
            //                        bookingDetails.Add(bookingDetail);
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                foreach (var item in findPositionResult.Item3)
            //                {
            //                    for (int i = 0; i < item.Value.Length; i++)
            //                    {
            //                        var bookingDetail = new BookingDetail();
            //                        int id = item.Key;

            //                        bookingDetail.AreaId = id;

            //                        var slot = allSlot.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
            //                        bookingDetail.SlotId = slot.SlotId;
            //                        bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
            //                        if (i == item.Value.Length - 1)
            //                        {
            //                            bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
            //                        }
            //                        else
            //                            //bookingDetail.CheckoutTime = null;

            //                            bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
            //                        var areaBooks = await _areaService.GetAllWithInclude(x => x.AreaType);
            //                        var areaBook = areaBooks.FirstOrDefault(x => x.AreaId == id);
            //                        bookingDetail.Price = areaBook.AreaType.Price;
            //                        bookingDetails.Add(bookingDetail);
            //                    }
            //                }
            //            }
            //        }
            //    }

            //    // Tính TotalPrice và lưu Booking
            //    booking.Price = bookingDetails.Sum(br => br.Price);
            //    booking.BookingDetails = bookingDetails;

            //    //// Lưu booking vào database trước để nhận BookingId hợp lệ
            //    await _bookingService.Add(booking);
            //    //await _unitOfWork.CommitAsync(); // Gọi CommitAsync ở đây là cần thiết

            //    //// Lấy slot đầu tiên để gọi smart contract
            //    //var firstSlot = bookingDetails.FirstOrDefault()?.SlotId;
            //    //if (firstSlot == null)
            //    //{
            //    //    // Xóa booking nếu không tìm thấy slot
            //    //    await _bookingService.Remove(booking); // Đã bao gồm CommitAsync bên trong
            //    //    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy slot để đặt phòng!", null));
            //    //}

            //    //// Gọi blockchain để trừ tiền và phát sự kiện BookingCreated
            //    //var (success, txHash) = await _blockchainBookingService.BookOnBlockchain(booking.BookingId, userWalletAddress, (byte)firstSlot, booking.Price);
            //    //if (!success)
            //    //{
            //    //    // Nếu giao dịch blockchain thất bại, xóa booking vừa tạo
            //    //    await _bookingService.Remove(booking); // Đã bao gồm CommitAsync bên trong

            //    //    // Kiểm tra nguyên nhân cụ thể
            //    //    var userBalance = await _blockchainBookingService.GetUserBalance(userWalletAddress);
            //    //    var requiredTokens = Nethereum.Util.UnitConversion.Convert.ToWei(booking.Price);
            //    //    if (userBalance < requiredTokens)
            //    //    {
            //    //        return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! Số dư token không đủ.", null));
            //    //    }
            //    //    if (booking.BookingId <= 0)
            //    //    {
            //    //        return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! BookingId không hợp lệ.", null));
            //    //    }
            //    //    return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! Giao dịch không thành công.", null));
            //    //}

            //    // Chuẩn bị dữ liệu trả về giống GetStudentBookingHistoryDetail
            //    var responseData = new
            //    {
            //        BookingId = booking.BookingId,
            //        BookingCreatedDate = booking.BookingCreatedDate,
            //        TotalPrice = booking.Price,
            //        Details = bookingDetails.Select(bd =>
            //        {
            //            string positionDisplay = null;
            //            string areaName = null;
            //            string areaTypeName = null;
            //            string roomName = null;

            //            if (bd.AreaId.HasValue && areaLookup.TryGetValue(bd.AreaId.Value, out var areaFromAreaId))
            //            {
            //                areaName = areaFromAreaId.AreaName;
            //                positionDisplay = areaTypeLookup.TryGetValue(areaFromAreaId.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
            //                roomName = areaFromAreaId.Room?.RoomName;
            //                areaTypeName = positionDisplay;
            //            }
            //            else if (bd.PositionId.HasValue)
            //            {
            //                var areaFromPosition = areaLookup.Values.FirstOrDefault(a => a.Positions.Any(p => p.PositionId == bd.PositionId));
            //                if (areaFromPosition != null)
            //                {
            //                    positionDisplay = areaFromPosition.Positions.FirstOrDefault(p => p.PositionId == bd.PositionId)?.PositionNumber.ToString() ?? "N/A";
            //                    areaName = areaFromPosition.AreaName;
            //                    areaTypeName = areaFromPosition.AreaTypeId != 0 && areaTypeLookup.TryGetValue(areaFromPosition.AreaTypeId, out var at) ? at.AreaTypeName : "N/A";
            //                    roomName = areaFromPosition.Room?.RoomName;
            //                }
            //                else
            //                {
            //                    positionDisplay = "N/A";
            //                    areaName = "N/A";
            //                    areaTypeName = "N/A";
            //                    roomName = "N/A";
            //                }
            //            }

            //            var slot = allSlots.FirstOrDefault(s => s.SlotId == bd.SlotId);
            //            return new
            //            {
            //                BookingDetailId = bd.BookingDetailId,
            //                Position = positionDisplay,
            //                AreaName = areaName,
            //                AreaTypeName = areaTypeName,
            //                RoomName = roomName,
            //                SlotNumber = slot?.SlotNumber
            //            };
            //        }).ToList()
            //    };

            //    return Ok(new ResponseDTO<object>(200, "Đặt phòng thành công!", responseData));
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi đặt phòng", ex.Message));
            //}
        }

        private async Task<Tuple<bool, string>> CheckBookingTime(List<BookingTime> bookingTimes)
        {
            if(bookingTimes == null)
            {
                return new Tuple<bool, string>(false, "Bắt buộc ngày và slot muốn đặt phòng");
            }
            // Thêm kiểm tra giới hạn 2 tuần
            var maxBookingDate = DateTime.Now.Date.AddDays(14); // 2 tuần từ hôm nay
            var outOfRangeDates = bookingTimes.Where(x => x.BookingDate.Date > maxBookingDate);
            if (outOfRangeDates.Any())
            {
                return new Tuple<bool, string>(false, "Chỉ có thể đặt phòng trước tối đa 2 tuần!");
            }
            var wrongDte = bookingTimes.Where(x => x.BookingDate.Date < DateTime.Now.Date);
            if (wrongDte.Any())
            {
                return new Tuple<bool, string>(false, "Ngày đặt bắt buộc lớn hơn hoặc bằng ngày hiện tại!");
            }

            var inValidSlotInToday = await _unitOfWork.SlotRepository.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date && DateTime.Now.Date.Add(x.StartTime.Value) < DateTime.Now);
            var inValidSlotId = inValidSlotInToday.Select(x => x.SlotId).ToList();
            var bookingToday = bookingTimes.FirstOrDefault(x => x.BookingDate.Date == DateTime.Now.Date);
           foreach(var i in bookingToday.SlotId)
            {
                if (inValidSlotId.Contains(i))
                {
                    return new Tuple<bool, string>(false, "Đã quá thời gian đặt ngày hôm nay!");
                }
            }
           return new Tuple<bool, string>(true, "");

        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Booking> Get(Expression<Func<Booking, bool>> expression)
        {
           return await _unitOfWork.BookingRepository.Get(expression);
        }

        public async Task<IEnumerable<Booking>> GetAll()
        {
            return await _unitOfWork.BookingRepository.GetAll();
        }

        public async Task<IEnumerable<Booking>> GetAll(Expression<Func<Booking, bool>> expression)
        {
            return await _unitOfWork.BookingRepository.GetAll(expression);
        }

        public async Task<IEnumerable<Booking>> GetAllWithInclude(params Expression<Func<Booking, object>>[] includes)
        {
            return await _unitOfWork.BookingRepository.GetAllWithInclude(includes);
        }

        public async Task<Booking> GetById(int id)
        {
            return await _unitOfWork.BookingRepository.GetById(id);
        }

        public async Task<Booking> GetWithInclude(Expression<Func<Booking, bool>> expression, params Expression<Func<Booking, object>>[] includes)
        {
            return await _unitOfWork.BookingRepository.GetWithInclude(expression, includes);
        }

        public async Task Remove(Booking entity)
        {
             _unitOfWork.BookingRepository.Delete(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task Update(Booking entity)
        {
            await _unitOfWork.BookingRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }
    }
}
