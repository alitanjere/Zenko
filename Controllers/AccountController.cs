using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (BD.ValidarUsuario(connectionString, username, password))
        {
            HttpContext.Session.SetString("Usuario", username);
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Error = "Credenciales inválidas";
        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
