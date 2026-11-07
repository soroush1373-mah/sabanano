using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NanoSaba.Controllers;

// Any address that no MVC page answers comes here, and Blazor shows its page.
public class BlazorController : Controller
{
    [AllowAnonymous]
    public IActionResult Host()
    {
        return View("~/Views/Shared/_BlazorHost.cshtml");
    }
}
