using System;
using System.Security.Claims;
using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
    }

    // On a normal page the user comes from the web request (HttpContext).
    // Inside a live Blazor component there is no web request, so we ask Blazor who is logged in.
    private ClaimsPrincipal? User
    {
        get
        {
            if (_httpContextAccessor.HttpContext is not null)
                return _httpContextAccessor.HttpContext.User;
            try
            {
                return _authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult().User;
            }
            catch (InvalidOperationException)
            {
                return null; // not inside Blazor either, so nobody is logged in
            }
        }
    }

    public long? UserId => Convert.ToInt64(User?.FindFirstValue(ClaimTypes.NameIdentifier));

    public string UserName => Convert.ToString(User?.FindFirstValue(ClaimTypes.Name));

    public string FullName => Convert.ToString(User?.FindFirstValue(ClaimTypes.Actor));

    public bool IsInRole(string roleName) => User?.IsInRole(roleName) ?? false;

    public string Email => Convert.ToString(User?.FindFirstValue(ClaimTypes.Email));
}