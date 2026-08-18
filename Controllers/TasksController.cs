using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackr.Data;
using TaskTrackr.Models;

namespace TaskTrackr.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Tasks
        public async Task<IActionResult> Index(
            string? status,
            string? priority,
            string? search)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var query = _context.TaskItems
                .Where(t => t.UserId == userId)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    (t.Description != null &&
                     t.Description.Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            var tasks = await query
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Statuses = new List<string> { "Pending", "In Progress", "Completed" };
            ViewBag.Priorities = new List<string> { "Low", "Medium", "High" };

            return View(tasks);
        }


        // GET: /Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }


        // GET: /Tasks/Create
        public IActionResult Create()
        {
            return View();
        }


        // POST: /Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Server-side validation
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                ModelState.AddModelError(
                    nameof(task.Title),
                    "Title is required.");
            }

            if (task.DueDate == default)
            {
                ModelState.AddModelError(
                    nameof(task.DueDate),
                    "Please enter a valid due date.");
            }

            if (!ModelState.IsValid)
            {
                return View(task);
            }

            task.UserId = userId;

            if (string.IsNullOrWhiteSpace(task.Status))
            {
                task.Status = "Pending";
            }

            if (string.IsNullOrWhiteSpace(task.Priority))
            {
                task.Priority = "Medium";
            }

            _context.TaskItems.Add(task);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // GET: /Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }


        // POST: /Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TaskItem task)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(task.Title))
            {
                ModelState.AddModelError(
                    nameof(task.Title),
                    "Title is required.");
            }

            if (task.DueDate == default)
            {
                ModelState.AddModelError(
                    nameof(task.DueDate),
                    "Please enter a valid due date.");
            }

            if (!ModelState.IsValid)
            {
                return View(task);
            }

            // Get the actual database record belonging to this user.
            var existingTask = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (existingTask == null)
            {
                return NotFound();
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Priority = task.Priority;
            existingTask.Status = task.Status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // GET: /Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }


        // POST: /Tasks/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            _context.TaskItems.Remove(task);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // POST: /Tasks/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            task.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task marked as completed.";

            return RedirectToAction(nameof(Index));
        }


        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        // Get logged-in user's ID safely
        private string? GetCurrentUserId()
        {
            return User?
                .FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}