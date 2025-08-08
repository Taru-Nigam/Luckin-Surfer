using System;
using GameCraft.Data;
using GameCraft.Models;
using GameCraftTesting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GameCraftTesting
{
    public static class TestHelper
    {
       
            public static ITempDataDictionary GetTempData(HttpContext httpContext)
            {
                var tempDataProvider = new Mock<ITempDataProvider>();
                var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
                return tempData;
            }

            public static TestGameCraftDbContext GetDbContextWithCustomer(string email, string otp, DateTime otpTime)
        {
            var options = new DbContextOptionsBuilder<GameCraftDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new TestGameCraftDbContext(options);

            context.Customers.Add(new Customer
            {
                Email = email,
                Otp = otp,
                OtpGeneratedTime = otpTime,
                IsEmailVerified = false,
                Name = "Test User",
                PasswordHash = "fakehash",
                Salt = "fakesalt",
                AvatarImageData = new byte[] { 0x01, 0x02 }
            });

            context.SaveChanges();
            return context;
        }
    }
}
