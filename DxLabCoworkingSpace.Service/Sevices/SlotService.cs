using DXLAB_Coworking_Space_Booking_System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
namespace DxLabCoworkingSpace.Service.Sevices
{
    public class SlotService : ISlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        public SlotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //Generate slot
        public async Task<List<Slot>> GenerateSlots(TimeSpan startTime, TimeSpan endTime, int? breakTime = 10)
        {
            List<Slot> slots = new List<Slot>();
            TimeSpan currentStart = startTime;
            double breakTimeInMinutes = breakTime ?? 10;
            int slotDuration = 120;

            // Lấy danh sách slot đã có từ database
            var existingSlots = (await _unitOfWork.SlotRepository.GetAll()).ToList();

            // Kiểm tra trước toàn bộ khoảng thời gian để phát hiện chồng chéo
            while (currentStart < endTime)
            {
                TimeSpan currentEnd = currentStart.Add(TimeSpan.FromMinutes(slotDuration));
                if (currentEnd > endTime) break;

                // Sửa logic: coi giao nhau ở biên cũng là xung đột
                var conflictingSlot = existingSlots.FirstOrDefault(s => s.StartTime <= currentEnd && s.EndTime >= currentStart);
                if (conflictingSlot != null)
                {
                    throw new InvalidOperationException("Khoảng thời gian slot trùng với khoảng thời gian slot đã tồn tại!");
                }
                 
                // Nếu không có xung đột, thêm slot vào danh sách
                slots.Add(new Slot
                {
                    StartTime = currentStart,
                    EndTime = currentEnd,
                    Status = 1 // Chưa book
                });

                currentStart = currentEnd.Add(TimeSpan.FromMinutes(breakTimeInMinutes));
            }

            if (slots.Count == 0)
            {
                throw new InvalidOperationException("Không thể tạo được slots vì rằng buộc thời gian!");
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
