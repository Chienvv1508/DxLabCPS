using DXLAB_Coworking_Space_Booking_System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ISlotService : IGenericService<Slot>
    {
        Task<List<Slot>> CreateSlots(TimeSpan startTime, TimeSpan endTime, int? breakTime = 10); // Genarate slot
        Task AddMany(List<Slot> slots); // Add many slot 
    }
}
