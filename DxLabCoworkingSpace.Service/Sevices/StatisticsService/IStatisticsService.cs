using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IStatisticsService
    {
        Task<StudentRevenueDTO> GetRevenueByStudentGroup(string period);
        Task<ServiceTypeRevenueDTO> GetRevenueByServiceType(string period);
        Task<List<RoomPerformanceDTO>> GetRoomPerformanceByTime(string period);
        Task<List<RoomServicePerformanceDTO>> GetRoomPerformanceByServiceTime(string period);
    }
}
