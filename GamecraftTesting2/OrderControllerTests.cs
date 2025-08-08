using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using GameCraft.Controllers;
using GameCraft.Data;
using GameCraft.Models;
using GameCraft.Services; 
using Microsoft.EntityFrameworkCore;

namespace GameCraft.Tests.Controllers
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task OrderDetail_WithDummyOrderId_ReturnsViewResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase("TestDb_Order")
                .Options;

            using (var context = new TestGameCraftDbContext(options))
            {
                context.Orders.Add(new Order
                {
                    OrderId = 1,
                    CustomerId = 1,
                    OrderDate = DateTime.Now
                });
                context.SaveChanges();

                var mockEmailService = new Mock<IEmailService>();
                var controller = new OrderController(context, mockEmailService.Object);

                // Act
                var result = await controller.OrderDetail(1);  

                // Assert
                Assert.IsType<ViewResult>(result); 
            }
        }


    }
}
