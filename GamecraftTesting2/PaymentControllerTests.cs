using System.Threading.Tasks;
using GameCraft.Controllers;
using GameCraft.Data;
using GameCraft.Models;
using GameCraft.Services;
using GameCraftTesting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Tls;
using Xunit;

namespace GameCraft.Tests.Controllers
{
    public class PaymentControllerTests
    {
        [Fact]
        public async Task StartCardCheckout_WithValidCardIdAndLoggedInUser_RedirectsToCardPayment()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_StartCardCheckout")
                .Options;

            var context = new TestGameCraftDbContext(options);
            var mockLogger = new Mock<ILogger<PaymentController>>();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new PaymentController(context, mockLogger.Object, mockEmailService.Object);

            // fake card
            var card = new Card
            {
                CardId = 1,
                Name = "Test Card",
                Description = "Test description",
                ImageData = new byte[] { 0x01, 0x02 },
                Price = 15.5M
            };
            context.Cards.Add(card);
            context.SaveChanges();

            
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new DummySession();  
            httpContext.Session.SetString("Email", "user@example.com");
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            controller.TempData = TestHelper.GetTempData(httpContext);

            // Act
            var result = await controller.StartCardCheckout(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CardPayment", redirectResult.ActionName);

            Assert.Equal("1", controller.TempData["PurchaseCardId"].ToString());
            Assert.Equal("15.5", controller.TempData["PurchaseCardPrice"].ToString());
            Assert.Equal("Test Card", controller.TempData["PurchaseCardName"].ToString());
        }
    }
}
