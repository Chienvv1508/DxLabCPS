using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Mvc;

namespace DXLAB_Coworking_Space_Booking_System
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ISlotService _slotService;
        private readonly IBookingService _bookingService;
        private readonly IBookingDetailService _bookDetailService;
        private readonly IAreaService _areaService;

        public BookingController(IRoomService roomService,ISlotService slotService, IBookingService bookingService, IBookingDetailService bookDetailService, IAreaService areaService)
        {
            _roomService = roomService;
            _slotService = slotService;
            _bookingService = bookingService;
            _bookDetailService = bookDetailService;
            _areaService = areaService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDTO bookingDTO)
        {

            //Get Information
            var roomId = bookingDTO.RoomId;
            var areaTypeId = bookingDTO.AreaTypeId;
            var bookingDates = bookingDTO.bookingTimes;


            //Check RoomId
            Room room = await _roomService.GetRoomWithAllInClude(x => x.RoomId == bookingDTO.RoomId);
            if (room == null)
            {
                var reponse = new ResponseDTO<object>(400, "Không tìm thấy phòng cần đặt!", null);
                return BadRequest(reponse);
            }
            //Check AraeTypeId
            var areasInRoom = room.Areas.Where(x => x.AreaTypeId == areaTypeId);
            if (!areasInRoom.Any())
            {
                var reponse = new ResponseDTO<object>(400, "Trong phòng không có khu vực nào có loại khu vực như đã nhập!", null);
                return BadRequest(reponse);

            }
            //Check Ngày
            var wrongDte = bookingDTO.bookingTimes.Where(x => x.BookingDate.Date < DateTime.Now.Date);
            if(wrongDte.Any())
            {
                var reponse = new ResponseDTO<object>(400, "Ngày đặt bắt buộc lớn hơn hoặc bằng ngày hiện tại!", null);
                return BadRequest(reponse);
            }
            List<Area> newAreaInRoom = new List<Area>();
            //Get All Information
            foreach(var ar in areasInRoom)
            {
                var x  = await _areaService.GetWithInclude(x => x.AreaId == ar.AreaId, x => x.AreaType, x => x.Positions);
                newAreaInRoom.Add(x);
            }
            areasInRoom = newAreaInRoom;

            ////CheckSlot ngày hiện tại
            //var currentDte = bookingDTO.bookingTimes.FirstOrDefault(x => x.BookingDate == DateTime.Now.Date);
            //if (currentDte != null)
            //{
            //    var slots = currentDte.SlotId;
            //    var slotListFromDb = await _slotService.GetAll();
            //    if(!slotListFromDb.Any())
            //    {
            //        var reponse = new ResponseDTO<object>(400, "Lỗi khi đặt phòng", null);
            //        return BadRequest(reponse);
            //    }
            //    List<Slot> slotList = new List<Slot>();
            //    foreach(var item in slots)
            //    {
            //        var slot = slotListFromDb.FirstOrDefault(x => x.SlotId == item);
            //        if(slot == null)
            //        {
            //            var reponse = new ResponseDTO<object>(400, "Không tồn tại slot", null);
            //            return BadRequest(reponse);
            //        }
            //        slotList.Add(slot);
            //    }


            //}


            Booking booking = new Booking();
            List<BookingDetail> bookingDetails = new List<BookingDetail>();

            foreach (var dte in bookingDates)
            {
                booking.UserId = 3;
                booking.BookingCreatedDate = dte.BookingDate;
                booking.Price = 10000;
               
                // Tạo ma trận
                Dictionary<int, int[]> searchMatrix =  await CreateSearchMatrix(areasInRoom, dte.BookingDate.Date);
                int[] slotArray = new int[dte.SlotId.Count];
                for(int i = 0; i < slotArray.Length; i++)
                {
                    var x = await _slotService.GetById(dte.SlotId[i]);
                    slotArray[i] = x.SlotNumber;
                }
                int[][] slotJaggedMatrix = CreateSlotJaggedMatrix(slotArray);
                Tuple<bool, string, Dictionary<int, int[]>> findPositionResult = findPosition(slotJaggedMatrix, searchMatrix);
                if(findPositionResult.Item1 == false)
                {
                    var reponse = new ResponseDTO<object>(400, findPositionResult.Item2, null);
                    return BadRequest(reponse);
                }
                else
                {
                    var allSlot = await _slotService.GetAll();
                    if (areasInRoom.First().AreaType.AreaCategory == 1)
                    {
                        
                        foreach (var item in findPositionResult.Item3)
                        {
                            for(int i = 0; i < item.Value.Length; i++)
                            {
                                var bookingDetail = new BookingDetail();
                                int id = item.Key;
                               
                                bookingDetail.PositionId = id;
                                var slot = allSlot.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
                                bookingDetail.SlotId = slot.SlotId;
                                bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
                                if(i == item.Value.Length - 1)
                                {
                                    bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                }else
                                bookingDetail.CheckoutTime = null;

                                var areaBooks = await _areaService.GetAllWithInclude(x => x.AreaType, x => x.Positions);
                                var areaBook = areaBooks.FirstOrDefault(x => x.Positions.FirstOrDefault(x => x.PositionId == id) != null);
                                bookingDetail.Price = areaBook.AreaType.Price;
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

                                var slot = allSlot.FirstOrDefault(x => x.SlotNumber == item.Value[i]);
                                bookingDetail.SlotId = slot.SlotId;
                                bookingDetail.CheckinTime = dte.BookingDate.Date.Add(slot.StartTime.Value);
                                if (i == item.Value.Length - 1)
                                {
                                    bookingDetail.CheckoutTime = dte.BookingDate.Date.Add(slot.EndTime.Value);
                                }else
                                bookingDetail.CheckoutTime = null;
                                var areaBooks = await _areaService.GetAllWithInclude(x => x.AreaType);
                                var areaBook = areaBooks.FirstOrDefault(x => x.AreaId == id);
                                bookingDetail.Price = areaBook.AreaType.Price;
                                bookingDetails.Add(bookingDetail);
                            }
                        }

                    }
                       
                }
                
            }

           
            booking.BookingDetails = bookingDetails;



                
            await _bookingService.Add(booking);
            return Ok();
        }

        private Tuple<bool, string, Dictionary<int, int[]>> findPosition(int[][] slotJaggedMatrix, Dictionary<int, int[]> searchMatrix)
        {
            if (slotJaggedMatrix == null || searchMatrix == null)
            return new Tuple<bool, string, Dictionary<int, int[]>>(false, "Lỗi khi đặt phòng", null);
            List<int[]> listOfJaggedMatrix = slotJaggedMatrix.ToList();
            listOfJaggedMatrix.Sort((a, b) => b.Length.CompareTo(a.Length));
            
            //kết quả
            Dictionary<int, int[]> dictResult = new Dictionary<int, int[]>();

            //Duyệt listJaggedMatrix
            for(int i = 0; i < listOfJaggedMatrix.Count; i++)
            {
                //tìm ra key + slotNumber 
                bool find = false;

                List<FilterPos> filterPos = new List<FilterPos>();
                foreach(var item in searchMatrix)
                {
                    bool check = true;
                    for(int j = 0; j < listOfJaggedMatrix[i].Length; j++)
                    {
                        if( item.Value[listOfJaggedMatrix[i][j] - 1] == 0)
                        {
                            check = false; break;
                        }
                    }
                    if (check)
                    {
                        //find sizeOffFrag
                        int size = listOfJaggedMatrix[i].Length;
                        // dời trái/ dời phải
                        for(int k = listOfJaggedMatrix[i][0] - 1; k >= 0; k--)
                        {
                            if (item.Value[k] == 1)
                                size++;
                            else
                                break;
                        }
                        for(int h = listOfJaggedMatrix[i][listOfJaggedMatrix[i].Length -1] - 1; h < item.Value.Length; h++)
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
                    searchMatrix.Remove(bestFitPos.Key);
                    dictResult.Add(bestFitPos.Key, bestFitPos.slotNums);
                }
                else return new Tuple<bool, string, Dictionary<int, int[]>>(false, "Lỗi khi đặt phòng", null);
            }

            return new Tuple<bool, string, Dictionary<int, int[]>>(true, "", dictResult);
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
        private async Task<Dictionary<int, int[]>> CreateSearchMatrix(IEnumerable<Area> areasInRoom, DateTime date)
        {
            if (areasInRoom == null) return null;
            var listOfSlot = await _slotService.GetAll();
            if(areasInRoom.FirstOrDefault() != null)
            {
                Dictionary<int, int[]> dict = new Dictionary<int, int[]>();
                if(areasInRoom.FirstOrDefault().AreaType.AreaCategory == 1)
                {
                    var individualArea = areasInRoom.FirstOrDefault();

                    

                    foreach(var pos in individualArea.Positions)
                    {
                        int[] slotNumber = listOfSlot.Select(x => x.SlotNumber).ToArray();
                        Array.Sort(slotNumber);
                        KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(pos.PositionId, slotNumber);
                        keyValuePair =  await FillDataInToKeyValuePair(keyValuePair, 1, date.Date);
                        dict.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    return dict;

                }
                else
                {
                    
                    foreach (var are in areasInRoom)
                    {
                        int[] slotNumber = listOfSlot.Select(x => x.SlotNumber).ToArray();
                        Array.Sort(slotNumber);
                        KeyValuePair<int, int[]> keyValuePair = new KeyValuePair<int, int[]>(are.AreaId, slotNumber);
                        keyValuePair = await FillDataInToKeyValuePair(keyValuePair, 2,date);
                        dict.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    return dict;
                }
            }

            return null;
        }

        private async Task<KeyValuePair<int, int[]>> FillDataInToKeyValuePair( KeyValuePair<int, int[]> keyValuePair, int v, DateTime date)
        {
            if (v < 1 || v > 2)
                return new KeyValuePair<int, int[]>(0,new int[0]);

            var bookingDetail= await _bookDetailService.GetAllWithInclude(x => x.Slot);

            var bookingDetailIn_Date = bookingDetail.Where(x => x.CheckinTime.Date == date.Date);
            if (v == 1)
            {
                //Cá nhân lấy posNumber
                for(int i = 0; i < keyValuePair.Value.Length; i++)
                {
                    if (bookingDetailIn_Date.FirstOrDefault(x => x.PositionId == keyValuePair.Key && x.Slot.SlotNumber == keyValuePair.Value[i] ) != null)
                    {
                        keyValuePair.Value[i] = 0;
                        continue;
                    }
                    keyValuePair.Value[i] = 1;
                }

            }else
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

        //[HttpPost("test")]
        //public async Task<IActionResult> TestBooking()
        //{
        //    BookingDetail b1 = await _bookDetailService.GetById(58);
        //    b1.Price = 1;
        //   await _bookDetailService.Update(b1);
        //    return Ok();
        //}
    }

    public class FilterPos
    {
        public int Key { get; set; }
        public int[] slotNums { get; set; }
        public int sizeOfFrag { get; set; }
    }


    
}
