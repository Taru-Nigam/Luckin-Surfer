using System;
using System.Threading.Tasks;
using GameCraft.Controllers;
using GameCraft.Data;
using GameCraft.Models;
using GameCraft.Services;
using GameCraft.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using Xunit;

namespace GameCraftTesting
{
    public class AccountControllerTests
    {
        private GameCraftDbContext GetDbContextWithCustomer(string otp, DateTime otpTime)
        {
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new GameCraftDbContext(options);
            context.Customers.Add(new Customer
            {
                Email = "test@example.com",
                Otp = otp,
                OtpGeneratedTime = otpTime,
                IsEmailVerified = false,
                Name = "Test User",
                PasswordHash = "fakehash",
                Salt = "fakesalt"
            });
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task VerifyOtp_InvalidOtp_ReturnsErrorView()
        {
            // Create a virtual database）
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Ensure uniqueness each time.
                .Options;

            // Use test DbContext
            using var context = new TestGameCraftDbContext(options);

            // add new user
            context.Customers.Add(new Customer
            {
                Email = "test@example.com",
                Otp = "123456",
                OtpGeneratedTime = DateTime.UtcNow.AddMinutes(-2), // Not yet expired
                IsEmailVerified = false,
                Name = "Test User",
                PasswordHash = "fakehash",
                Salt = "fakesalt"
            });
            context.SaveChanges();

            var mockEmail = new Mock<IEmailService>();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var controller = new AccountController(context, mockEnv.Object, mockEmail.Object);

            var model = new VerifyOtpViewModel
            {
                Email = "test@example.com",
                Otp = "000000" // Wrong OTP
            };

            var result = await controller.VerifyOtp(model);

            //Validation Result
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(string.IsNullOrEmpty(viewResult.ViewName) || viewResult.ViewName == "VerifyOtp");
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task VerifyOtp_WithValidOtp_ShouldRedirectToLogin()
        {
            // Arrange
            string email = "test@example.com";
            string otp = "123456";
            DateTime otpTime = DateTime.UtcNow;

            var context = TestHelper.GetDbContextWithCustomer(email, otp, otpTime);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");

            var mockEmailService = new Mock<IEmailService>();

            var controller = new AccountController(context, mockEnv.Object, mockEmailService.Object);

            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempDataProvider;

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("mocked-url");
            controller.Url = mockUrlHelper.Object;
           
            httpContext.Session = new DummySession();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var model = new VerifyOtpViewModel
            {
                Email = email,
                Otp = otp
            };

            // Act
            var result = await controller.VerifyOtp(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }
    }
}
