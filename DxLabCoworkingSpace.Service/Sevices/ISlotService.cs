
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ISlotService : IGenericeService<Slot>
    {
        Task<List<Slot>> CreateSlots(TimeSpan startTime, TimeSpan endTime, int? timeSlot, int? breakTime); // Create slot
        Task AddMany(List<Slot> slots); // Add many slot 
        Task<DateTime> GetNewExpiredDate(int id);
    }
}
