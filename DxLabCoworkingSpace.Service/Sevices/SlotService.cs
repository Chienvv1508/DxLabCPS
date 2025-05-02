
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
namespace DxLabCoworkingSpace
{
    public class SlotService : ISlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        public SlotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //Create slot
        public async Task<List<Slot>> CreateSlots(TimeSpan startTime, TimeSpan endTime, int?timeSlot, int? breakTime)
        {
           
            List<Slot> slots = new List<Slot>();
            TimeSpan currentStart = startTime;
            double slotDuration = timeSlot ?? throw new ArgumentException(nameof(breakTime), "Time Slot là bắt buộc");
            double breakTimeInMinutes = breakTime ?? throw new ArgumentException(nameof(breakTime), "Break Time là bắt buộc");
            

            var existingSlots = (await _unitOfWork.SlotRepository.GetAll(x => x.ExpiredTime.Date > DateTime.Now.Date)).OrderBy(s => s.StartTime).ToList();
            int maxSlotNumber = existingSlots.Any() ? existingSlots.Max(s => s.SlotNumber) : 0;
            int slotNumber = maxSlotNumber + 1;

            while (currentStart < endTime)
            {
                TimeSpan currentEnd = currentStart.Add(TimeSpan.FromMinutes(slotDuration));
                if (currentEnd > endTime) break;

                var conflictingSlot = existingSlots.FirstOrDefault(s =>
                    s.StartTime <= currentEnd && s.EndTime >= currentStart);

                if (conflictingSlot == null)
                {
                    slots.Add(new Slot
                    {
                        StartTime = currentStart,
                        EndTime = currentEnd,
                        SlotNumber = slotNumber++,
                        ExpiredTime = new DateTime(3000, 1, 1)
                    }); ;
                    currentStart = currentEnd.Add(TimeSpan.FromMinutes(breakTimeInMinutes));
                }
                else
                {
                    TimeSpan conflictingEndTime = conflictingSlot.EndTime.HasValue
                        ? conflictingSlot.EndTime.Value
                        : (conflictingSlot.StartTime.HasValue
                            ? conflictingSlot.StartTime.Value.Add(TimeSpan.FromMinutes(slotDuration))
                            : currentEnd);
                    currentStart = conflictingEndTime.Add(TimeSpan.FromMinutes(breakTimeInMinutes));
                }
            }

            if (slots.Count == 0)
            {
                throw new InvalidOperationException("Không thể tạo được slots vì tất cả khoảng thời gian đều xung đột hoặc không đủ thời gian!");
            }

            return slots;
        }

        //Thêm nhiều slot vào database
        public async Task AddMany(List<Slot> slots)
        {
            try
            {
                foreach (var slot in slots)
                {
                    await _unitOfWork.SlotRepository.Add(slot);
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Lỗi khi thêm slot vào database: " + ex.Message);
            }
        }

        async Task Add(Slot entity)
        {
            var existingSlots = await _unitOfWork.SlotRepository.GetAll();
            if (existingSlots.Any(s => s.StartTime < entity.EndTime && s.EndTime > entity.StartTime))
            {
                throw new InvalidOperationException("Slot mới bị chèn vào slot đã tồn tại!");
            }

            await _unitOfWork.SlotRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }
        public async Task<IEnumerable<Slot>> GetAll()
        {
            return await _unitOfWork.SlotRepository.GetAll();
        }
        public async Task<Slot> Get(Expression<Func<Slot, bool>> expression)
        {
            return await _unitOfWork.SlotRepository.Get(expression);
        }
        public async Task<IEnumerable<Slot>> GetAll(Expression<Func<Slot, bool>> expression)
        {
            return await _unitOfWork.SlotRepository.GetAll(expression);
        }
         public async Task<Slot> GetById(int id)
        {
            return await _unitOfWork.SlotRepository.GetById(id);
        }
        public async Task<IEnumerable<Slot>> GetAllWithInclude(params Expression<Func<Slot, object>>[] includes)
        {
            throw new NotImplementedException();
        }
        public async Task<Slot> GetWithInclude(Expression<Func<Slot, bool>> expression, params Expression<Func<Slot, object>>[] includes)
        {
            throw new NotImplementedException();
        }
         public async Task Update(Slot entity)
        {
            // Kiểm tra slot có tồn tại không
            var existingSlot = await _unitOfWork.SlotRepository.GetById(entity.SlotId);
            if (existingSlot == null)
            {
                throw new InvalidOperationException($"Slot với Id {entity.SlotId} không tìm thấy!");
            }

            // Kiểm tra xem slot có BookingDetail nào không (nếu thay đổi trạng thái sang inactive)
            //if (entity.Status == 0) // Nếu đánh dấu slot là inactive
            //{
            //    var bookingDetails = await _unitOfWork.BookingDetailRepository.GetAll(bd => bd.SlotId == entity.SlotId);
            //    if (bookingDetails.Any())
            //    {
            //        throw new InvalidOperationException($"Slot với ID {entity.SlotId} có {bookingDetails.Count()} đặt chỗ, không thể thay đổi trạng thái thành inactive!");
            //    }
            //}

            // Chỉ cập nhật trạng thái (Status)
            existingSlot.ExpiredTime = entity.ExpiredTime;

            // Lưu thay đổi vào database
            await _unitOfWork.SlotRepository.Update(existingSlot);
            await _unitOfWork.CommitAsync();
        }
         public async Task Delete(int id)
        {
            throw new NotImplementedException();
        }
        Task IGenericeService<Slot>.Add(Slot entity)
        {
            throw new NotImplementedException();
        }

        public async Task<DateTime> GetNewExpiredDate(int id)
        {
            try
            {
                var slot = await _unitOfWork.SlotRepository.Get(x => x.SlotId == id && x.ExpiredTime.Date > DateTime.Now.Date);
                if(slot == null)
                    return new DateTime(3000, 1, 1);
                var bookingDetails = await _unitOfWork.BookingDetailRepository.GetAll(x => x.SlotId == id);


                var lastBooking = bookingDetails.OrderByDescending(x => x.CheckinTime).FirstOrDefault();
                DateTime expiredDate;
                if (lastBooking == null)
                {
                    expiredDate = DateTime.Now.Date.AddDays(1);
                }
                else if (lastBooking.CheckinTime.Date < DateTime.Now.Date)
                {
                    expiredDate = DateTime.Now.Date;
                }
                else
                {
                    expiredDate = lastBooking.CheckinTime.Date.AddDays(1);
                }

                return expiredDate;


            }
            catch (Exception ex)
            {
                return new DateTime(3000, 1, 1);
            }
            
           
        }
    }
}
