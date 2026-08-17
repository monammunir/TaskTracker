using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public TasksController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? priority,
            string? sort)
        {
            var userId = _userManager.GetUserId(User);

            var tasks = _context.Tasks
                .Where(t => t.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(t =>
                    t.Title.Contains(search) ||
                    (t.Description != null &&
                     t.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                tasks = tasks.Where(t => t.Priority == priority);
            }

            tasks = sort switch
            {
                "dueDate" => tasks.OrderBy(t => t.DueDate),
                "dueDate_desc" => tasks.OrderByDescending(t => t.DueDate),
                "title" => tasks.OrderBy(t => t.Title),
                _ => tasks.OrderByDescending(t => t.Id)
            };

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            ViewBag.Statuses = new[]
            {
                "Pending",
                "In Progress",
                "Completed"
            };

            ViewBag.Priorities = new[]
            {
                "Low",
                "Medium",
                "High"
            };

            return View(await tasks.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
                return NotFound();

            return View(task);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task)
        {
            if (ModelState.IsValid)
            {
                task.UserId = _userManager.GetUserId(User)!;

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Task created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem task)
        {
            if (id != task.Id)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var existingTask = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (existingTask == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.DueDate = task.DueDate;
                existingTask.Priority = task.Priority;
                existingTask.Status = task.Status;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Task updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Task deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (task == null)
                return NotFound();

            task.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Task marked as completed.";

            return RedirectToAction(nameof(Index));
        }
    }
}