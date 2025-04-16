using AutoMapper;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestCode
{
    public class StudentBookingHistoryControllerTest
    {
        private readonly Mock<IRoomService> _mockRoomService;
        private readonly Mock<ISlotService> _mockSlotService;
        private readonly Mock<IBookingService> _mockBookingService;
        private readonly Mock<IBookingDetailService> _mockBookingDetailService;
        private readonly Mock<IAreaService> _mockAreaService;
        private readonly Mock<IAreaTypeService> _mockAreaTypeService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly StudentBookingHistoryController _controller;

        public StudentBookingHistoryControllerTest()
        {
            _mockRoomService = new Mock<IRoomService>();
            _mockSlotService = new Mock<ISlotService>();
            _mockBookingService = new Mock<IBookingService>();
            _mockBookingDetailService = new Mock<IBookingDetailService>();
            _mockAreaService = new Mock<IAreaService>();
            _mockAreaTypeService = new Mock<IAreaTypeService>();
            _mockMapper = new Mock<IMapper>();

            _controller = new StudentBookingHistoryController(
                _mockRoomService.Object,
                _mockSlotService.Object,
                _mockBookingService.Object,
                _mockBookingDetailService.Object,
                _mockAreaService.Object,
                _mockAreaTypeService.Object,
                _mockMapper.Object
            );
        }

        private void SetupUserClaims(int? userId, string role = "Student")
        {
            var claims = new List<Claim>();
            if (userId.HasValue)
            {
                claims.Add(new Claim("UserId", userId.Value.ToString()));
            }
            claims.Add(new Claim(ClaimTypes.Role, role));

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // Tests for GET api/studentbookinghistory (GetStudentBookingHistory)
        [Fact]
        public async Task GetStudentBookingHistory_HasBookings_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(1, "Student");
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = 1,
                    UserId = 1,
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 100,
                    User = new User { FullName = "Đức Student", Email = "ductrungnguyen11042002@gmail.com" },
                    BookingDetails = new List<BookingDetail> { new BookingDetail(), new BookingDetail() }
                },
                new Booking
                {
                    BookingId = 2,
                    UserId = 2, // Khác UserId, sẽ bị lọc
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 200,
                    User = new User { FullName = "Other User", Email = "other@example.com" },
                    BookingDetails = new List<BookingDetail> { new BookingDetail() }
                }
            };

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);

            // Act
            var result = await _controller.GetStudentBookingHistory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy danh sách lịch sử booking của bạn thành công!", response.Message);

            // Ép kiểu Data thành IEnumerable<object> và kiểm tra bằng Reflection
            var bookingHistoryList = Assert.IsAssignableFrom<IEnumerable<object>>(response.Data);
            Assert.Single(bookingHistoryList); // Chỉ có 1 booking của userId = 1
            var bookingData = bookingHistoryList.First();

            var bookingId = (int)bookingData.GetType().GetProperty("BookingId").GetValue(bookingData);
            var userName = (string)bookingData.GetType().GetProperty("UserName").GetValue(bookingData);
            var userEmail = (string)bookingData.GetType().GetProperty("UserEmail").GetValue(bookingData);
            var totalPrice = (decimal)bookingData.GetType().GetProperty("TotalPrice").GetValue(bookingData);
            var totalBookingDetail = (int)bookingData.GetType().GetProperty("TotalBookingDetail").GetValue(bookingData);

            Assert.Equal(1, bookingId);
            Assert.Equal("Đức Student", userName);
            Assert.Equal("ductrungnguyen11042002@gmail.com", userEmail);
            Assert.Equal(100, totalPrice);
            Assert.Equal(2, totalBookingDetail);
        }

        [Fact]
        public async Task GetStudentBookingHistory_NoBookings_ReturnsOk()
        {
            // Arrange
            SetupUserClaims(1, "Student");
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = 1,
                    UserId = 2, // Khác UserId, sẽ bị lọc
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 200,
                    User = new User { FullName = "Other User", Email = "other@example.com" },
                    BookingDetails = new List<BookingDetail> { new BookingDetail() }
                }
            };

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);

            // Act
            var result = await _controller.GetStudentBookingHistory();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Bạn không có lịch sử booking nào!", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetStudentBookingHistory_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserClaims(null, "Student"); // Không có UserId trong claims

            // Act
            var result = await _controller.GetStudentBookingHistory();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
            Assert.Equal(401, response.StatusCode);
            Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetStudentBookingHistory_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserClaims(1, "Student");
            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.GetStudentBookingHistory();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi lấy danh sách lịch sử booking", response.Message);
            Assert.Equal("Lỗi hệ thống", response.Data);
        }

        // Tests for GET api/studentbookinghistory/{bookingId} (GetStudentBookingHistoryDetail)
        [Fact]
        public async Task GetStudentBookingHistoryDetail_ValidBooking_ReturnsOk()
        {
            // Arrange
            int bookingId = 1;
            SetupUserClaims(1, "Student");
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = bookingId,
                    UserId = 1,
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 100,
                    User = new User { FullName = "Đức Student", Email = "ductrungnguyen11042002@gmail.com" },
                    BookingDetails = new List<BookingDetail>()
                }
            };
            var bookingDetails = new List<BookingDetail>
            {
                new BookingDetail
                {
                    BookingDetailId = 1,
                    BookingId = bookingId,
                    Position = new Position { PositionNumber = 1, AreaId = 1 },
                    Slot = new Slot { SlotNumber = 1 },
                    CheckinTime = DateTime.UtcNow,
                    CheckoutTime = DateTime.UtcNow.AddHours(1),
                    Price = 50,
                    Status = 0
                }
            };
            var areas = new List<Area>
            {
                new Area
                {
                    AreaId = 1,
                    AreaName = "khu vực A",
                    AreaTypeId = 1,
                    Room = new Room { RoomName = "Al101" }
                }
            };
            var areaTypes = new List<AreaType>
            {
                new AreaType { AreaTypeId = 1, AreaTypeName = "Khu vực cá nhân" }
            };

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);
            _mockBookingDetailService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<BookingDetail, object>>[]>()))
                .ReturnsAsync(bookingDetails);
            _mockAreaService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Area, object>>[]>()))
                .ReturnsAsync(areas);
            _mockAreaTypeService.Setup(s => s.GetAll(It.IsAny<Expression<Func<AreaType, bool>>>()))
                .ReturnsAsync(areaTypes);

            // Act
            var result = await _controller.GetStudentBookingHistoryDetail(bookingId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy chi tiết booking của bạn thành công!", response.Message);

            // Sử dụng Reflection để truy cập các thuộc tính của response.Data
            var responseData = response.Data;
            var bookingIdFromData = (int)responseData.GetType().GetProperty("BookingId").GetValue(responseData);
            var userName = (string)responseData.GetType().GetProperty("UserName").GetValue(responseData);
            var userEmail = (string)responseData.GetType().GetProperty("UserEmail").GetValue(responseData);
            var totalPrice = (decimal)responseData.GetType().GetProperty("TotalPrice").GetValue(responseData);

            Assert.Equal(bookingId, bookingIdFromData);
            Assert.Equal("Đức Student", userName);
            Assert.Equal("ductrungnguyen11042002@gmail.com", userEmail);
            Assert.Equal(100, totalPrice);

            // Truy cập danh sách Details và kiểm tra bằng Reflection
            var details = (IEnumerable<object>)responseData.GetType().GetProperty("Details").GetValue(responseData);
            Assert.Single(details);
            var detail = details.First();

            var bookingDetailId = (int)detail.GetType().GetProperty("BookingDetailId").GetValue(detail);
            var position = (string)detail.GetType().GetProperty("Position").GetValue(detail);
            var areaName = (string)detail.GetType().GetProperty("AreaName").GetValue(detail);
            var areaTypeName = (string)detail.GetType().GetProperty("AreaTypeName").GetValue(detail);
            var roomName = (string)detail.GetType().GetProperty("RoomName").GetValue(detail);
            var slotNumber = (int?)detail.GetType().GetProperty("SlotNumber").GetValue(detail);
            var price = (decimal?)detail.GetType().GetProperty("Price").GetValue(detail);
            var status = (int?)detail.GetType().GetProperty("Status").GetValue(detail);

            Assert.Equal(1, bookingDetailId);
            Assert.Equal("1", position);
            Assert.Equal("khu vực A", areaName);
            Assert.Equal("Khu vực cá nhân", areaTypeName);
            Assert.Equal("Al101", roomName);
            Assert.Equal(1, slotNumber);
            Assert.Equal(50, price);
            Assert.Equal(0, status);
        }

        [Fact]
        public async Task GetStudentBookingHistoryDetail_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            int bookingId = 1;
            SetupUserClaims(null, "Student"); // Không có UserId trong claims

            // Act
            var result = await _controller.GetStudentBookingHistoryDetail(bookingId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(unauthorizedResult.Value);
            Assert.Equal(401, response.StatusCode);
            Assert.Equal("Bạn chưa đăng nhập hoặc token không hợp lệ!", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetStudentBookingHistoryDetail_BookingNotFound_ReturnsNotFound()
        {
            // Arrange
            int bookingId = 1;
            SetupUserClaims(1, "Student");
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = 2, // Khác bookingId
                    UserId = 1,
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 100,
                    User = new User { FullName = "Đức Student", Email = "ductrungnguyen11042002@gmail.com" },
                    BookingDetails = new List<BookingDetail>()
                }
            };

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);

            // Act
            var result = await _controller.GetStudentBookingHistoryDetail(bookingId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Không tìm thấy booking hoặc bạn không có quyền xem!", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetStudentBookingHistoryDetail_NoBookingDetails_ReturnsNotFound()
        {
            // Arrange
            int bookingId = 1;
            SetupUserClaims(1, "Student");
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = bookingId,
                    UserId = 1,
                    BookingCreatedDate = DateTime.UtcNow,
                    Price = 100,
                    User = new User { FullName = "Đức Student", Email = "ductrungnguyen11042002@gmail.com" },
                    BookingDetails = new List<BookingDetail>()
                }
            };

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);
            _mockBookingDetailService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<BookingDetail, object>>[]>()))
                .ReturnsAsync(new List<BookingDetail>());

            // Act
            var result = await _controller.GetStudentBookingHistoryDetail(bookingId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Không tìm thấy chi tiết booking!", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetStudentBookingHistoryDetail_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            int bookingId = 1;
            SetupUserClaims(1, "Student");

            _mockBookingService.Setup(s => s.GetAllWithInclude(It.IsAny<Expression<Func<Booking, object>>[]>()))
                .ThrowsAsync(new Exception("Lỗi hệ thống"));

            // Act
            var result = await _controller.GetStudentBookingHistoryDetail(bookingId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi lấy chi tiết booking", response.Message);
            Assert.Equal("Lỗi hệ thống", response.Data);
        }
    }
}