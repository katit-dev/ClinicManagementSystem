using System.Security.Claims;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ClinicManagementSystem.Web.Services;

public class CustomAuthenticationStateProvider
    : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorageService;

    private readonly ClaimsPrincipal _anonymous =
        new(new ClaimsIdentity());


    public CustomAuthenticationStateProvider(
        ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }


    // =====================================================
    // LẤY TRẠNG THÁI ĐĂNG NHẬP HIỆN TẠI
    // =====================================================

    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");
        var currentUserJson = await _localStorageService.GetItemAsync<string>("currentUser");


        if (string.IsNullOrWhiteSpace(accessToken) ||
            string.IsNullOrWhiteSpace(currentUserJson))
        {
            return new AuthenticationState(_anonymous);
        }


        try
        {
            var user =
                JsonSerializer.Deserialize<AuthUserDTO>(
                    currentUserJson
                );


            if (user == null)
            {
                return new AuthenticationState(_anonymous);
            }


            var principal = CreateClaimsPrincipal(user);

            return new AuthenticationState(principal);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }


    // =====================================================
    // THÔNG BÁO USER ĐÃ LOGIN
    // =====================================================

    public void MarkUserAsAuthenticated(
        AuthUserDTO user)
    {
        var principal =
            CreateClaimsPrincipal(user);


        NotifyAuthenticationStateChanged(
            Task.FromResult(
                new AuthenticationState(principal)
            )
        );
    }


    // =====================================================
    // THÔNG BÁO USER ĐÃ LOGOUT
    // =====================================================

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged(
            Task.FromResult(
                new AuthenticationState(_anonymous)
            )
        );
    }


    // =====================================================
    // TẠO CLAIMS PRINCIPAL
    // =====================================================

    private ClaimsPrincipal CreateClaimsPrincipal(
        AuthUserDTO user)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new(
                ClaimTypes.Name,
                user.FullName ?? string.Empty
            )
        };


        // Add Role Claims
        foreach (var role in user.Roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role
                )
            );
        }


        // DoctorId nếu user là Doctor
        if (user.DoctorId.HasValue)
        {
            claims.Add(
                new Claim(
                    "DoctorId",
                    user.DoctorId.Value.ToString()
                )
            );
        }


        // PatientId nếu user là Patient
        if (user.PatientId.HasValue)
        {
            claims.Add(
                new Claim(
                    "PatientId",
                    user.PatientId.Value.ToString()
                )
            );
        }


        var identity =
            new ClaimsIdentity(
                claims,
                "ClinicAuthentication"
            );


        return new ClaimsPrincipal(identity);
    }
}