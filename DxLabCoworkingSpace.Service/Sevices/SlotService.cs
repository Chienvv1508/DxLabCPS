
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
            

            var existingSlots = (await _unitOfWork.SlotRepository.GetAll()).OrderBy(s => s.StartTime).ToList();
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
                        Status = 1,
                        SlotNumber = slotNumber++
                    });
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

        async Task IGenericService<Slot>.Add(Slot entity)
        {
            var existingSlots = await _unitOfWork.SlotRepository.GetAll();
            if (existingSlots.Any(s => s.StartTime < entity.EndTime && s.EndTime > entity.StartTime))
            {
                throw new InvalidOperationException("Slot mới bị chèn vào slot đã tồn tại!");
            }

            await _unitOfWork.SlotRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }
        async Task<IEnumerable<Slot>> IGenericService<Slot>.GetAll()
        {
            var a = await _unitOfWork.SlotRepository.GetAll();
            return a;
        }
        public async Task<Slot> Get(Expression<Func<Slot, bool>> expression)
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<Slot>> GetAll(Expression<Func<Slot, bool>> expression)
        {
            throw new NotImplementedException();
        }
        async Task<Slot> IGenericService<Slot>.GetById(int id)
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
        async Task IGenericService<Slot>.Update(Slot entity)
        {
            throw new NotImplementedException();
        }
        async Task IGenericService<Slot>.Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
