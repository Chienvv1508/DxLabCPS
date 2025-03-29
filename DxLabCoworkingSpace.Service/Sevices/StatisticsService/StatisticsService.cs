using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<StudentRevenueDTO> GetRevenueByStudentGroup(string period, int? year = null, int? month = null, int? week = null)
        {
            var bookingDetails = await _unitOfWork.BookingDetailRepository
                .GetAllWithInclude(
                    bd => bd.Booking,
                    bd => bd.Booking.User
                );

            bookingDetails = ApplyPeriodFilter(bookingDetails.AsQueryable(), period, year, month, week).ToList();

            if (!bookingDetails.Any())
            {
                return new StudentRevenueDTO
                {
                    TotalRevenue = 0,
                    StudentRevenue = 0,
                    StudentPercentage = 0
                };
            }

            var totalRevenue = bookingDetails.Sum(bd => bd.Price);
            var studentRevenue = bookingDetails
                .Where(bd => bd.Booking.User != null && IsStudent(bd.Booking.User))
                .Sum(bd => bd.Price);

            return new StudentRevenueDTO
            {
                TotalRevenue = totalRevenue,
                StudentRevenue = studentRevenue,
                StudentPercentage = totalRevenue > 0 ? Math.Round((double)(studentRevenue / totalRevenue * 100), 2) : 0
            };
        }

        private IQueryable<BookingDetail> ApplyPeriodFilter(IQueryable<BookingDetail> query, string period, int? year, int? month, int? week)
        {
            var now = DateTime.Now;

            // Nếu không có tham số cụ thể, dùng logic cũ
            if (!year.HasValue && !month.HasValue && !week.HasValue)
            {
                switch (period.ToLower())
                {
                    case "tuần":
                        return query.Where(bd => bd.CheckinTime >= now.AddDays(-7) && bd.CheckinTime <= now);
                    case "tháng":
                        return query.Where(bd => bd.CheckinTime >= now.AddMonths(-1) && bd.CheckinTime <= now);
                    case "năm":
                        return query.Where(bd => bd.CheckinTime >= now.AddYears(-1) && bd.CheckinTime <= now);
                    default:
                        return query;
                }
            }

            // Xử lý thời gian cụ thể
            DateTime startDate;
            DateTime endDate;

            switch (period.ToLower())
            {
                case "tuần":
                    if (!year.HasValue || !month.HasValue || !week.HasValue)
                        throw new ArgumentException("Cần cung cấp Year, Month, và Week cho period 'tuần'");
                    startDate = GetFirstDayOfMonth(year.Value, month.Value).AddDays((week.Value - 1) * 7);
                    endDate = startDate.AddDays(7);
                    // Đảm bảo không vượt quá cuối tháng
                    var lastDayOfMonth = new DateTime(year.Value, month.Value, 1).AddMonths(1).AddDays(-1);
                    if (endDate > lastDayOfMonth) endDate = lastDayOfMonth.AddDays(1); // Đến hết ngày cuối tháng
                    break;

                case "tháng":
                    if (!year.HasValue || !month.HasValue)
                        throw new ArgumentException("Cần cung cấp Year và Month cho period 'tháng'");
                    startDate = new DateTime(year.Value, month.Value, 1);
                    endDate = startDate.AddMonths(1);
                    break;

                case "năm":
                    if (!year.HasValue)
                        throw new ArgumentException("Cần cung cấp Year cho period 'năm'");
                    startDate = new DateTime(year.Value, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;

                default:
                    return query;
            }

            return query.Where(bd => bd.CheckinTime >= startDate && bd.CheckinTime < endDate);
        }

        private bool IsStudent(User user)
        {
            return user.RoleId == 3; // Role Student
        }

        // Hàm tính ngày đầu tiên của tháng
        private static DateTime GetFirstDayOfMonth(int year, int month)
        {
            return new DateTime(year, month, 1);
        }
    }
}