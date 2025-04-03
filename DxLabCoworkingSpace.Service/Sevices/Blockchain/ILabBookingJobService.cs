using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ILabBookingJobService
    {
        void ScheduleBookingLogJob();
        Task RunBookingLogJobAsync();
    }
}
