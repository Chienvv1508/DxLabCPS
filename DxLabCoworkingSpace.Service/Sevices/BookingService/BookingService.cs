using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Nethereum.RPC.Eth.DTOs;
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


            try
            {
                var isValid = ValidationModel<BookingDTO>.ValidateModel(bookingDTO);
                if (isValid.Item1 == false)
                {
                    return new ResponseDTO<object>(400, isValid.Item2, null);
                }

                Tuple<bool, string> checkBookingTime = await CheckBookingTime(bookingDTO.bookingTimes);

                if (checkBookingTime.Item1 == false)
                {
                    return new ResponseDTO<object>(400, checkBookingTime.Item2, null);
                }

                Tuple<bool, string, Room, List<Area>> checkRoom = await CheckRoom(bookingDTO.RoomId, bookingDTO.AreaTypeId);
                if(checkRoom.Item1 == false)
                {
                    return new ResponseDTO<object>(400, checkRoom.Item2, null);
                }

                Tuple<bool, string, Booking> bookingResult = await AddBooking(bookingDTO,checkRoom.Item3, checkRoom.Item4);

                if(bookingResult.Item1 == false)
                {
                    return new ResponseDTO<object>(400, bookingResult.Item2, null);
                }
                else
                    return new ResponseDTO<object>(200, bookingResult.Item2, bookingResult.Item3);

            }
            catch (Exception ex)
            {
                return  new ResponseDTO<object>(500, "Lỗi khi đặt phòng", ex.Message);
            }
        }

        private async Task<Tuple<bool, string, Booking>> AddBooking(BookingDTO bookingDTO, Room room, List<Area> areaInRoom)
        {
            if (bookingDTO == null || room == null)
                return new Tuple<bool, string, Booking>(false, "Bắt buộc nhập tham số đầu vào!", null);
            Booking booking = new Booking();
            List<BookingDetail> bookingDetails = new List<BookingDetail>();
            var slots = await _unitOfWork.SlotRepository.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
            var existedBookingDetails = await _unitOfWork.BookingDetailRepository.GetAll(x => x.CheckinTime.Date >= DateTime.Now.Date);
            
            foreach (var dte in bookingDTO.bookingTimes)
            {
                booking.UserId = 1;
                booking.BookingCreatedDate = DateTime.Now;
                var slotBooks = slots.Where(x => x.ExpiredTime.Date > dte.BookingDate);
                // Tạo ma trận
                Dictionary<int, int[]> searchMatrix = await CreateSearchMatrix(areaInRoom, dte.BookingDate.Date, slotBooks.ToList(), existedBookingDetails);
                if (searchMatrix == null)
                {
                    return new Tuple<bool, string, Booking>(false, "Lỗi đặt phòng", null);
                }
                int[] slotArray = new int[dte.SlotId.Count];
                for (int i = 0; i < slotArray.Length; i++)
                {
                    var x = slotBooks.FirstOrDefault(x => x.SlotId == dte.SlotId[i]);
                    if (x == null)
                    return new Tuple<bool, string, Booking>(false, "Slot không có hoặc đã bị xóa", null);
                    slotArray[i] = x.SlotNumber;
                }
                int[][] slotJaggedMatrix = CreateSlotJaggedMatrix(slotArray);
                Tuple<bool, string, List<KeyValuePair<int, int[]>>> findPositionResult = findPosition(slotJaggedMatrix, searchMatrix);
                if (findPositionResult.Item1 == false)
                {
                    return new Tuple<bool, string, Booking>(false, findPositionResult.Item2, null);
                }
                else
                {
                    if (areaInRoom.First().AreaType.AreaCategory == 1)
                    {
                        foreach (var item in findPositionResult.Item3)
                        {
                            for (int i = 0; i < item.Value.Length; i++)
                            {
                                var bookingDetail = new BookingDetail();
                                int id = item.Key;

                                bookingDetail.PositionId = id;
                                var slot = slots.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
                                bookingDetail.SlotId = slot.SlotId;
                                bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
                                //if (i == item.Value.Length - 1)
                                //{
                                //    bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                //}
                                //else
                                //    bookingDetail.CheckoutTime = null;
                                bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                var areaBook = areaInRoom.FirstOrDefault(x => x.Positions.FirstOrDefault(x => x.PositionId == id) != null);
                                bookingDetail.Price = areaBook.AreaType.Price;
                                bookingDetail.AreaId = areaBook.AreaId;
                                bookingDetails.Add(bookingDetail);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in findPositionResult.Item3)
                        {
                            for (int i = 0; i < item.Value.Length; i++)
                            {
                                var bookingDetail = new BookingDetail();
                                int id = item.Key;

                                bookingDetail.AreaId = id;

                                var slot = slots.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
                                bookingDetail.SlotId = slot.SlotId;
                                bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
                                //if (i == item.Value.Length - 1)
                                //{
                                //    bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                //}
                                //else
                                //    //bookingDetail.CheckoutTime = null;

                                    bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                var areaBook = areaInRoom.FirstOrDefault(x => x.AreaId == id);
                                bookingDetail.Price = areaBook.AreaType.Price;
                                bookingDetails.Add(bookingDetail);
                            }
                        }
                    }
                }
            }

            // Tính TotalPrice và lưu Booking
            booking.Price = bookingDetails.Sum(br => br.Price);
            booking.BookingDetails = bookingDetails;

            //// Lưu booking vào database trước để nhận BookingId hợp lệ
            return new Tuple<bool, string, Booking>(true, "", booking);
        }
        private Tuple<bool, string, List<KeyValuePair<int, int[]>>> findPosition(int[][] slotJaggedMatrix, Dictionary<int, int[]> searchMatrix)
        {
            if (slotJaggedMatrix == null || searchMatrix == null)
                return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(false, "Lỗi khi đặt phòng", null);
            List<int[]> listOfJaggedMatrix = slotJaggedMatrix.ToList();
            listOfJaggedMatrix.Sort((a, b) => b.Length.CompareTo(a.Length));

            //kết quả
            List<KeyValuePair<int, int[]>> dictResult = new List<KeyValuePair<int, int[]>>();

            //Duyệt listJaggedMatrix
            for (int i = 0; i < listOfJaggedMatrix.Count; i++)
            {
                //tìm ra key + slotNumber 
                bool find = false;
                List<FilterPos> filterPos = new List<FilterPos>();
                foreach (var item in searchMatrix)
                {
                    bool check = true;
                    for (int j = 0; j < listOfJaggedMatrix[i].Length; j++)
                    {
                        if (item.Value[listOfJaggedMatrix[i][j] - 1] == 0)
                        {
                            check = false; break;
                        }
                    }
                    if (check)
                    {
                        //find sizeOffFrag
                        int size = listOfJaggedMatrix[i].Length;
                        // dời trái/ dời phải
                        for (int k = listOfJaggedMatrix[i][0] - 1; k >= 0; k--)
                        {
                            if (item.Value[k] == 1)
                                size++;
                            else
                                break;
                        }
                        for (int h = listOfJaggedMatrix[i][listOfJaggedMatrix[i].Length - 1] - 1; h < item.Value.Length; h++)
                        {
                            if (item.Value[h] == 1)
                                size++;
                            else
                                break;
                        }

                        var fil = new FilterPos() { Key = item.Key, slotNums = listOfJaggedMatrix[i], sizeOfFrag = size };
                        filterPos.Add(fil);
                        find = true;
                    }
                    else
                        continue;
                }
                if (find)
                {
                    var bestFitPos = filterPos.OrderBy(x => x.sizeOfFrag).FirstOrDefault();
                    foreach (var item in bestFitPos.slotNums)
                    {
                        searchMatrix[bestFitPos.Key][item - 1] = 0;
                    }

                  
                    dictResult.Add(new KeyValuePair<int, int[]>(bestFitPos.Key, bestFitPos.slotNums));
                }
                else return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(false, "Lỗi khi đặt phòng", null);
            }
            return new Tuple<bool, string, List<KeyValuePair<int, int[]>>>(true, "", dictResult);
        }

        private int[][] CreateSlotJaggedMatrix(int[] arr)
        {
            List<int[]> result = new List<int[]>();
            List<int> currentGroup = new List<int>();

            for (int i = 0; i < arr.Length; i++)
            {
                if (currentGroup.Count == 0 || arr[i] == arr[i - 1] + 1)
                {
                    currentGroup.Add(arr[i]);
                }
                else
                {
                    result.Add(currentGroup.ToArray());
                    currentGroup = new List<int> { arr[i] };
                }
            }
            if (currentGroup.Count > 0)
            {
                result.Add(currentGroup.ToArray());
            }
            return result.ToArray();
        }
        // area ở đây đã lấy được list pos
        private async Task<Dictionary<int, int[]>> CreateSearchMatrix(IEnumerable<Area> areasInRoom, DateTime date, List<Slot> slots, IEnumerable<BookingDetail> existedBookingDetails)
        {
            if (areasInRoom == null || slots == null) return null;
            if (areasInRoom.FirstOrDefault() != null)
            {
                Dictionary<int, int[]> dict = new Dictionary<int, int[]>();
                if (areasInRoom.FirstOrDefault().AreaType.AreaCategory == 1)
                {
                    var individualArea = areasInRoom.FirstOrDefault();
                    foreach (var pos in individualArea.Positions)
                    {
                        int[] slotNumber = slots.Select(x => x.SlotNumber).ToArray();
                        Array.Sort(slotNumber);
                        KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(pos.PositionId, slotNumber);
                        keyValuePair = await FillDataInToKeyValuePair(keyValuePair, 1, date.Date, existedBookingDetails);
                        dict.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    return dict;
                }
                else
                {
                    foreach (var are in areasInRoom)
                    {
                        int[] slotNumber = slots.Select(x => x.SlotNumber).ToArray();
                        Array.Sort(slotNumber);
                        KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(are.AreaId, slotNumber);
                        keyValuePair = await FillDataInToKeyValuePair(keyValuePair, 2, date, existedBookingDetails);
                        dict.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    return dict;
                }
            }
            return null;
        }

        private async Task<KeyValuePair<int, int[]>> FillDataInToKeyValuePair(KeyValuePair<int, int[]> keyValuePair, int v, DateTime date, IEnumerable<BookingDetail> existedBookingDetails)
        {
            if (v < 1 || v > 2)
                return new KeyValuePair<int, int[]>(0, new int[0]);
            if ( existedBookingDetails == null)
                throw new ArgumentNullException();

            var bookingDetailIn_Date = existedBookingDetails.Where(x => x.CheckinTime.Date == date.Date);
            if (v == 1)
            {
                //Cá nhân lấy posNumber
                for (int i = 0; i < keyValuePair.Value.Length; i++)
                {
                    if (bookingDetailIn_Date.FirstOrDefault(x => x.PositionId == keyValuePair.Key && x.Slot.SlotNumber == keyValuePair.Value[i]) != null)
                    {
                        keyValuePair.Value[i] = 0;
                        continue;
                    }
                    keyValuePair.Value[i] = 1;
                }
            }
            else
            {
                //Cá nhân lấy areaid
                for (int i = 0; i < keyValuePair.Value.Length; i++)
                {
                    if (bookingDetailIn_Date.FirstOrDefault(x => x.AreaId == keyValuePair.Key && x.Slot.SlotNumber == keyValuePair.Value[i]) != null)
                    {
                        keyValuePair.Value[i] = 0;
                        continue;

                    }
                    keyValuePair.Value[i] = 1;
                }
            }
            return keyValuePair;
        }

        private async Task<Tuple<bool, string, Room, List<Area>>> CheckRoom(int roomId, int areaTypeId)
        {
            //Check RoomId
            Room room = await _unitOfWork.RoomRepository.GetWithInclude(x => x.RoomId == roomId && x.Status == 1, x => x.Areas, x => x.Images);
            if (room == null)
            {
                return new Tuple<bool, string, Room, List<Area>>(false, "Không tìm thấy phòng cần đặt!", null, null);
            }

            var areasInRoomForBook = room.Areas.Where(x => x.ExpiredDate.Date > DateTime.Now.Date && x.Status == 1 && x.AreaTypeId == areaTypeId);
            if (!areasInRoomForBook.Any())
            {
                return new Tuple<bool, string, Room, List<Area>>(false, "Không tồn tại khu vực cần đặt trong phòng!", null, null);
            }
            List<Area> newAreaInRoom = new List<Area>();
            //Get All Information
            foreach (var ar in areasInRoomForBook)
            {
                var x = await _unitOfWork.AreaRepository.GetWithInclude(x => x.AreaId == ar.AreaId, x => x.AreaType, x => x.Positions);
                newAreaInRoom.Add(x);
            }
            //room.Areas = newAreaInRoom;
            return new Tuple<bool, string, Room, List<Area>>(true, "", room,newAreaInRoom);
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

            var slots = await _unitOfWork.SlotRepository.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date);
            if (!slots.Any())
            {
                return new Tuple<bool, string>(false, "Chưa có slot có sẵn cho thuê");
            }
            //&& DateTime.Now.Date.Add(x.StartTime.Value) < DateTime.Now
            List<Slot> inValidSlotInToday = new List<Slot>();
            foreach(var slot in slots)
            {
                if(DateTime.Now.Date.Add(slot.StartTime.Value) < DateTime.Now)
                {
                    inValidSlotInToday.Add(slot);
                }
            }
            var inValidSlotId = inValidSlotInToday.Select(x => x.SlotId).ToList();
            var bookingToday = bookingTimes.FirstOrDefault(x => x.BookingDate.Date == DateTime.Now.Date);
            if(bookingToday != null)
            {
                foreach (var i in bookingToday.SlotId)
                {
                    if (inValidSlotId.Contains(i))
                    {
                        return new Tuple<bool, string>(false, "Đã quá thời gian đặt ngày hôm nay!");
                    }
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

        public async Task<ResponseDTO<object>> GetAvailableSlot(AvailableSlotRequestDTO availableSlotRequestDTO)
        {
            var isValid = ValidationModel<AvailableSlotRequestDTO>.ValidateModel(availableSlotRequestDTO);
            if (isValid.Item1 == false)
                return new ResponseDTO<object>(400, isValid.Item2, null);
            
            var slots = await _unitOfWork.SlotRepository.GetAll(x => x.ExpiredTime.Date > availableSlotRequestDTO.BookingDate.Date);
            if(!slots.Any()) return new ResponseDTO<object>(400, "Chưa có slots có sẵn", null);
            Tuple<bool, string> isValidDate = CheckValidDate(availableSlotRequestDTO.BookingDate, slots);
            if(isValidDate.Item1 == false)
            {
                return new ResponseDTO<object>(400, isValidDate.Item2, null);
            }
            Tuple<bool, string, Room, AreaType> isValidRoomAndAreaType = await CheckValidRoomAndAreaType(availableSlotRequestDTO);
            if(isValidRoomAndAreaType.Item1 == false)
            {
                return new ResponseDTO<object>(400, isValidRoomAndAreaType.Item2, null);
            }
            var room = isValidRoomAndAreaType.Item3;
            var areaType = isValidRoomAndAreaType.Item4;

            List<AvailableSlotResponseDTO> availableSlotResponseDTOs = await FindAvailableSlot(room, areaType, slots, availableSlotRequestDTO.BookingDate.Date);
            return new ResponseDTO<object>(200,"Danh sách các slot phù hợp cho thuê", availableSlotResponseDTOs);



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

            // List<AvailableSlotResponseDTO> availableSlotResponseDTOs = new List<AvailableSlotResponseDTO>();
            //if (areaType.AreaCategory == 1)
            //{
            //    var individualArea = room.Areas.FirstOrDefault();
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

        private async Task<List<AvailableSlotResponseDTO>> FindAvailableSlot(Room room, AreaType areaType, IEnumerable<Slot> slots, DateTime date)
        {
            if (room == null || areaType == null || slots == null)
                return null;
            List<AvailableSlotResponseDTO> availableSlotResponseDTOs = new List<AvailableSlotResponseDTO>();
            var areas = room.Areas.Where(x => x.Status == 1 && x.ExpiredDate.Date > date.Date && x.AreaTypeId == areaType.AreaTypeId);
            if (!areas.Any()) return null;
            var bookingDetails = await _unitOfWork.BookingDetailRepository.GetAll(x => x.CheckinTime.Date == date.Date);
            if (areaType.AreaCategory == 1)
            {
                var individualArea = areas.First();
                individualArea = await _unitOfWork.AreaRepository.GetWithInclude(x => x.AreaId == individualArea.AreaId, x => x.Positions);
                foreach (var slot in slots)
                {
                    int availblePos = 0;
                    int bookedPos = 0;
                    foreach (var item in bookingDetails)
                    {
                        if (item.SlotId == slot.SlotId && individualArea.Positions.FirstOrDefault(x => x.PositionId == item.PositionId) != null)
                        { bookedPos++; }
                    }
                    var response = new AvailableSlotResponseDTO() { SlotId = slot.SlotId, SlotNumber = slot.SlotNumber, AvailableSlot = areaType.Size - bookedPos };
                    availableSlotResponseDTOs.Add(response);
                }
            }
            else
            {
                foreach (var slot in slots)
                {
                    int availblePos = 0;
                    int bookedPos = 0;
                    foreach (var item in bookingDetails)
                    {
                        if (item.SlotId == slot.SlotId && areas.FirstOrDefault(x => x.AreaId == item.AreaId) != null)
                        { bookedPos++; }
                    }
                    var response = new AvailableSlotResponseDTO() { SlotId = slot.SlotId, SlotNumber = slot.SlotNumber, AvailableSlot = areas.Count() - bookedPos };
                    availableSlotResponseDTOs.Add(response);
                }
            }

            return availableSlotResponseDTOs;

        }

        private async Task<Tuple<bool, string, Room, AreaType>> CheckValidRoomAndAreaType(AvailableSlotRequestDTO availableSlotRequestDTO)
        {
            var room = await _unitOfWork.RoomRepository.GetWithInclude(x => x.Status == 1 && x.RoomId == availableSlotRequestDTO.RoomId, x => x.Areas);
            if (room == null)
            {
                return new Tuple<bool, string, Room, AreaType>(false, "Phòng không tồn tại. Vui lòng nhập lại!", null, null);
            }
            var areaType = await _unitOfWork.AreaTypeRepository.Get(x => x.AreaTypeId == availableSlotRequestDTO.AreaTypeId && x.Status == 1);
            if (areaType == null)
            {
                return new Tuple<bool, string, Room, AreaType>(false, $"Không tìm thấy loại khu vực cho thuê", null, null);
            }
            return new Tuple<bool, string, Room, AreaType>(true, "", room, areaType);
        }

        private Tuple<bool, string> CheckValidDate(DateTime bookingDate, IEnumerable<Slot> slots)
        {
            if (slots == null) return new Tuple<bool, string>(false, "Chưa có slots có sẵn");

            if(bookingDate.Date > DateTime.Now.Date.AddDays(14) || bookingDate.Date < DateTime.Now.Date)
                return new Tuple<bool, string>(false, "Ngày đặt không được quá 14 ngày hoặc ngày trong quá khứ!");
            if(bookingDate.Date == DateTime.Now.Date)
            {
                var currentDateTime = DateTime.Now;
                var validSlots = slots.Where(slot =>
                {
                    var slotStartTime = bookingDate.Date.Add(slot.StartTime.Value);
                    // Chỉ giữ lại slot nếu ngày đặt là hôm nay và slot chưa bắt đầu, hoặc ngày đặt là tương lai
                    return  slotStartTime > currentDateTime;
                }).ToList();

                slots = validSlots;
               
            }

            return new Tuple<bool, string>(true, "");
        }
    }

    public class FilterPos
    {
        public int Key { get; set; }
        public int[] slotNums { get; set; }
        public int sizeOfFrag { get; set; }
    }



}
