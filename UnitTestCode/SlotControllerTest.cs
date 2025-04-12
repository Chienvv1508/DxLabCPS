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

            //_mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsASecretKeyThatIsAtLeast32BytesLong");
            //_mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            //_mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _controller = new SlotController(_mockSlotService.Object, _mockMapper.Object);
        }

        // Test cho API  CreateSlots
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
            Assert.Equal(2, response.Data.Count());
            Assert.Equal("2 slots được tạo thành công!", response.Message);
        }

        [Fact]
        public async Task CreateSlots_InvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "12:00:00",
                EndTime = "08:00:00",
                TimeSlot = 60,
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

        [Fact]
        public async Task CreateSlots_ConflictingSlots_ReturnsConflict()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "08:00:00",
                EndTime = "12:00:00",
                TimeSlot = 60,
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

        [Fact]
        public async Task CreateSlots_MissingStartTime_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = null,
                EndTime = "12:00:00",
                TimeSlot = 60,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Contains("StartTime là bắt buộc!", response.Message);
        }

        [Fact]
        public async Task CreateSlots_MissingEndTime_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "08:00:00",
                EndTime = null,
                TimeSlot = 60,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Contains("EndTime là bắt buộc!", response.Message);
        }

        [Fact]
        public async Task CreateSlots_InvalidStartTimeFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "25:00:00",
                EndTime = "12:00:00",
                TimeSlot = 60,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Contains("StartTime sai định dạng thời gian!", response.Message);
        }

        [Fact]
        public async Task CreateSlots_InvalidEndTimeFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SlotGenerationRequest
            {
                StartTime = "08:00:00",
                EndTime = "12:60:00",
                TimeSlot = 60,
                BreakTime = 10
            };

            // Act
            var result = await _controller.CreateSlots(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ResponseDTO<object>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.Contains("EndTime sai định dang thời gian!", response.Message);
        }


    }
}
