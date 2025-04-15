using AutoMapper;
using Castle.Core.Configuration;
using DXLAB_Coworking_Space_Booking_System.Controllers;
using DxLabCoworkingSpace;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestCode
{
    public class SlotControllerTest
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ISlotService> _mockSlotService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly SlotController _controller;

        public SlotControllerTest()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockSlotService = new Mock<ISlotService>();
            _mockMapper = new Mock<IMapper>();
            _controller = new SlotController(_mockSlotService.Object, _mockMapper.Object);
        }

        // Test cho API  CreateSlots

        // UT-01: Valid value
        [Fact]
        public async Task CreateSlots_ValidRequest_ReturnsOkWithSlotDtos()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "07:30:00",
                EndTime = "17:40:00",
                TimeSlot = 140,
                BreakTime = 10
            };

            var slots = new List<Slot>
            {
                new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 },
                new Slot { SlotId = 2, StartTime = TimeSpan.Parse("10:00:00"), EndTime = TimeSpan.Parse("12:20:00"), Status = 1, SlotNumber = 2 },
                new Slot { SlotId = 3, StartTime = TimeSpan.Parse("12:30:00"), EndTime = TimeSpan.Parse("14:50:00"), Status = 1, SlotNumber = 3 },
                new Slot { SlotId = 4, StartTime = TimeSpan.Parse("15:00:00"), EndTime = TimeSpan.Parse("17:20:00"), Status = 1, SlotNumber = 4 }
            };

            var slotDtos = new List<SlotDTO>
            {
                new SlotDTO { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 },
                new SlotDTO { SlotId = 2, StartTime = TimeSpan.Parse("10:00:00"), EndTime = TimeSpan.Parse("12:20:00"), Status = 1, SlotNumber = 2 },
                new SlotDTO { SlotId = 3, StartTime = TimeSpan.Parse("12:30:00"), EndTime = TimeSpan.Parse("14:50:00"), Status = 1, SlotNumber = 3 },
                new SlotDTO { SlotId = 4, StartTime = TimeSpan.Parse("15:00:00"), EndTime = TimeSpan.Parse("17:20:00"), Status = 1, SlotNumber = 4 }
            };

            _mockSlotService.Setup(s => s.CreateSlots(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(slots);
            _mockSlotService.Setup(s => s.AddMany(It.IsAny<List<Slot>>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<IEnumerable<SlotDTO>>(slots)).Returns(slotDtos);

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<SlotDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal(4, response.Data.Count());
            Assert.Equal("4 slots được tạo thành công!", response.Message);
        }

        // UT-02: StartTime < EndTime
        [Fact]
        public async Task CreateSlots_InvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "17:40:00",
                EndTime = "07:30:00",
                TimeSlot = 140,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("StartTime phải sớm hơn EndTime!", response.Message);
        }

        // UT-03 Conflict TimeSlot or dupplicate TimeSlot
        [Fact]
        public async Task CreateSlots_ConflictingSlots_ReturnsConflict()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "08:30:00",
                EndTime = "12:00:00",
                TimeSlot = 140,
                BreakTime = 10
            };

            _mockSlotService.Setup(s => s.CreateSlots(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Không thể tạo được slots vì tất cả khoảng thời gian đều xung đột hoặc không đủ thời gian!"));

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(conflictResult.Value);
            Assert.Equal(409, response.StatusCode);
            Assert.Contains("Không thể tạo được slots", response.Message);
        }

        // UT-04: StartTime = null or empty
        [Fact]
        public async Task CreateSlots_MissingStartTime_Empty_ReturnsBadRequest()
        {
            var request = new SlotGenerationRequest
            {
                StartTime = null,
                EndTime = "12:00:00",
                TimeSlot = 140,
                BreakTime = 10
            };
            var result = await _controller.CreateSlots(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("StartTime là bắt buộc!", response.Message);
        }

        // UT-05: EndTime = null or empty
        [Fact]
        public async Task CreateSlots_MissingEndTime_Empty_ReturnsBadRequest()
        {
            var request = new SlotGenerationRequest
            {
                StartTime = "07:30:00",
                EndTime = null,
                TimeSlot = 60,
                BreakTime = 10
            };
            var result = await _controller.CreateSlots(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("EndTime là bắt buộc!", response.Message);
        }

        // UT-06: StartTime is wrong format time ("HH:mm:ss")
        [Fact]
        public async Task CreateSlots_InvalidStartTimeFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "13:60:00", 
                EndTime = "17:40:00",
                TimeSlot = 140,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("StartTime sai định dạng thời gian!", response.Message);
        }

        // UT-07: EndTime is wrong format time ("HH:mm:ss")
        [Fact]
        public async Task CreateSlots_InvalidEndTimeFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "08:00:00",
                EndTime = "12:60:00", 
                TimeSlot = 140,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("EndTime sai định dạng thời gian!", response.Message);
        }

        // Test cho API  GetAllSlots

        // UT-08 lấy thành công List Slots
        [Fact]
        public async Task GetAllSlots_ReturnsOkWithSlots()
        {
            // Arrange
            var slots = new List<Slot>
            {
                new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 },
                new Slot { SlotId = 2, StartTime = TimeSpan.Parse("10:00:00"), EndTime = TimeSpan.Parse("12:20:00"), Status = 1, SlotNumber = 2 },
                new Slot { SlotId = 3, StartTime = TimeSpan.Parse("12:30:00"), EndTime = TimeSpan.Parse("14:50:00"), Status = 1, SlotNumber = 3 },
                new Slot { SlotId = 4, StartTime = TimeSpan.Parse("15:00:00"), EndTime = TimeSpan.Parse("17:20:00"), Status = 1, SlotNumber = 4 }
            };

            var slotDtos = new List<SlotDTO>
            {
                new SlotDTO { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 },
                new SlotDTO { SlotId = 2, StartTime = TimeSpan.Parse("10:00:00"), EndTime = TimeSpan.Parse("12:20:00"), Status = 1, SlotNumber = 2 },
                new SlotDTO { SlotId = 3, StartTime = TimeSpan.Parse("12:30:00"), EndTime = TimeSpan.Parse("14:50:00"), Status = 1, SlotNumber = 3 },
                new SlotDTO { SlotId = 4, StartTime = TimeSpan.Parse("15:00:00"), EndTime = TimeSpan.Parse("17:20:00"), Status = 1, SlotNumber = 4 }
            };

            _mockSlotService.Setup(s => s.GetAll()).ReturnsAsync(slots);
            _mockMapper.Setup(m => m.Map<IEnumerable<SlotDTO>>(slots)).Returns(slotDtos);

            // Act
            var result = await _controller.GetAllSlots();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<SlotDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy danh sách slot thành công.", response.Message);
            Assert.Equal(2, response.Data.Count());
        }

        // UT-09 Lấy ra List Slots rỗng
        [Fact]
        public async Task GetAllSlots_EmptyList_ReturnsOk()
        {
            // Arrange
            _mockSlotService.Setup(s => s.GetAll()).ReturnsAsync(new List<Slot>());
            _mockMapper.Setup(m => m.Map<IEnumerable<SlotDTO>>(It.IsAny<IEnumerable<Slot>>())).Returns(new List<SlotDTO>());

            // Act
            var result = await _controller.GetAllSlots();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<IEnumerable<SlotDTO>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy danh sách slot thành công.", response.Message);
            Assert.Empty(response.Data);
        }

        // UT-10 Lỗi khi lấy danh sách
        [Fact]
        public async Task GetAllSlots_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockSlotService.Setup(s => s.GetAll()).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllSlots();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi lấy danh sách slot: Database error", response.Message);
        }

        // Tests cho API GetSlotById

        // UT-11 Lấy Slot theo Id
        [Fact]
        public async Task GetSlotById_ValidId_ReturnsOk()
        {
            // Arrange
            var slot = new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            var slotDto = new SlotDTO { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync(slot);
            _mockMapper.Setup(m => m.Map<SlotDTO>(slot)).Returns(slotDto);

            // Act
            var result = await _controller.GetSlotById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<SlotDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Lấy thông tin slot thành công.", response.Message);
            Assert.Equal(1, response.Data.SlotId);
        }

        // UT-12 Không tìm thấy Slot với Id ko tồn tại
        [Fact]
        public async Task GetSlotById_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockSlotService.Setup(s => s.GetById(8)).ReturnsAsync((Slot)null);

            // Act
            var result = await _controller.GetSlotById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Slot với ID 8 không tìm thấy!", response.Message);
        }
        // UT-13 Lỗi khi lấy ra slot theo id

        [Fact]
        public async Task GetSlotById_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockSlotService.Setup(s => s.GetById(2)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetSlotById(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi truy xuất slot: Database error", response.Message);
        }

        // Tests cho API UpdateSlot

        // UT-14 Cập nhật thành công (1 => 0)
        [Fact]
        public async Task UpdateSlot_ValidId_Status1To0_ReturnsOk()
        {
            // Arrange
            var slot = new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            var slotDto = new SlotDTO { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 0, SlotNumber = 1 };
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync(slot);
            _mockSlotService.Setup(s => s.Update(It.IsAny<Slot>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<SlotDTO>(slot)).Returns(slotDto);

            // Act
            var result = await _controller.UpdateSlot(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<SlotDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Cập nhật trạng thái slot thành công! Trạng thái mới: 0", response.Message);
            Assert.Equal(0, response.Data.Status);
        }

        [Fact]
        // UT-15 Cập nhật thành công (0 => 1)
        public async Task UpdateSlot_ValidId_Status0To1_ReturnsOk()
        {
            // Arrange
            var slot = new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 0, SlotNumber = 1 };
            var slotDto = new SlotDTO { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync(slot);
            _mockSlotService.Setup(s => s.Update(It.IsAny<Slot>())).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<SlotDTO>(slot)).Returns(slotDto);

            // Act
            var result = await _controller.UpdateSlot(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<SlotDTO>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("Cập nhật trạng thái slot thành công! Trạng thái mới: 1", response.Message);
            Assert.Equal(1, response.Data.Status);
        }

        [Fact]
        // UT-16 Không tìm thấy id để cập nhật
        public async Task UpdateSlot_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync((Slot)null);

            // Act
            var result = await _controller.UpdateSlot(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(notFoundResult.Value);
            Assert.Equal(404, response.StatusCode);
            Assert.Equal("Slot với ID 1 không tìm thấy!", response.Message);
        }

        [Fact]
        // UT-17 Conflict
        public async Task UpdateSlot_Conflict_ReturnsConflict()
        {
            // Arrange
            var slot = new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync(slot);
            _mockSlotService.Setup(s => s.Update(It.IsAny<Slot>())).ThrowsAsync(new InvalidOperationException("Slot đang được sử dụng, không thể cập nhật!"));

            // Act
            var result = await _controller.UpdateSlot(1);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(conflictResult.Value);
            Assert.Equal(409, response.StatusCode);
            Assert.Equal("Slot đang được sử dụng, không thể cập nhật!", response.Message);
        }

        [Fact]
        // UT-18 Lỗi khi cập nhật
        public async Task UpdateSlot_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var slot = new Slot { SlotId = 1, StartTime = TimeSpan.Parse("07:30:00"), EndTime = TimeSpan.Parse("09:50:00"), Status = 1, SlotNumber = 1 };
            _mockSlotService.Setup(s => s.GetById(1)).ReturnsAsync(slot);
            _mockSlotService.Setup(s => s.Update(It.IsAny<Slot>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateSlot(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(statusCodeResult.Value);
            Assert.Equal(500, response.StatusCode);
            Assert.Equal("Lỗi khi cập nhật slot: Database error", response.Message);
        }
    }
}
