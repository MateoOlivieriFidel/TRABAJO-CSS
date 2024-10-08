﻿using Microsoft.AspNetCore.Mvc;
using projectoFInal.Models;
using System.Diagnostics;

namespace projectoFInal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Login", "Acceso");
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public ActionResult CerrarSesion()
        {
            HttpContext.Session.Remove("usuario");

            return RedirectToAction("Login", "Acceso");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
