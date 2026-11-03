using Catalog.Contracts.DTOs.Category;
using Catalog.Contracts.UseCases.CategoryCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stocka.MVC.Models;
using System.Diagnostics;

namespace Stocka.MVC.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryCases _categoryCases;

        public CategoriesController(ICategoryCases categoryCases)
        {
            _categoryCases = categoryCases;
        }

        // GET: /Catalog/Categories  or /Catalog/Categories/Index
        // UseCase: ListCategoriesQuery
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Categories";
            var query = new ListCategoriesQuery { OrganizationId = Guid.Empty };
            var categories = await _categoryCases.ListCategoriesAsync(query);
            return View(categories);
        }

        // GET: /Catalog/Categories/Details/{id}
        // UseCase: RetrieveCategoryQuery
        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
                return RedirectToAction(nameof(Error), new { message = "Category ID is required." });

            try
            {
                ViewBag.Title = "Category Details";
                var category = await _categoryCases.RetrieveCategoryAsync(new RetrieveCategoryQuery(id));
                return View(category);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Catalog/Categories/SearchByName?name=...
        // UseCase: RetrieveCategoryByNameQuery
        [HttpGet("SearchByName")]
        public async Task<IActionResult> SearchByName(string? name)
        {
            ViewBag.Title = "Search Category";
            if (string.IsNullOrWhiteSpace(name))
                return View(null);

            try
            {
                var category = await _categoryCases.RetrieveCategoryByNameAsync(new RetrieveCategoryByNameQuery(name));
                return View(category);
            }
            catch (KeyNotFoundException ex)
            {
                ViewBag.NotFound = ex.Message;
                return View(null);
            }
            catch (ArgumentException ex)
            {
                ViewBag.Error = ex.Message;
                return View(null);
            }
        }

        // GET: /Catalog/Categories/Create
        // UseCase: RegisterCategoryCommand (GET)
        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.Title = "Create Category";
            return View(new RegisterCategoryCommand());
        }

        // POST: /Catalog/Categories/Create
        // UseCase: RegisterCategoryCommand
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterCategoryCommand command)
        {
            if (!ModelState.IsValid)
                return View(command);

            try
            {
                var result = await _categoryCases.RegisterCategoryAsync(command);
                if (result is null)
                {
                    ModelState.AddModelError(string.Empty, "Failed to create category.");
                    return View(command);
                }

                TempData["Success"] = $"Category '{result.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ArgumentNullException ex)
            {
                ModelState.AddModelError(nameof(command.Name), ex.Message);
                return View(command);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(command.Name), ex.Message);
                return View(command);
            }
        }

        // GET: /Catalog/Categories/Edit/{id}
        // UseCase: RetrieveCategoryQuery + UpdateCategoryCommand (GET - load current)
        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty)
                return RedirectToAction(nameof(Error), new { message = "Category ID is required." });

            try
            {
                var category = await _categoryCases.RetrieveCategoryAsync(new RetrieveCategoryQuery(id));
                ViewBag.Title = "Edit Category";
                return View(new UpdateCategoryCommand
                {
                    CategoryId = category.Id,
                    Name = category.Name
                });
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Catalog/Categories/Edit
        // UseCase: UpdateCategoryCommand
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateCategoryCommand command)
        {
            if (!ModelState.IsValid)
                return View(command);

            try
            {
                var result = await _categoryCases.UpdateCategoryAsync(command);
                TempData["Success"] = $"Category '{result.Name}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // POST: /Catalog/Categories/Activate/{id}
        // UseCase: ActivateCategoryCommand
        [HttpPost("Activate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Category ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _categoryCases.ActivateCategoryAsync(new ActivateCategoryCommand(id));
                TempData["Success"] = $"Category '{result.Name}' activated.";
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Catalog/Categories/Deactivate/{id}
        // UseCase: DeactivateCategoryCommand
        [HttpPost("Deactivate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Category ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _categoryCases.DeactivateCategoryAsync(new DeactivateCategoryCommand(id));
                TempData["Success"] = $"Category '{result.Name}' deactivated.";
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? message)
        {
            ViewBag.ErrorMessage = message ?? "An error occurred.";
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
