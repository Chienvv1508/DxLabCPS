using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<StudentRevenueDTO> GetRevenueByStudentGroup(string period)
        {
            var bookings = await _unitOfWork.BookingRepository.GetAllWithInclude(b => b.User, b => b.BookingDetails);
            bookings = ApplyPeriodFilter(bookings.AsQueryable(), period);

            var totalRevenue = bookings.Sum(b => b.Price);
            var studentRevenue = bookings
                .Where(b => b.User != null && IsStudent(b.User))
                .Sum(b => b.Price);

            return new StudentRevenueDTO
            {
                TotalRevenue = totalRevenue,
                StudentRevenue = studentRevenue,
                StudentPercentage = totalRevenue > 0 ? (double)(studentRevenue / totalRevenue * 100) : 0
            };
        }

        public async Task<ServiceTypeRevenueDTO> GetRevenueByServiceType(string period)
        {
            var bookingDetails = await _unitOfWork.BookingDetailRepository
                .GetAllWithInclude(bd => bd.Area, bd => bd.Area.AreaType, bd => bd.Booking);
            bookingDetails = ApplyPeriodFilter(bookingDetails.AsQueryable(), period);

            var totalRevenue = bookingDetails.Sum(bd => bd.Price);
            var revenueByType = bookingDetails
                .GroupBy(bd => bd.Area.AreaType.AreaTypeName)
                .Select(g => new ServiceTypeDetailDTO
                {
                    ServiceType = g.Key,
                    Revenue = g.Sum(bd => bd.Price),
                    Percentage = totalRevenue > 0 ? (double)(g.Sum(bd => bd.Price) / totalRevenue * 100) : 0
                })
                .ToList();

            return new ServiceTypeRevenueDTO
            {
                TotalRevenue = totalRevenue,
                ServiceTypes = revenueByType
            };
        }

        public async Task<List<RoomPerformanceDTO>> GetRoomPerformanceByTime(string period)
        {
            var bookingDetails = await _unitOfWork.BookingDetailRepository
                .GetAllWithInclude(bd => bd.Area, bd => bd.Area.Room);
            bookingDetails = ApplyPeriodFilter(bookingDetails.AsQueryable(), period);

            return bookingDetails
                .GroupBy(bd => bd.Area.Room.RoomName)
                .Select(g => new RoomPerformanceDTO
                {
                    RoomName = g.Key,
                    TotalRevenue = g.Sum(bd => bd.Price),
                    BookingCount = g.Count(),
                    AverageRevenuePerBooking = g.Average(bd => bd.Price)
                })
                .ToList();
        }

        public async Task<List<RoomServicePerformanceDTO>> GetRoomPerformanceByServiceTime(string period)
        {
            var bookingDetails = await _unitOfWork.BookingDetailRepository
                .GetAllWithInclude(bd => bd.Area, bd => bd.Area.Room, bd => bd.Area.AreaType);
            bookingDetails = ApplyPeriodFilter(bookingDetails.AsQueryable(), period);

            return bookingDetails
                .GroupBy(bd => new { bd.Area.Room.RoomName, bd.Area.AreaType.AreaTypeName })
                .Select(g => new RoomServicePerformanceDTO
                {
                    RoomName = g.Key.RoomName,
                    ServiceType = g.Key.AreaTypeName,
                    TotalRevenue = g.Sum(bd => bd.Price),
                    BookingCount = g.Count(),
                    AverageRevenuePerBooking = g.Average(bd => bd.Price)
                })
                .ToList();
        }

        private IQueryable<T> ApplyPeriodFilter<T>(IQueryable<T> query, string period) where T : class
        {
            var now = DateTime.Now;
            switch (period.ToLower())
            {
                case "tuần":
                    return query.Where(b => EF.Property<DateTime>(b, "CheckinTime") >= now.AddDays(-7)
                        && EF.Property<DateTime>(b, "CheckinTime") <= now.AddDays(7)); // Thêm giới hạn trên
                case "tháng":
                    return query.Where(b => EF.Property<DateTime>(b, "CheckinTime") >= now.AddMonths(-1)
                        && EF.Property<DateTime>(b, "CheckinTime") <= now.AddMonths(1));
                case "năm":
                    return query.Where(b => EF.Property<DateTime>(b, "CheckinTime") >= now.AddYears(-1)
                        && EF.Property<DateTime>(b, "CheckinTime") <= now.AddYears(1));
                default:
                    return query;
            }
        }

        private bool IsStudent(User user)
        {
            return user.RoleId == 3; // Role Student
        }
    }
}
