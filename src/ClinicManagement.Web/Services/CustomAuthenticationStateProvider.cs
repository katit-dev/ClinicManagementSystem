using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// CUSTOM AUTHENTICATION STATE PROVIDER
// =====================================================

public class CustomAuthenticationStateProvider
    : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous =
        new(new ClaimsIdentity());

    private AuthenticationState _authenticationState;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public CustomAuthenticationStateProvider()
    {
        _authenticationState =
            new AuthenticationState(_anonymous);
    }


    // =====================================================
    // GET CURRENT AUTHENTICATION STATE
    //
    // Không đọc LocalStorage ở đây.
    // App.razor sẽ khôi phục LocalStorage sau khi
    // Blazor đã interactive.
    // =====================================================

    public override Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        return Task.FromResult(
            _authenticationState
        );
    }


    // =====================================================
    // MARK USER AS AUTHENTICATED
    // =====================================================

    public void MarkUserAsAuthenticated(
        AuthUserDTO user)
    {
        var principal =
            CreateClaimsPrincipal(user);

        _authenticationState =
            new AuthenticationState(principal);

        NotifyAuthenticationStateChanged(
            Task.FromResult(
                _authenticationState
            )
        );
    }


    // =====================================================
    // MARK USER AS LOGGED OUT
    // =====================================================

    public void MarkUserAsLoggedOut()
    {
        _authenticationState =
            new AuthenticationState(_anonymous);

        NotifyAuthenticationStateChanged(
            Task.FromResult(
                _authenticationState
            )
        );
    }


    // =====================================================
    // CREATE CLAIMS PRINCIPAL
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


        // =================================================
        // ROLES
        // =================================================

        foreach (var role in user.Roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role
                )
            );
        }


        // =================================================
        // DOCTOR ID
        // =================================================

        if (user.DoctorId.HasValue)
        {
            claims.Add(
                new Claim(
                    "DoctorId",
                    user.DoctorId.Value.ToString()
                )
            );
        }


        // =================================================
        // PATIENT ID
        // =================================================

        if (user.PatientId.HasValue)
        {
            claims.Add(
                new Claim(
                    "PatientId",
                    user.PatientId.Value.ToString()
                )
            );
        }


        // =================================================
        // IDENTITY
        // =================================================

        var identity =
            new ClaimsIdentity(
                claims,
                "ClinicAuthentication"
            );


        return new ClaimsPrincipal(identity);
    }
}