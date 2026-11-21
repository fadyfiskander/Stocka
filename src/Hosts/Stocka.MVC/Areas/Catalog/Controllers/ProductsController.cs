using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.CategoryCases;
using Catalog.Contracts.UseCases.ProductCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stocka.MVC.Localization;
using Stocka.MVC.Models;
using System.Diagnostics;

namespace Stocka.MVC.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]
    public class ProductsController : Controller
    {
        private readonly IProductCases _productCases;
        private readonly ICategoryCases _categoryCases;

        public ProductsController(IProductCases productCases, ICategoryCases categoryCases)
        {
            _productCases = productCases;
            _categoryCases = categoryCases;
        }

        // GET: /Catalog/Products  or /Catalog/Products/Index
        // UseCase: ListProductsQuery
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = Loc.T("Products_Title");
            var products = await _productCases.ListProductsAsync(new ListProductsQuery { OrganizationId = null });

            if (products == null || !products.Any())
            {
                products = new List<ProductDto>();
                ViewBag.Status = "No Data";
            }

            return View(products);
        }

        // GET: /Catalog/Products/Details/{id}
        // UseCase: RetrieveProductQuery
        [HttpGet("Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
                return Error();

            try
            {
                ViewBag.Title = Loc.T("Products_DetailsTitle");
                var product = await _productCases.RetrieveProductAsync(new RetrieveProductQuery(id));
                return View(product);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or AggregateException)
            {
                return RedirectToAction(nameof(Handler), new
                {
                    HandlerText = Loc.T("Msg_ProductUnavailable"),
                    Title = Loc.T("Msg_ProductUnavailableTitle")
                });
            }
        }

        // GET: /Catalog/Products/SearchByName?name=...
        // UseCase: RetrieveProductByNameQuery
        [HttpGet("SearchByName")]
        public async Task<IActionResult> SearchByName(string? name)
        {
            ViewBag.Title = Loc.T("Products_SearchTitle");
            if (string.IsNullOrWhiteSpace(name))
                return View(null);

            try
            {
                var product = await _productCases.RetrieveProductByNameAsync(new RetrieveProductByNameQuery(name));
                return View(product);
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

        // GET: /Catalog/Products/Create
        // UseCase: RegisterProductCommand (GET)
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            ViewBag.Title = Loc.T("Products_CreateTitle");
            return View(new RegisterProductCommand());
        }

        // POST: /Catalog/Products/Create
        // UseCase: RegisterProductCommand
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterProductCommand command)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(command);
            }

            try
            {
                var createdProduct = await _productCases.RegisterProductAsync(command);
                if (createdProduct is null)
                {
                    await PopulateCategoriesAsync();
                    ModelState.AddModelError(string.Empty, Loc.T("Msg_ProductCreateFailed"));
                    return View(command);
                }

                TempData["Success"] = Loc.F("Msg_ProductCreated", createdProduct.Name);
                return RedirectToAction(nameof(Details), new { id = createdProduct.Id });
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is KeyNotFoundException)
            {
                await PopulateCategoriesAsync();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // GET: /Catalog/Products/Edit/{id}
        // UseCase: RetrieveProductQuery + UpdateProductCommand (GET - load existing)
        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty)
                return Error();

            try
            {
                var product = await _productCases.RetrieveProductAsync(new RetrieveProductQuery(id));
                ViewBag.Title = Loc.T("Products_EditTitle");

                await PopulateCategoriesAsync();
                return View(new UpdateProductCommand
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    CategoryId = product.CategoryId,
                    SellingPrice = product.Price,
                    Barcode = product.Barcode,
                    ImagePath = product.ImagePath
                });
            }
            catch (Exception ex) when (ex is KeyNotFoundException or AggregateException)
            {
                return RedirectToAction(nameof(Handler), new { HandlerText = Loc.T("Msg_ProductNotFound"), Title = Loc.T("Msg_NotFoundTitle") });
            }
        }

        // POST: /Catalog/Products/Edit
        // UseCase: UpdateProductCommand
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProductCommand command)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(command);
            }

            try
            {
                var result = await _productCases.UpdateProductAsync(command);
                TempData["Success"] = Loc.F("Msg_ProductUpdated", result.Name);
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (KeyNotFoundException ex)
            {
                await PopulateCategoriesAsync();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
            catch (ArgumentException ex)
            {
                await PopulateCategoriesAsync();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(command);
            }
        }

        // POST: /Catalog/Products/Activate/{id}
        // UseCase: ActivateProductCommand
        [HttpPost("Activate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = Loc.T("Msg_ProductIdRequired");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _productCases.ActivateProductAsync(new ActivateProductCommand(id));
                TempData["Success"] = Loc.F("Msg_ProductActivated", result.Name);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Catalog/Products/Deactivate/{id}
        // UseCase: DeactivateProductCommand
        [HttpPost("Deactivate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = Loc.T("Msg_ProductIdRequired");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _productCases.DeactivateProductAsync(new DeactivateProductCommand(id));
                TempData["Success"] = Loc.F("Msg_ProductDeactivated", result.Name);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Handler")]
        public IActionResult Handler(string handlerText, string title)
        {
            ViewBag.Title = title;
            return View(handlerText);
        }

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task PopulateCategoriesAsync()
        {
            try
            {
                var categories = await _categoryCases.ListCategoriesAsync(new ListCategoriesQuery { OrganizationId = Guid.Empty });
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
            }
            catch
            {
                ViewBag.Categories = new SelectList(Enumerable.Empty<object>(), "Id", "Name");
            }
        }
    }
}
