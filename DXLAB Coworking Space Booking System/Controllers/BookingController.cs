using AutoMapper;
using DxLabCoworkingSpace;
using DxLabCoworkingSpace.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System
{
    [Route("api/booking")]
    [ApiController]
    //[Authorize(Roles = "Student")]
    public class BookingController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ISlotService _slotService;
        private readonly IBookingService _bookingService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IAreaService _areaService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IAreaTypeCategoryService _areaTypeCategoryService;
        private readonly IBlockchainBookingService _blockchainBookingService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        public BookingController(IRoomService roomService, ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService,
            IAreaService areaService, IAreaTypeService areaTypeService, IMapper mapper, IConfiguration configuration, IAreaTypeCategoryService areaTypeCategoryService,
            IBlockchainBookingService blockchainBookingService, IUserService userService, IUnitOfWork unitOfWork)
        {
            _roomService = roomService;
            _slotService = slotService;
            _bookingService = bookingService;
            _bookDetailService = bookDetailService;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
            _mapper = mapper;
            _configuration = configuration;
            _areaTypeCategoryService = areaTypeCategoryService;
            _blockchainBookingService = blockchainBookingService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDTO bookingDTO)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ResponseDTO<object>(401, "Bạn chưa đăng nhập hoặc token không hợp lệ!", null));
                }

                // Lấy WalletAddress của user
                var user = await _userService.Get(u => u.UserId == userId);
                if (user == null || string.IsNullOrEmpty(user.WalletAddress))
                {
                    return BadRequest(new ResponseDTO<object>(400, "Người dùng không có địa chỉ ví blockchain!", null));
                }
                ResponseDTO<object> result = await _bookingService.CreateBooking(bookingDTO, userId);
                if (result.StatusCode != 200)
                {
                    return StatusCode(result.StatusCode, result);
                }
                Booking booking = result.Data as Booking;
                booking.UserId = userId;
                await _bookingService.Add(booking);

                var allSlots = await _slotService.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
                var allAreasWithDetails = await _areaService.GetAllWithInclude(a => a.Room, a => a.AreaType, a => a.Positions);
                //var filteredAreas = allAreasWithDetails.Where(a => areasInRoom.Select(ar => ar.AreaId).Contains(a.AreaId)).ToList();
                var areaTypeIds = allAreasWithDetails.Select(a => a.AreaTypeId).Distinct().ToList();
                var areaTypes = await _areaTypeService.GetAll(at => areaTypeIds.Contains(at.AreaTypeId));

                // Tạo lookup để tra cứu nhanh
                var areaLookup = allAreasWithDetails.ToDictionary(a => a.AreaId, a => a);
                var areaTypeLookup = areaTypes.ToDictionary(at => at.AreaTypeId, at => at);

                var responseData = new
                {
                    BookingId = booking.BookingId,
                    BookingCreatedDate = booking.BookingCreatedDate,
                    TotalPrice = booking.Price,
                    Details = booking.BookingDetails.Select(bd =>
                    {
                        string positionDisplay = null;
                        string areaName = null;
                        string areaTypeName = null;
                        string roomName = null;

                        if (bd.AreaId.HasValue && areaLookup.TryGetValue(bd.AreaId.Value, out var areaFromAreaId))
                        {
                            areaName = areaFromAreaId.AreaName;
                            positionDisplay = areaTypeLookup.TryGetValue(areaFromAreaId.AreaTypeId, out var areaType) ? areaType.AreaTypeName : "N/A";
                            roomName = areaFromAreaId.Room?.RoomName;
                            areaTypeName = positionDisplay;
                        }
                        else if (bd.PositionId.HasValue)
                        {
                            var areaFromPosition = areaLookup.Values.FirstOrDefault(a => a.Positions.Any(p => p.PositionId == bd.PositionId));
                            if (areaFromPosition != null)
                            {
                                positionDisplay = areaFromPosition.Positions.FirstOrDefault(p => p.PositionId == bd.PositionId)?.PositionNumber.ToString() ?? "N/A";
                                areaName = areaFromPosition.AreaName;
                                areaTypeName = areaFromPosition.AreaTypeId != 0 && areaTypeLookup.TryGetValue(areaFromPosition.AreaTypeId, out var at) ? at.AreaTypeName : "N/A";
                                roomName = areaFromPosition.Room?.RoomName;
                            }
                            else
                            {
                                positionDisplay = "N/A";
                                areaName = "N/A";
                                areaTypeName = "N/A";
                                roomName = "N/A";
                            }
                        }

                        var slot = allSlots.FirstOrDefault(s => s.SlotId == bd.SlotId);
                        return new
                        {
                            BookingDetailId = bd.BookingDetailId,
                            Position = positionDisplay,
                            AreaName = areaName,
                            AreaTypeName = areaTypeName,
                            RoomName = roomName,
                            SlotNumber = slot?.SlotNumber
                        };
                    }).ToList()
                };
                return Ok(new ResponseDTO<object>(200, "Đặt phòng thành công!", responseData));

                //try
                //{

                //    // Thêm kiểm tra giới hạn 2 tuần
                //    var maxBookingDate = DateTime.Now.Date.AddDays(14); // 2 tuần từ hôm nay
                //    var outOfRangeDates = bookingDTO.bookingTimes.Where(x => x.BookingDate.Date > maxBookingDate);
                //    if (outOfRangeDates.Any())
                //    {
                //        return BadRequest(new ResponseDTO<object>(400, "Chỉ có thể đặt phòng trước tối đa 2 tuần!", null));
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
                //    var wrongDte = bookingDTO.bookingTimes.Where(x => x.BookingDate.Date < DateTime.Now.Date);
                //    if (wrongDte.Any())
                //    {
                //        var reponse = new ResponseDTO<object>(400, "Ngày đặt bắt buộc lớn hơn hoặc bằng ngày hiện tại!", null);
                //        return BadRequest(reponse);
                //    }
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
                //   //await _unitOfWork.CommitAsync(); // Gọi CommitAsync ở đây là cần thiết

                //// Lấy slot đầu tiên để gọi smart contract
                //var firstSlot = bookingDetails.FirstOrDefault()?.SlotId;
                //if (firstSlot == null)
                //{
                //    // Xóa booking nếu không tìm thấy slot
                //    await _bookingService.Remove(booking); // Đã bao gồm CommitAsync bên trong
                //    return BadRequest(new ResponseDTO<object>(400, "Không tìm thấy slot để đặt phòng!", null));
                //}

                //// Gọi blockchain để trừ tiền và phát sự kiện BookingCreated
                //var (success, txHash) = await _blockchainBookingService.BookOnBlockchain(booking.BookingId, userWalletAddress, (byte)firstSlot, booking.Price);
                //if (!success)
                //{
                //    // Nếu giao dịch blockchain thất bại, xóa booking vừa tạo
                //    await _bookingService.Remove(booking); // Đã bao gồm CommitAsync bên trong

                //    // Kiểm tra nguyên nhân cụ thể
                //    var userBalance = await _blockchainBookingService.GetUserBalance(userWalletAddress);
                //    var requiredTokens = Nethereum.Util.UnitConversion.Convert.ToWei(booking.Price);
                //    if (userBalance < requiredTokens)
                //    {
                //        return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! Số dư token không đủ.", null));
                //    }
                //    if (booking.BookingId <= 0)
                //    {
                //        return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! BookingId không hợp lệ.", null));
                //    }
                //    return BadRequest(new ResponseDTO<object>(400, "Thanh toán trên blockchain thất bại! Giao dịch không thành công.", null));
                //}

                // Chuẩn bị dữ liệu trả về giống GetStudentBookingHistoryDetail

            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDTO<object>(500, "Lỗi khi đặt phòng", ex.Message));
            }

        }

        //private Tuple<bool, string, List<KeyValuePair<int, int[]>>> findPosition(int[][] slotJaggedMatrix, Dictionary<int, int[]> searchMatrix)
        //{
        //    if (slotJaggedMatrix == null || searchMatrix == null)
        //        return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(false, "Lỗi khi đặt phòng", null);
        //    List<int[]> listOfJaggedMatrix = slotJaggedMatrix.ToList();
        //    listOfJaggedMatrix.Sort((a, b) => b.Length.CompareTo(a.Length));

        //    //kết quả
        //    List<KeyValuePair<int, int[]>> dictResult = new List<KeyValuePair<int, int[]>>();

        //    //Duyệt listJaggedMatrix
        //    for (int i = 0; i < listOfJaggedMatrix.Count; i++)
        //    {
        //        //tìm ra key + slotNumber 
        //        bool find = false;

        //        List<FilterPos> filterPos = new List<FilterPos>();
        //        foreach (var item in searchMatrix)
        //        {
        //            bool check = true;
        //            for (int j = 0; j < listOfJaggedMatrix[i].Length; j++)
        //            {
        //                if (item.Value[listOfJaggedMatrix[i][j] - 1] == 0)
        //                {
        //                    check = false; break;
        //                }
        //            }
        //            if (check)
        //            {
        //                //find sizeOffFrag
        //                int size = listOfJaggedMatrix[i].Length;
        //                // dời trái/ dời phải
        //                for (int k = listOfJaggedMatrix[i][0] - 1; k >= 0; k--)
        //                {
        //                    if (item.Value[k] == 1)
        //                        size++;
        //                    else
        //                        break;
        //                }
        //                for (int h = listOfJaggedMatrix[i][listOfJaggedMatrix[i].Length - 1] - 1; h < item.Value.Length; h++)
        //                {
        //                    if (item.Value[h] == 1)
        //                        size++;
        //                    else
        //                        break;
        //                }

        //                var fil = new FilterPos() { Key = item.Key, slotNums = listOfJaggedMatrix[i], sizeOfFrag = size };
        //                filterPos.Add(fil);
        //                find = true;
        //            }
        //            else
        //                continue;
        //        }
        //        if (find)
        //        {
        //            var bestFitPos = filterPos.OrderBy(x => x.sizeOfFrag).FirstOrDefault();
        //            foreach (var item in bestFitPos.slotNums)
        //            {
        //                searchMatrix[bestFitPos.Key][item - 1] = 0;
        //            }

        //            ;
        //            dictResult.Add(new KeyValuePair<int, int[]>(bestFitPos.Key, bestFitPos.slotNums));
        //        }
        //        else return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(false, "Lỗi khi đặt phòng", null);
        //    }
        //    return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(true, "", dictResult);
        //}

        //private int[][] CreateSlotJaggedMatrix(int[] arr)
        //{
        //    List<int[]> result = new List<int[]>();
        //    List<int> currentGroup = new List<int>();

        //    for (int i = 0; i < arr.Length; i++)
        //    {
        //        if (currentGroup.Count == 0 || arr[i] == arr[i - 1] + 1)
        //        {
        //            currentGroup.Add(arr[i]);
        //        }
        //        else
        //        {
        //            result.Add(currentGroup.ToArray());
        //            currentGroup = new List<int> { arr[i] };
        //        }
        //    }
        //    if (currentGroup.Count > 0)
        //    {
        //        result.Add(currentGroup.ToArray());
        //    }
        //    return result.ToArray();
        //}
        //// area ở đây đã lấy được list pos
        //private async Task<Dictionary<int, int[]>> CreateSearchMatrix(IEnumerable<Area> areasInRoom, DateTime date)
        //{
        //    if (areasInRoom == null) return null;
        //    var listOfSlot = await _slotService.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
        //    if (areasInRoom.FirstOrDefault() != null)
        //    {
        //        Dictionary<int, int[]> dict = new Dictionary<int, int[]>();
        //        if (areasInRoom.FirstOrDefault().AreaType.AreaCategory == 1)
        //        {
        //            var individualArea = areasInRoom.FirstOrDefault();
        //            if(individualArea.ExpiredDate <= date) 
        //            {
        //                KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(-1, new int[1] {1});
        //                var dictError = new Dictionary<int, int[]>();
        //                dictError.Add(keyValuePair.Key,keyValuePair.Value);
        //                return dictError;
        //            }
        //            foreach (var pos in individualArea.Positions)
        //            {
        //                int[] slotNumber = listOfSlot.Select(x => x.SlotNumber).ToArray();
        //                Array.Sort(slotNumber);
        //                KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(pos.PositionId, slotNumber);
        //                keyValuePair = await FillDataInToKeyValuePair(keyValuePair, 1, date.Date);
        //                dict.Add(keyValuePair.Key, keyValuePair.Value);
        //            }
        //            return dict;
        //        }
        //        else
        //        {
        //            foreach (var are in areasInRoom)
        //            {
        //                if (are.ExpiredDate <= date)
        //                {
        //                    KeyValuePair<int, int[]> keyValuePair2 = new KeyValuePair<int, int[]>(-1, new int[1] { 1 });
        //                    var dictError = new Dictionary<int, int[]>();
        //                    dictError.Add(keyValuePair2.Key, keyValuePair2.Value);
        //                    return dictError;
        //                }
        //                int[] slotNumber = listOfSlot.Select(x => x.SlotNumber).ToArray();
        //                Array.Sort(slotNumber);
        //                KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(are.AreaId, slotNumber);
        //                keyValuePair = await FillDataInToKeyValuePair(keyValuePair, 2, date);
        //                dict.Add(keyValuePair.Key, keyValuePair.Value);
        //            }
        //            return dict;
        //        }
        //    }
        //    return null;
        //}

        //private async Task<KeyValuePair<int, int[]>> FillDataInToKeyValuePair(KeyValuePair<int, int[]> keyValuePair, int v, DateTime date)
        //{
        //    if (v < 1 || v > 2)
        //        return new KeyValuePair<int, int[]>(0, new int[0]);

        //    var bookingDetail = await _bookDetailService.GetAllWithInclude(x => x.Slot);

        //    var bookingDetailIn_Date = bookingDetail.Where(x => x.CheckinTime.Date == date.Date);
        //    if (v == 1)
        //    {
        //        //Cá nhân lấy posNumber
        //        for (int i = 0; i < keyValuePair.Value.Length; i++)
        //        {
        //            if (bookingDetailIn_Date.FirstOrDefault(x => x.PositionId == keyValuePair.Key && x.Slot.SlotNumber == keyValuePair.Value[i]) != null)
        //            {
        //                keyValuePair.Value[i] = 0;
        //                continue;
        //            }
        //            keyValuePair.Value[i] = 1;
        //        }
        //    }
        //    else
        //    {
        //        //Cá nhân lấy areaid
        //        for (int i = 0; i < keyValuePair.Value.Length; i++)
        //        {
        //            if (bookingDetailIn_Date.FirstOrDefault(x => x.AreaId == keyValuePair.Key && x.Slot.SlotNumber == keyValuePair.Value[i]) != null)
        //            {
        //                keyValuePair.Value[i] = 0;
        //                continue;

        //            }
        //            keyValuePair.Value[i] = 1;
        //        }
        //    }
        //    return keyValuePair;
        //}

        //[HttpPost("test")]
        //public async Task<IActionResult> TestBooking()
        //{
        //    BookingDetail b1 = await _bookDetailService.GetById(58);
        //    b1.Price = 1;
        //   await _bookDetailService.Update(b1);
        //    return Ok();
        //}

        [HttpGet("availiblepos")]
        public async Task<IActionResult> GetAvailableSlot([FromQuery] AvailableSlotRequestDTO availableSlotRequestDTO)
        {
            ResponseDTO<object> result = await _bookingService.GetAvailableSlot(availableSlotRequestDTO);
            return StatusCode(result.StatusCode, result);

            //var maxBookingDate = DateTime.Now.Date.AddDays(14);
            //if (availableSlotRequestDTO.BookingDate.Date > maxBookingDate.Date)
            //    return BadRequest();
            //var room = await _roomService.GetWithInclude(x => x.Status == 1 && x.RoomId == availableSlotRequestDTO.RoomId, x => x.Areas);

            //if (room == null)
            //{
            //    var responseDTO = new ResponseDTO<object>(404, "Phòng không tồn tại. Vui lòng nhập lại!", null);
            //    return NotFound(responseDTO);

            //}
            //var areas = room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date);
            //room.Areas = areas.ToList();
            //if (availableSlotRequestDTO.BookingDate.Date < DateTime.Now.Date)
            //{
            //    var responseDTO = new ResponseDTO<object>(404, $"Phải nhập ngày lớn hoặc bằng ngày: {DateTime.Now.Date}", null);
            //    return BadRequest(responseDTO);
            //}
            //var areaType = await _areaTypeService.Get(x => x.AreaTypeId == availableSlotRequestDTO.AreaTypeId && x.Status == 1);
            //if(areaType == null)
            //{
            //    var responseDTO = new ResponseDTO<object>(404, $"Không tìm thấy loại khu vực cho thuê", null);
            //    return BadRequest(responseDTO);
            //}
            //var areasInRoom = room.Areas.Where(x => x.AreaTypeId == availableSlotRequestDTO.AreaTypeId);
            //if (!areasInRoom.Any())
            //{
            //    var responseDTO = new ResponseDTO<object>(404, $"Không tồn tại khu vực đã nhập trong phòng{room.RoomName}", null);
            //    return NotFound(responseDTO);
            //}
            //var bookingDetails = await _bookDetailService.GetAll(x => x.CheckinTime.Date == availableSlotRequestDTO.BookingDate.Date);
            //var slots = await _slotService.GetAll( x => x.ExpiredTime.Date > DateTime.Now.Date);
            //if (!slots.Any())
            //{
            //    var responseDTO = new ResponseDTO<object>(404, $"Chưa có slot cho thuê", null);
            //    return NotFound(responseDTO);
            //}

            //// Lọc các slot còn hợp lệ dựa trên thời gian hiện tại
            //var currentDateTime = DateTime.Now;
            //var validSlots = slots.Where(slot =>
            //{
            //    var slotStartTime = availableSlotRequestDTO.BookingDate.Date.Add(slot.StartTime.Value);
            //    // Chỉ giữ lại slot nếu ngày đặt là hôm nay và slot chưa bắt đầu, hoặc ngày đặt là tương lai
            //    return availableSlotRequestDTO.BookingDate.Date > DateTime.Now.Date || slotStartTime > currentDateTime;
            //}).ToList();

            //List<AvailableSlotResponseDTO> availableSlotResponseDTOs = new List<AvailableSlotResponseDTO>();
            //if (areaType.AreaCategory == 1)
            //{
            //    var individualArea = areasInRoom.FirstOrDefault();
            //    individualArea = await _areaService.GetWithInclude(x => x.AreaId == individualArea.AreaId, x => x.Positions);
            //    foreach (var slot in validSlots)
            //    {
            //        int availblePos = 0;
            //        int bookedPos = 0;
            //        foreach (var item in bookingDetails)
            //        {
            //            if (item.CheckinTime.Date == availableSlotRequestDTO.BookingDate.Date
            //              && item.SlotId == slot.SlotId && individualArea.Positions.FirstOrDefault(x => x.PositionId == item.PositionId) != null)
            //            { bookedPos++; }
            //        }
            //        var response = new AvailableSlotResponseDTO() { SlotId = slot.SlotId, SlotNumber = slot.SlotNumber, AvailableSlot = areaType.Size - bookedPos };
            //        availableSlotResponseDTOs.Add(response);
            //    }
            //}
            //else
            //{
            //    foreach (var slot in validSlots)
            //    {
            //        int availblePos = 0;
            //        int bookedPos = 0;
            //        foreach (var item in bookingDetails)
            //        {
            //            if (item.CheckinTime.Date == availableSlotRequestDTO.BookingDate.Date
            //              && item.SlotId == slot.SlotId && areasInRoom.FirstOrDefault(x => x.AreaId == item.AreaId) != null)
            //            { bookedPos++; }
            //        }
            //        var response = new AvailableSlotResponseDTO() { SlotId = slot.SlotId, SlotNumber = slot.SlotNumber, AvailableSlot = areasInRoom.Count() - bookedPos };
            //        availableSlotResponseDTOs.Add(response);
            //    }
            //}
            //return Ok(new ResponseDTO<List<AvailableSlotResponseDTO>>(200, "ok", availableSlotResponseDTOs));
        }

        [HttpGet("categoryinroom")]
        public async Task<IActionResult> GetAllAreaCategoryInRoom(int id)
        {
            ResponseDTO<object> result = await _roomService.GetAllAreaCategoryInRoom(id);
            return StatusCode(result.StatusCode, result);


            //try
            //{

            //    var room = await _roomService.Get(x => x.RoomId == id && x.Status == 1);
            //    if (room == null)
            //    {
            //        var response = new ResponseDTO<object>(400, "Không tìm thấy phòng", null);
            //        return NotFound(response);

            //    }
            //    var areas = await _areaService.GetAll(x => x.RoomId == room.RoomId && x.ExpiredDate.Date > DateTime.Now.Date);
            //    if (areas == null)
            //    {
            //        var response = new ResponseDTO<object>(400, "Không tìm thấy khu vực sẵn sàng", null);
            //        return NotFound(response);
            //    }
            //    List<AreaType> areaTypes = new List<AreaType>();
            //    var areaTypesDb = await _areaTypeService.GetAll(x => x.Status == 1);
            //    foreach (var area in areas)
            //    {
            //        var areaType = areaTypesDb.FirstOrDefault(x => x.AreaTypeId == area.AreaTypeId);
            //        if (areaType != null)
            //            areaTypes.Add(areaType);
            //    }

            //    var areaTypeGroup = areaTypes.GroupBy(x => x.AreaCategory);
            //    var areaTypeCates = await _areaTypeCategoryService.GetAllWithInclude(x => x.Images);
            //    if (areaTypeCates == null) return NotFound(new ResponseDTO<object>(200, "Không tìm thấy thông tin phù hợp!", null));
            //    List<KeyValuePair<AreaTypeCategoryDTO, List<AreaTypeDTO>>> result = new List<KeyValuePair<AreaTypeCategoryDTO, List<AreaTypeDTO>>>();
            //    foreach (var group in areaTypeGroup)
            //    {
            //        List<AreaTypeDTO> areaTypeDTOs = new List<AreaTypeDTO>();
            //        foreach (var item in group)
            //        {
            //            var areaType = _mapper.Map<AreaTypeDTO>(item);
            //            areaTypeDTOs.Add(areaType);
            //        }
            //        var aretypeCategory = areaTypeCates.FirstOrDefault(x => x.CategoryId == group.Key);
            //        if (aretypeCategory == null)
            //            return NotFound(new ResponseDTO<object>(200, "Không tìm thấy thông tin phù hợp!", null));
            //        var aretypeCategoryDTO = new AreaTypeCategoryDTO()
            //        {
            //            CategoryId = aretypeCategory.CategoryId,
            //            Title = aretypeCategory.Title,
            //            CategoryDescription = aretypeCategory.CategoryDescription,
            //            Images = aretypeCategory.Images.Select(x => x.ImageUrl).ToList()

            //        };
            //        KeyValuePair<AreaTypeCategoryDTO, List<AreaTypeDTO>> keyValuePair = new KeyValuePair<AreaTypeCategoryDTO, List<AreaTypeDTO>>(aretypeCategoryDTO, areaTypeDTOs);
            //        result.Add(keyValuePair);
            //    }
            //    var response1 = new ResponseDTO<object>(200, "Trả thành công", result);
            //    return Ok(response1);

            //}
            //catch (Exception ex)
            //{
            //    var response1 = new ResponseDTO<object>(500, "Lỗi khi đặt phòng", ex.Message);
            //    return StatusCode(500, response1);
            //}
        }


    }

}