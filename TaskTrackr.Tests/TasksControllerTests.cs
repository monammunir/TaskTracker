using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TaskTrackr.Controllers;
using TaskTrackr.Data;
using TaskTrackr.Models;
using Xunit;

namespace TaskTrackr.Tests
{
    public class TasksControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _controller = new TasksController(_context);

            SetUser();
            SetTempData();
        }


        // -----------------------------------------
        // Fake logged-in user
        // -----------------------------------------

        private void SetUser(
            string userId = "test-user-1")
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId),

                new Claim(
                    ClaimTypes.Name,
                    "test@example.com")
            };

            var identity = new ClaimsIdentity(
                claims,
                "TestAuthentication");

            var principal =
                new ClaimsPrincipal(identity);

            _controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext
                        {
                            User = principal
                        }
                };
        }


        // -----------------------------------------
        // Fake TempData
        // -----------------------------------------

        private void SetTempData()
        {
            _controller.TempData =
                new TempDataDictionary(
                    new DefaultHttpContext(),
                    new TestTempDataProvider());
        }


        // -----------------------------------------
        // TEST 1
        // Index only returns current user's tasks
        // -----------------------------------------

        [Fact]
        public async Task Index_ReturnsOnlyCurrentUsersTasks()
        {
            // Arrange

            _context.TaskItems.AddRange(
                new TaskItem
                {
                    Id = 1,
                    Title = "My Task",
                    Description = "My description",
                    DueDate = DateTime.Today.AddDays(1),
                    Priority = "High",
                    Status = "Pending",
                    UserId = "test-user-1"
                },

                new TaskItem
                {
                    Id = 2,
                    Title = "Other User Task",
                    Description = "Other description",
                    DueDate = DateTime.Today.AddDays(2),
                    Priority = "Low",
                    Status = "Pending",
                    UserId = "other-user"
                }
            );

            await _context.SaveChangesAsync();


            // Act

            var result =
                await _controller.Index(
                    null,
                    null,
                    null);


            // Assert

            var viewResult =
                Assert.IsType<ViewResult>(result);

            var tasks =
                Assert.IsAssignableFrom<IEnumerable<TaskItem>>(
                    viewResult.Model);

            Assert.Single(tasks);

            Assert.Equal(
                "My Task",
                tasks.First().Title);

            var statuses = Assert.IsAssignableFrom<IEnumerable<string>>(viewResult.ViewData["Statuses"]);
            Assert.Equal(new[] { "Pending", "In Progress", "Completed" }, statuses);

            var priorities = Assert.IsAssignableFrom<IEnumerable<string>>(viewResult.ViewData["Priorities"]);
            Assert.Equal(new[] { "Low", "Medium", "High" }, priorities);
        }


        // -----------------------------------------
        // TEST 2
        // Create valid task
        // -----------------------------------------

        [Fact]
        public async Task Create_ValidTask_SavesTask()
        {
            // Arrange

            var task = new TaskItem
            {
                Title = "Learn ASP.NET",
                Description = "Learn MVC and EF Core",
                DueDate = DateTime.Today.AddDays(5),
                Priority = "High",
                Status = "Pending"
            };


            // Act

            var result =
                await _controller.Create(task);


            // Assert

            var redirect =
                Assert.IsType<RedirectToActionResult>(
                    result);

            Assert.Equal(
                "Index",
                redirect.ActionName);

            var savedTask =
                await _context.TaskItems
                    .FirstOrDefaultAsync();

            Assert.NotNull(savedTask);

            Assert.Equal(
                "Learn ASP.NET",
                savedTask!.Title);

            Assert.Equal(
                "test-user-1",
                savedTask.UserId);

            Assert.Equal(
                "High",
                savedTask.Priority);
        }


        // -----------------------------------------
        // TEST 3
        // Create invalid task
        // -----------------------------------------

        [Fact]
        public async Task Create_InvalidTask_ReturnsView()
        {
            // Arrange

            var task = new TaskItem
            {
                Title = "",
                Description = "Invalid task",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "Medium",
                Status = "Pending"
            };

            _controller.ModelState.AddModelError(
                "Title",
                "Title is required.");


            // Act

            var result =
                await _controller.Create(task);


            // Assert

            var viewResult =
                Assert.IsType<ViewResult>(result);

            Assert.Equal(
                task,
                viewResult.Model);
        }


        // -----------------------------------------
        // TEST 4
        // Complete task
        // -----------------------------------------

        [Fact]
        public async Task Complete_ChangesStatusToCompleted()
        {
            // Arrange

            var task = new TaskItem
            {
                Id = 1,
                Title = "Complete this",
                Description = "Test task",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "Medium",
                Status = "Pending",
                UserId = "test-user-1"
            };

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();


            // Act

            var result =
                await _controller.Complete(1);


            // Assert

            var redirect =
                Assert.IsType<RedirectToActionResult>(
                    result);

            Assert.Equal(
                "Index",
                redirect.ActionName);

            var updatedTask =
                await _context.TaskItems
                    .FirstAsync(t => t.Id == 1);

            Assert.Equal(
                "Completed",
                updatedTask.Status);

            Assert.Equal(
                "Task marked as completed.",
                _controller.TempData["SuccessMessage"]);
        }


        // -----------------------------------------
        // TEST 5
        // Delete current user's task
        // -----------------------------------------

        [Fact]
        public async Task Delete_RemovesUserTask()
        {
            // Arrange

            var task = new TaskItem
            {
                Id = 1,
                Title = "Delete this",
                Description = "Test task",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "Low",
                Status = "Pending",
                UserId = "test-user-1"
            };

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();


            // Act

            var result =
                await _controller.DeleteConfirmed(1);


            // Assert

            var redirect =
                Assert.IsType<RedirectToActionResult>(
                    result);

            Assert.Equal(
                "Index",
                redirect.ActionName);

            var deletedTask =
                await _context.TaskItems
                    .FirstOrDefaultAsync(
                        t => t.Id == 1);

            Assert.Null(deletedTask);

            Assert.Equal(
                "Task deleted successfully.",
                _controller.TempData["SuccessMessage"]);
        }


        // -----------------------------------------
        // TEST 6
        // Cannot delete another user's task
        // -----------------------------------------

        [Fact]
        public async Task Delete_DoesNotDeleteAnotherUsersTask()
        {
            // Arrange

            var task = new TaskItem
            {
                Id = 1,
                Title = "Other user's task",
                Description = "Should remain",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "Medium",
                Status = "Pending",
                UserId = "other-user"
            };

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();


            // Act

            var result =
                await _controller.DeleteConfirmed(1);


            // Assert

            Assert.IsType<NotFoundResult>(
                result);

            var remainingTask =
                await _context.TaskItems
                    .FirstOrDefaultAsync(
                        t => t.Id == 1);

            Assert.NotNull(remainingTask);
        }


        // -----------------------------------------
        // TEST 7
        // Cannot complete another user's task
        // -----------------------------------------

        [Fact]
        public async Task Complete_DoesNotChangeAnotherUsersTask()
        {
            // Arrange

            var task = new TaskItem
            {
                Id = 1,
                Title = "Other user's task",
                Description = "Should remain pending",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "Medium",
                Status = "Pending",
                UserId = "other-user"
            };

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();


            // Act

            var result =
                await _controller.Complete(1);


            // Assert

            Assert.IsType<NotFoundResult>(
                result);

            var unchangedTask =
                await _context.TaskItems
                    .FirstAsync(
                        t => t.Id == 1);

            Assert.Equal(
                "Pending",
                unchangedTask.Status);
        }
    }


    // =========================================
    // Fake TempData Provider for Unit Tests
    // =========================================

    public class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object?> _data =
            new Dictionary<string, object?>();


        public IDictionary<string, object?> LoadTempData(
            HttpContext context)
        {
            return _data;
        }


        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object?> values)
        {
            _data.Clear();

            foreach (var item in values)
            {
                _data[item.Key] = item.Value;
            }
        }
    }
}