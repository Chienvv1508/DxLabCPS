using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DxLabCoworkingSpace.Core;

namespace DxLabCoworkingSpace
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DetailedRevenueDTO> GetDetailedRevenue(string period, int year, int? month = null)
        {
            var allBookingDetails = await _unitOfWork.BookingDetailRepository
                .GetAllWithInclude(
                    bd => bd.Booking,
                    bd => bd.Booking.User
                );

            var filteredDetails = ApplyPeriodFilter(allBookingDetails.AsQueryable(), period, year, month).ToList();

            if (!filteredDetails.Any())
            {
                return new DetailedRevenueDTO
                {
                    Period = period,
                    Year = year,
                    Month = month,
                    Details = new List<RevenueByPeriodDTO>()
                };
            }

            var result = new DetailedRevenueDTO
            {
                Period = period,
                Year = year,
                Month = month,
                Details = period.ToLower() == "năm"
                    ? GetRevenueByMonth(filteredDetails, year)
                    : GetRevenueByDay(filteredDetails, year, month.Value)
            };

            return result;
        }

        private IQueryable<BookingDetail> ApplyPeriodFilter(IQueryable<BookingDetail> query, string period, int year, int? month)
        {
            DateTime startDate;
            DateTime endDate;

            switch (period.ToLower())
            {
                case "năm":
                    startDate = new DateTime(year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;

                case "tháng":
                    if (!month.HasValue) throw new ArgumentException("tháng là bắt buộc cho period 'tháng'");
                    startDate = new DateTime(year, month.Value, 1);
                    endDate = startDate.AddMonths(1);
                    break;

                default:
                    throw new ArgumentException("Period không hợp lệ, phải là 'năm' hoặc 'tháng'");
            }

            return query.Where(bd => bd.Booking != null && bd.Booking.BookingCreatedDate >= startDate && bd.Booking.BookingCreatedDate < endDate);
        }

        private List<RevenueByPeriodDTO> GetRevenueByMonth(List<BookingDetail> bookingDetails, int year)
        {
            var result = new List<RevenueByPeriodDTO>();
            for (int month = 1; month <= 12; month++)
            {
                var monthDetails = bookingDetails
                    .Where(bd => bd.Booking != null && bd.Booking.BookingCreatedDate.Year == year && bd.Booking.BookingCreatedDate.Month == month)
                    .ToList();

                var totalRevenue = monthDetails.Sum(bd => bd.Price);
                var studentRevenue = monthDetails
                    .Where(bd => bd.Booking.User != null && IsStudent(bd.Booking.User))
                    .Sum(bd => bd.Price);

                result.Add(new RevenueByPeriodDTO
                {
                    PeriodNumber = month,
                    Revenue = new StudentRevenueDTO
                    {
                        TotalRevenue = totalRevenue,
                        StudentRevenue = studentRevenue,
                        StudentPercentage = totalRevenue > 0 ? Math.Round((double)(studentRevenue / totalRevenue * 100), 2) : 0
                    }
                });
            }
            return result;
        }

        private List<RevenueByPeriodDTO> GetRevenueByDay(List<BookingDetail> bookingDetails, int year, int month)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var result = new List<RevenueByPeriodDTO>();
            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayDetails = bookingDetails
                    .Where(bd => bd.Booking != null && bd.Booking.BookingCreatedDate.Year == year && bd.Booking.BookingCreatedDate.Month == month && bd.Booking.BookingCreatedDate.Day == day)
                    .ToList();

                var totalRevenue = dayDetails.Sum(bd => bd.Price);
                var studentRevenue = dayDetails
                    .Where(bd => bd.Booking.User != null && IsStudent(bd.Booking.User))
                    .Sum(bd => bd.Price);

                result.Add(new RevenueByPeriodDTO
                {
                    PeriodNumber = day,
                    Revenue = new StudentRevenueDTO
                    {
                        TotalRevenue = totalRevenue,
                        StudentRevenue = studentRevenue,
                        StudentPercentage = totalRevenue > 0 ? Math.Round((double)(studentRevenue / totalRevenue * 100), 2) : 0
                    }
                });
            }
            return result;
        }

        private bool IsStudent(User user)
        {
            return user.RoleId == 3; // Role Student
        }
    }
}