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
using CleanArchitecture.Application.ContactMessages.Commands.CreateContactUs;
using System.Text.RegularExpressions;

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
    public async Task<IActionResult> SendContactMessage(CreateContactUsCommand command)
    {
        try
        {
            await _mediator.Send(command);
            TempData["ContactResult"] = "پیام شما با موفقیت ارسال شد.";
        }
        catch (CleanArchitecture.Application.Common.Exceptions.ValidationException ex)
        {
            TempData["ContactError"] = string.Join(" ", ex.Errors.SelectMany(e => e.Value));
        }

        return RedirectToAction("Contact");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register()
    {
        // The form has one "نام و نام خانوادگی" field: the first word is the first name, the rest is the last name
        var fullName = Request.Form["FullName"].ToString().Trim();
        var companyName = Request.Form["CompanyName"].ToString().Trim();
        var mobilenumber = ToEnglishDigits(Request.Form["Mobile"].ToString().Trim());
        var address = Request.Form["Address"].ToString().Trim();
        var email = Request.Form["Email"].ToString().Trim();
        var password = Request.Form["Password"].ToString();
        var confirmPassword = Request.Form["ConfirmPassword"].ToString();

        // The same rules as registerRules in wwwroot/js/nms-auth.js.
        // The browser checks first, but someone can skip the browser, so the server checks again.
        var errors = new Dictionary<string, string>();
        if (!Regex.IsMatch(fullName, @"^[؀-ۿ‌ a-zA-Z]+$") || !fullName.Contains(' '))
            errors["FullName"] = "لطفا نام و نام خانوادگی را فقط با حروف و با فاصله وارد کنید";
        if (companyName == "")
            errors["CompanyName"] = "وارد کردن شرکت/دانشگاه/سازمان الزامی است";
        if (!Regex.IsMatch(mobilenumber, @"^09\d{9}$"))
            errors["Mobile"] = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود";
        if (address.Length < 10)
            errors["Address"] = "آدرس باید حداقل ۱۰ حرف باشد";
        if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$"))
            errors["Email"] = "ایمیل درست نیست (مثال: name@site.com)";
        if (password.Length < 8 || !Regex.IsMatch(password, "[a-zA-Z]") || !Regex.IsMatch(password, @"\d"))
            errors["Password"] = "رمز عبور باید حداقل ۸ حرف و شامل حرف انگلیسی و عدد باشد";
        if (confirmPassword != password)
            errors["ConfirmPassword"] = "تکرار رمز عبور با رمز عبور یکسان نیست";

        if (errors.Count > 0)
            return Json(new { success = false, message = "لطفا خطاهای فرم را برطرف کنید", errors });

        // The form has one "نام و نام خانوادگی" field: the first word is the first name, the rest is the last name
        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstName = nameParts[0];
        var lastName = nameParts[1];

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

        // Persian (۰-۹) and Arabic (٠-٩) digits become 0-9, like toEnglishDigits in nms-auth.js
    private static string ToEnglishDigits(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '۰' && chars[i] <= '۹') chars[i] = (char)('0' + chars[i] - '۰');
            else if (chars[i] >= '٠' && chars[i] <= '٩') chars[i] = (char)('0' + chars[i] - '٠');
        }
        return new string(chars);
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