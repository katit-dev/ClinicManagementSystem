using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClinicManagementSystem.Web.Services;

public class UserStateService
{
    private readonly ILocalStorageService _localStorageService;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly CustomAuthenticationStateProvider _authenticationStateProvider;


    // =====================================================
    // STATE CỦA USER
    // =====================================================

    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public AuthUserDTO? CurrentUser { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;


    // =====================================================
    // EVENT THÔNG BÁO STATE THAY ĐỔI
    // =====================================================

    public Action? OnChange { get; set; }

    public void StateHasChanged()
    {
        OnChange?.Invoke();
    }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public UserStateService(
    ILocalStorageService localStorageService,
    IHttpClientFactory httpClientFactory,
    NavigationManager navigationManager,
    CustomAuthenticationStateProvider authenticationStateProvider)
    {
        _localStorageService = localStorageService;

        _httpClient = httpClientFactory.CreateClient("ClinicApi");

        _navigationManager = navigationManager;

        _authenticationStateProvider = authenticationStateProvider;
    }


    // =====================================================
    // LOGIN
    // =====================================================


    public async Task LoginAsync(LoginRequestDTO loginRequest)
    {
        ErrorMessage = string.Empty;

        try
        {
            Console.WriteLine("1. Bat dau goi Login API");

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/api/auth/login",
                    loginRequest
                );

            Console.WriteLine(
                $"2. API Status: {response.StatusCode}"
            );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<AuthResponseDTO>>();

            Console.WriteLine("3. Doc response thanh cong");


            if (responseData == null)
            {
                ErrorMessage =
                    "Không nhận được phản hồi từ hệ thống";

                StateHasChanged();

                return;
            }


            if (!response.IsSuccessStatusCode ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                ErrorMessage = responseData.Message;

                StateHasChanged();

                return;
            }


            Console.WriteLine("4. Login thanh cong");

            var authData = responseData.Content;

            AccessToken = authData.AccessToken;
            RefreshToken = authData.RefreshToken;
            CurrentUser = authData.User;


            Console.WriteLine("5. Bat dau luu LocalStorage");

            await _localStorageService.SetItemAsync(
                "accessToken",
                AccessToken
            );

            await _localStorageService.SetItemAsync(
                "refreshToken",
                RefreshToken
            );


            var currentUserJson =
                JsonSerializer.Serialize(CurrentUser);

            await _localStorageService.SetItemAsync(
                "currentUser",
                currentUserJson
            );

            Console.WriteLine("6. Luu LocalStorage thanh cong");


            _authenticationStateProvider
                .MarkUserAsAuthenticated(CurrentUser);

            Console.WriteLine("7. Authentication state OK");


            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AccessToken
                );


            StateHasChanged();

            Console.WriteLine("8. Redirect");

            RedirectByRole();
        }
        catch (Exception ex)
        {
            Console.WriteLine("======================");
            Console.WriteLine("LOGIN ERROR");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("======================");

            ErrorMessage =
                "Không thể kết nối đến hệ thống. Vui lòng thử lại.";

            StateHasChanged();
        }
    }

    // =====================================================
    // LOAD USER STATE
    // Dùng khi F5 / mở lại website
    // =====================================================

    public async Task LoadUserStateAsync()
    {
        var accessToken =
            await _localStorageService.GetItemAsync<string>(
                "accessToken"
            );

        var refreshToken =
            await _localStorageService.GetItemAsync<string>(
                "refreshToken"
            );

        var currentUserJson =
            await _localStorageService.GetItemAsync<string>(
                "currentUser"
            );


        if (string.IsNullOrWhiteSpace(accessToken))
        {
            ClearState();

            _authenticationStateProvider
                .MarkUserAsLoggedOut();

            return;
        }


        AccessToken = accessToken;

        RefreshToken = refreshToken ?? string.Empty;


        if (!string.IsNullOrWhiteSpace(currentUserJson))
        {
            try
            {
                CurrentUser =
                    JsonSerializer.Deserialize<AuthUserDTO>(
                        currentUserJson
                    );
            }
            catch
            {
                CurrentUser = null;
            }
        }


        if (CurrentUser != null)
        {
            _authenticationStateProvider
                .MarkUserAsAuthenticated(CurrentUser);
        }


        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                AccessToken
            );


        StateHasChanged();
    }

    // =====================================================
    // LOGOUT
    // =====================================================

    public async Task LogoutAsync()
    {
        await _localStorageService.RemoveItemAsync(
            "accessToken"
        );

        await _localStorageService.RemoveItemAsync(
            "refreshToken"
        );

        await _localStorageService.RemoveItemAsync(
            "currentUser"
        );


        ClearState();


        _httpClient.DefaultRequestHeaders.Authorization = null;


        _authenticationStateProvider
            .MarkUserAsLoggedOut();


        StateHasChanged();


        _navigationManager.NavigateTo("/login");
    }

    // =====================================================
    // REDIRECT THEO ROLE
    // =====================================================

    private void RedirectByRole()
    {
        if (CurrentUser == null)
        {
            _navigationManager.NavigateTo("/");

            return;
        }


        if (CurrentUser.Roles.Any(
            role => role.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase)))
        {
            _navigationManager.NavigateTo("/admin");

            return;
        }


        if (CurrentUser.Roles.Any(
            role => role.Equals(
                "Doctor",
                StringComparison.OrdinalIgnoreCase)))
        {
            _navigationManager.NavigateTo("/doctor");

            return;
        }


        if (CurrentUser.Roles.Any(
            role => role.Equals(
                "Receptionist",
                StringComparison.OrdinalIgnoreCase)))
        {
            _navigationManager.NavigateTo("/reception");

            return;
        }


        if (CurrentUser.Roles.Any(
            role => role.Equals(
                "Patient",
                StringComparison.OrdinalIgnoreCase)))
        {
            _navigationManager.NavigateTo("/patient");

            return;
        }


        _navigationManager.NavigateTo("/");
    }


    // =====================================================
    // CLEAR STATE TRONG RAM
    // =====================================================

    private void ClearState()
    {
        AccessToken = string.Empty;

        RefreshToken = string.Empty;

        CurrentUser = null;

        ErrorMessage = string.Empty;
    }
}