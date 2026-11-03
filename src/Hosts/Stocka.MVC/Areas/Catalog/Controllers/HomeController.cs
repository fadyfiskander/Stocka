using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stocka.MVC.Areas.Catalog.Models;

namespace Stocka.MVC.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [Route("[area]/[controller]")]
    [Authorize(Roles = "Owner")]

    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var persons = new List<Person>();
            for (int i = 1; i <= 100; i++)
                persons.Add(Person.Create($"Name {i}"));

            return View(persons);
        }
    }
}
