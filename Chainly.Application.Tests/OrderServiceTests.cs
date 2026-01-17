using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Orders;
using Chainly.Application.Services.Implementation;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using static Chainly.Application.DTOs.GeoapifyResponse;

namespace Chainly.Application.Tests
{
    public class OrderServiceTests
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IAzureOpenAIService> _mockAzureOpenAIService;
        private readonly Mock<IExternalApiService> _mockExternalApiService;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mockAzureOpenAIService = new Mock<IAzureOpenAIService>();
            _mockExternalApiService = new Mock<IExternalApiService>();

            _orderService = new OrderService(_mockAzureOpenAIService.Object, _dbContext, _mockExternalApiService.Object);
        }

        [Fact]
        public async Task GetOrderUpdateAsync_ReturnsMessage_WhenOrderExists()
        {
            // Arrange
            var order = new Order { CompanyOrderId = 1, CompanyId = 100, Code = "O-001" };
            _dbContext.Orders.Add(order);
            _dbContext.OrderTracking.Add(new OrderTracking
            {
                OrderId = order.CompanyOrderId,
                CompanyId = 100,
                Status = OrderStatus.Pending,
                Notes = "Test tracking",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            _mockAzureOpenAIService
                .Setup(s => s.GenerateOrderUpdateMessageAsync(It.IsAny<string>()))
                .ReturnsAsync("AI: Order is pending");

            // Act
            var result = await _orderService.GetOrderUpdateAsync("125654",99);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("pending", result.Message);
        }

        [Fact]
        public async Task GetOrderUpdateAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _orderService.GetOrderUpdateAsync("123458",99);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Order not found", result.Message);
        }

        [Fact]
        public async Task GetPagedOrdersAsync_ReturnsPagedResult()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _dbContext.Orders.Add(new Order { CompanyOrderId = i, CompanyId = 200, Code = $"O-{i}" });
            }
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _orderService.GetPagedOrdersAsync(200, 1, 3);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);

            var totalCount = doc.RootElement.GetProperty("TotalCount").GetInt32();
            var items = doc.RootElement.GetProperty("Items").EnumerateArray().ToList();

            Assert.Equal(5, totalCount);
            Assert.Equal(3, items.Count);
        }

        [Fact]
        public async Task CreateOrderAsync_ReturnsOk_WhenValid()
        {
            // Arrange
            var order = new Order { CompanyOrderId = 10, CompanyId = 300, Code = "O-10" };
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var dto = new OrderTrackingDto
            {
                OrderId = 10,
                Latitude = 10,
                Longitude = 20,
                Status = OrderStatus.Pending,
                Notes = "First tracking"
            };

            // Act
            var result = await _orderService.CreateOrderAsync(dto, 300, 1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Order tracking created successfully.", ok.Value);
        }
        [Fact]
        public async Task CreateOrderAsync_ReturnsBadRequest_WhenDuplicateStatus()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                CompanyOrderId = 500,
                CompanyId = 77,
                Code = "ORD-XYZ"
            };
            await _dbContext.Orders.AddAsync(order);

            var tracking = new OrderTracking
            {
                OrderId = order.CompanyOrderId,
                CompanyId = order.CompanyId,
                Status = OrderStatus.Pending,
                Notes = "First tracking"
            };
            await _dbContext.OrderTracking.AddAsync(tracking);
            await _dbContext.SaveChangesAsync();

            var dto = new OrderTrackingDto
            {
                OrderId = order.CompanyOrderId,
                Status = OrderStatus.Pending,
                Notes = "Duplicate attempt",
                Latitude = 10,
                Longitude = 20
            };

            // Act
            var result = await _orderService.CreateOrderAsync(dto, order.CompanyId, 1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order tracking with the same status already exists.", badRequestResult.Value);
        }
        [Fact]
        public async Task UpdateOrderAsync_UpdatesOrder_WhenExists()
        {
            // Arrange
            var order = new Order { CompanyOrderId = 30, CompanyId = 500, Code = "OldCode" };
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var dto = new UpdateOrderDto { Code = "NewCode", Phone = "123", Email = "test@mail.com" };

            // Act
            var result = await _orderService.UpdateOrderAsync(30, dto);

            // Assert
            Assert.True(result.IsSuccess);
            var updated = await _dbContext.Orders.FirstOrDefaultAsync(o => o.CompanyOrderId == 30);
            Assert.Equal("NewCode", updated.Code);
        }
        [Fact]
        public async Task UpdateOrderTrackingAsync_UpdatesTracking_WhenExists()
        {
            // Arrange
            var tracking = new OrderTracking
            {
                Id = 100,
                OrderId = 40,
                CompanyId = 600,
                Status = OrderStatus.Pending,
                Notes = "Before update"
            };
            _dbContext.OrderTracking.Add(tracking);
            await _dbContext.SaveChangesAsync();

            var dto = new UpdateOrderTrackingDto
            {
                Status = OrderStatus.Delivered,
                Notes = "Delivered",
                Latitude = 1,
                Longitude = 2
            };

            // Act
            var result = await _orderService.UpdateOrderTrackingAsync(100, dto);

            // Assert
            Assert.True(result.IsSuccess);
            var updated = await _dbContext.OrderTracking.FindAsync(100);
            Assert.Equal(OrderStatus.Delivered, updated.Status);
        }
        [Fact]
        public async Task GetOrderTrackingByIdAsync_ReturnsOrder_WhenExists()
        {
            // Arrange
            var user = new User { Id = 1, FullName = "Test User" };
            _dbContext.Users.Add(user);

            var order = new Order { Id = 70, CompanyOrderId = 70, CompanyId = 700, Code = "O-70" };
            _dbContext.Orders.Add(order);

            var tracking = new OrderTracking
            {
                Id = 200,
                OrderId = 70,
                CompanyId = 700,
                Status = OrderStatus.Pending,
                Latitude = 10,
                Longitude = 20,
                UpdatedBy = 1,
                UpdatedByNavigation = user,
                Notes = "Tracking"
            };
            _dbContext.OrderTracking.Add(tracking);
            await _dbContext.SaveChangesAsync();

            _mockExternalApiService
                .Setup(s => s.GetAsync<GeoapifyResponse>(It.IsAny<string>()))
                .ReturnsAsync(new GeoapifyResponse
                {
                    Features = new List<Feature>
                    {
                    new Feature { Properties = new Properties { Formatted = "Test Location" } }
                    }
                });

            // Act
            var result = await _orderService.GetOrderTrackingByIdAsync(70, 700);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<OrderDto>(ok.Value);
            Assert.Equal("Test Location", dto.Trackings.First().Location);
        }

        [Fact]
        public async Task CreateOrderAsync_ReturnsBadRequest_WhenOrderDoesNotExist()
        {
            // Arrange
            var dto = new OrderTrackingDto
            {
                OrderId = 99,
                Latitude = 0,
                Longitude = 0,
                Status = OrderStatus.Pending,
                Notes = "Not found order"
            };

            // Act
            var result = await _orderService.CreateOrderAsync(dto, 999, 1);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order does not exist.", bad.Value);
        }

        [Fact]
        public async Task UpdateOrderAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var dto = new UpdateOrderDto { Code = "DoesNotExist" };

            // Act
            var result = await _orderService.UpdateOrderAsync(12345, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Order not found.", result.Message);
        }

        [Fact]
        public async Task UpdateOrderTrackingAsync_ReturnsNotFound_WhenTrackingDoesNotExist()
        {
            // Arrange
            var dto = new UpdateOrderTrackingDto
            {
                Latitude = 10,
                Longitude = 20,
                Status = OrderStatus.Cancelled,
                Notes = "Tracking not found"
            };

            // Act
            var result = await _orderService.UpdateOrderTrackingAsync(999, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Order tracking not found.", result.Message);
        }

        [Fact]
        public async Task GetOrderTrackingByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _orderService.GetOrderTrackingByIdAsync(999, 1000);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Order not found.", notFound.Value);
        }

        [Fact]
        public async Task GetOrdersSummaryAsync_ReturnsCorrectSummary()
        {
            // Arrange
            var order1 = new Order { CompanyOrderId = 1, CompanyId = 800, Code = "O-1" };
            var order2 = new Order { CompanyOrderId = 2, CompanyId = 800, Code = "O-2" };
            await _dbContext.Orders.AddRangeAsync(order1, order2);
            await _dbContext.SaveChangesAsync();

            _dbContext.OrderTracking.AddRange(
                new OrderTracking { OrderId = order1.Id, CompanyId = 800, Status = OrderStatus.Pending, Notes = "Test tracking" },
                new OrderTracking { OrderId = order1.Id, CompanyId = 800, Status = OrderStatus.Delivered, Notes = "Test tracking" },
                new OrderTracking { OrderId = order2.Id, CompanyId = 800, Status = OrderStatus.Cancelled, Notes = "Test tracking" }
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _orderService.GetOrdersSummaryAsync(800);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var summary = ok.Value;


            Assert.Equal(1, (int)summary.GetType().GetProperty("DeliveredOrders").GetValue(summary));
            Assert.Equal(1, (int)summary.GetType().GetProperty("PendingOrders").GetValue(summary));
            Assert.Equal(1, (int)summary.GetType().GetProperty("CancelledOrders").GetValue(summary));
            Assert.Equal(2, (int)summary.GetType().GetProperty("TotalPlacedOrders").GetValue(summary));
        }
    }
}
