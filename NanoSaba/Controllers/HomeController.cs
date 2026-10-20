using System.Diagnostics;
using System.Security.Claims;
using Azure.Core;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.HrManagment.People.Commands.CreatePersonItem;
using CleanArchitecture.Application.PagesDto;
using CleanArchitecture.Application.TodoItems.Commands.CreateTodoItem;
using CleanArchitecture.Infrastructure.Identity;
using CleanArchitecture.Infrastructure.Services;
using CleanArchitecture.Infrastructure.Services.SMSProvider.Faraz;
using Common;
using Common.Exceptions;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using DataTransferObjects.SharedModels;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NanoSaba.Models;
using Services.IServices.V2;

namespace NanoSaba.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserCodeService _userCodeService;
    private readonly IUsersService _userService;
    private readonly IMediator _mediator;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<ApplicationUser> _repository;

    public HomeController(ILogger<HomeController> logger,
        IUserCodeService userCodeService,
        IMediator mediator,
        IUsersService userService,
        SignInManager<ApplicationUser> signInManager,
        IRepository<ApplicationUser> repository,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userCodeService = userCodeService;
        _mediator = mediator;
        _userService = userService;
        this.signInManager = signInManager;
        _repository = repository;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var result = await _mediator.Send(new GetArticleWithPaginationQuery()
        {
            RecordsPerPage = 3
        }, CancellationToken.None);

        Request.HttpContext.User = User;
        return View("Index", new IndexDto()
        {
            Articles = result.Items
        });
    }

    [AllowAnonymous]
    public IActionResult SignIn()
    {
       return View();
    }

    [AllowAnonymous]
    public IActionResult About()
    {
    return View();
    }
    [AllowAnonymous]
    public IActionResult Contact()
    {
    return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register()
    {
        // The form has one "نام و نام خانوادگی" field: the first word is the first name, the rest is the last name
        var nameParts = Request.Form["FullName"].ToString()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "";
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";
        var companyName = Request.Form["CompanyName"].ToString();
        var mobilenumber = Request.Form["Mobile"].ToString();
        var email = Request.Form["Email"].ToString();
        var password = Request.Form["Password"].ToString();
        var address = Request.Form["Address"].ToString();

        if (string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) ||
            string.IsNullOrEmpty(mobilenumber) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(address) ||
            string.IsNullOrEmpty(password))
        {
            return AuthError("لطفا همه فیلدها را کامل وارد کنید");
        }

        long result;
        try
        {
            result = await _mediator.Send(new CreatePersonProfileCommand()
            {
                FirstName = firstName,
                LastName = lastName,
                MobileNumbers = new List<CreatePersonMobileNumberCommand>()
                {
                    new CreatePersonMobileNumberCommand()
                    {
                        IsDefault = true,
                        MobileNumber = mobilenumber
                    }
                },
                Addresses = new List<CreatePeopleAddressCommand>()
                {
                    new CreatePeopleAddressCommand()
                    {
                        Address =new CreateAddressCommand()
                        {
                            FullAddress = address,
                            PhoneNumber = mobilenumber
                        },
                        Title = firstName
                    }
                },
                Email = email,
                CompanyName = companyName,
                Password = password
            }, CancellationToken.None);
        }
        catch (AppException exp)
        {
            return AuthError(exp.Message);
        }

        if (result <= 0)
            return AuthError("ثبت نام انجام نشد. لطفا دوباره تلاش کنید");

        return Json(new
        {
            success = true,
            step = "registered",
            mobileNumber = mobilenumber,
            message = "ثبت نام انجام شد. برای ورود، کد تایید را دریافت کنید"
        });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("_token");
        // await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        return await Index();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignInWithCode()
    {
        var number1 = Request.Form["number1"].ToString();
        // Peek keeps the number, so a wrong code can be typed again
        var mobile = TempData.Peek("MobileNumber")?.ToString();
        if (string.IsNullOrEmpty(mobile))
            return AuthError("لطفا دوباره شماره موبایل را وارد کنید");

        try
        {
            var result = await _userCodeService.Token(new CodeWithMobileNumber()
            {
                Code = number1,
                MobileNumber = mobile
            }, CancellationToken.None);
            if (!result.IsSuccess)
                return AuthError(result.Message ?? "کد تایید درست نیست");

            HttpContext.User = result.Data.Identity;
            HttpContext.User.AddIdentity(result.Data.Identity.Identities.FirstOrDefault());
            await userOperation(User.Identity.GetUserId());
            SetCookie("_token", result.Data.access_token, result.Data.expires_in);
        }
        catch (BadRequestException ex)
        {
            return AuthError(ex.Message);
        }

        TempData.Remove("MobileNumber");
        return Json(new { success = true, step = "signedIn" });
    }

    public void SetCookie(string key, string value, int? expireTime)
    {
        CookieOptions option = new CookieOptions();

        if (expireTime.HasValue)
            option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
        else
            option.Expires = DateTime.Now.AddDays(10);

        Response.Cookies.Append(key, value, option);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignInWithUserPass()
    {
        var UserName = Request.Form["Username"].ToString();
        var Password = Request.Form["Password"].ToString();
        try
        {
            var result = await _userService.Login(UserName, Password, CancellationToken.None);
            HttpContext.User = result.Identity;
            HttpContext.User.AddIdentity(result.Identity.Identities.FirstOrDefault());
            await userOperation(User.Identity.GetUserId());
            SetCookie("_token", result.access_token, result.expires_in);
        }
        catch (BadRequestException ex)
        {
            return AuthError(ex.Message);
        }

        return Json(new { success = true, step = "signedIn" });
    }

    private async Task userOperation(long userId)
    {
        var existingUser = _repository.TableNoTracking.FirstOrDefault(x => x.Id == userId);
        if (existingUser is not null)
        {
            var userIdentity = await signInManager.CreateUserPrincipalAsync(existingUser);
            var claimsIdentity = userIdentity.Identity as ClaimsIdentity;
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Actor, existingUser.FullName));
            var userRoles = await _userManager.GetRolesAsync(existingUser);
            foreach (var role in userRoles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            HttpContext.User = userIdentity;
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MobileNumber()
    {
        var mobilenumber = Request.Form["Mobilenumber"].ToString();

        var result = await _userCodeService.GenerateCode(mobilenumber, CancellationToken.None);
        if (result.Data is null)
            return AuthError(result.Message);
        if (!result.Data.IsRegistered)
            return AuthError("این شماره ثبت نشده است. لطفا ابتدا ثبت نام کنید");

        TempData["MobileNumber"] = mobilenumber;
        return Json(new { success = true, step = "code", mobileNumber = mobilenumber });
    }

    // Answer for the login and sign-up forms (sent with fetch from wwwroot/js/nms-auth.js)
    private JsonResult AuthError(string message)
    {
        return Json(new { success = false, message });
    }

    [AllowAnonymous]
    public async Task<IActionResult> AddEmail()
    {
        var email = Request.Form["Email"].ToString();

        var result = await _mediator.Send(new CreateEmailDiscountCommand() { Email = email }
            , CancellationToken.None);

        return View("Index", new IndexDto()
        {
            IsSuccess = result > 0 ? true : false,
            Message = "ایمیل با خطا مواجه شد.",
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}