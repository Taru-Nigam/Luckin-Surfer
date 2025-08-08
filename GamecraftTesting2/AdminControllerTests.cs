using GameCraft.Controllers;
using GameCraft.Data;
using GameCraft.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameCraft.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public void Login_Get_ReturnsViewResult()
        {
            // Arrange
            var controller = GetAdminController();

            // Act
            var result = controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Admin Login", controller.ViewData["Title"]);
        }

        [Fact]
        public void Login_Post_WithEmptyAdminKey_ReturnsViewWithError()
        {
            // Arrange
            var controller = GetAdminController();

            // Act
            var result = controller.Login("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        

        // support method
        private AdminController GetAdminController()
        {
            // create DbContext
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;

            var context = new GameCraftDbContext(options);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var mockEmailService = new Mock<IEmailService>();

            return new AdminController(context, mockEnv.Object, mockEmailService.Object);
        }

        private IWebHostEnvironment GetWebHostEnv()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
            return env.Object;
        }

        private Mock<IEmailService> GetMockEmailService()
        {
            return new Mock<IEmailService>();
        }
    }
}