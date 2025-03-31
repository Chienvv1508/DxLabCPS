
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ISlotService : IFaciStatusService<Slot>
    {
        Task<List<Slot>> CreateSlots(TimeSpan startTime, TimeSpan endTime, int?timeSlot, int? breakTime); // Create slot
        Task AddMany(List<Slot> slots); // Add many slot 
    }
}
