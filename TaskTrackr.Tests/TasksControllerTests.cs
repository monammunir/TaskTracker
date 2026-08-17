using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TaskTrackr.Controllers;
using TaskTrackr.Data;
using TaskTrackr.Models;
using Xunit;

namespace TaskTrackr.Tests
{
    public class TasksControllerTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private TasksController CreateController(
            ApplicationDbContext context,
            string userId = "test-user")
        {
            var store = new Mock<IUserStore<IdentityUser>>();

            var userManager = new Mock<UserManager<IdentityUser>>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            userManager
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);

            var controller = new TasksController(
                context,
                userManager.Object);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId)
            };

            var identity = new ClaimsIdentity(
                claims,
                "TestAuth");

            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = principal
                    }
                };

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithUserTasks()
        {
            using var context = CreateDbContext();

            context.Tasks.AddRange(
                new TaskItem
                {
                    Id = 1,
                    Title = "My Task",
                    Description = "Test",
                    Priority = "High",
                    Status = "Pending",
                    UserId = "test-user"
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Other User Task",
                    Description = "Test",
                    Priority = "Low",
                    Status = "Pending",
                    UserId = "another-user"
                });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.Index(
                null,
                null,
                null,
                null);

            var viewResult = Assert.IsType<ViewResult>(result);

            var model =
                Assert.IsAssignableFrom<IEnumerable<TaskItem>>(
                    viewResult.Model);

            Assert.Single(model);

            Assert.Equal("My Task", model.First().Title);
        }

        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            using var context = CreateDbContext();

            var controller = CreateController(context);

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ValidTask_SavesTask()
        {
            using var context = CreateDbContext();

            var controller = CreateController(context);

            var task = new TaskItem
            {
                Title = "New Test Task",
                Description = "Testing create",
                Priority = "High",
                Status = "Pending"
            };

            var result = await controller.Create(task);

            var redirect =
                Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

            var savedTask =
                await context.Tasks.FirstAsync();

            Assert.Equal(
                "New Test Task",
                savedTask.Title);

            Assert.Equal(
                "test-user",
                savedTask.UserId);
        }

        [Fact]
        public async Task Complete_ChangesStatusToCompleted()
        {
            using var context = CreateDbContext();

            context.Tasks.Add(
                new TaskItem
                {
                    Id = 1,
                    Title = "Complete Me",
                    Priority = "Medium",
                    Status = "Pending",
                    UserId = "test-user"
                });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result =
                await controller.Complete(1);

            var redirect =
                Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

            var task =
                await context.Tasks.FindAsync(1);

            Assert.Equal(
                "Completed",
                task!.Status);
        }

        [Fact]
        public async Task Delete_RemovesUserTask()
        {
            using var context = CreateDbContext();

            context.Tasks.Add(
                new TaskItem
                {
                    Id = 1,
                    Title = "Delete Me",
                    Priority = "Low",
                    Status = "Pending",
                    UserId = "test-user"
                });

            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result =
                await controller.DeleteConfirmed(1);

            var redirect =
                Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

            var task =
                await context.Tasks.FindAsync(1);

            Assert.Null(task);
        }
    }
}