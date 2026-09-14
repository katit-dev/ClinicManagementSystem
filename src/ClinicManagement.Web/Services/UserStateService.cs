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
            // Gọi Backend API
            var response = await _httpClient.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest
            );


            // Đọc response theo format chung của Backend
            var responseData =
                await response.Content
                    .ReadFromJsonAsync<HttpResponseData<AuthResponseDTO>>();


            // Nếu Backend không trả được dữ liệu
            if (responseData == null)
            {
                ErrorMessage = "Không nhận được phản hồi từ hệ thống";

                StateHasChanged();

                return;
            }


            // Nếu đăng nhập thất bại
            if (!response.IsSuccessStatusCode ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                ErrorMessage = responseData.Message;

                StateHasChanged();

                return;
            }


            // =================================================
            // LOGIN THÀNH CÔNG
            // =================================================

            var authData = responseData.Content;

            AccessToken = authData.AccessToken;

            RefreshToken = authData.RefreshToken;

            CurrentUser = authData.User;


            // =================================================
            // LƯU TOKEN VÀ USER VÀO LOCAL STORAGE
            // =================================================

            await _localStorageService.SetItemAsync(
                "accessToken",
                AccessToken
            );

            await _localStorageService.SetItemAsync(
                "refreshToken",
                RefreshToken
            );


            // AuthUserDTO là object nên serialize thành JSON
            var currentUserJson =
                JsonSerializer.Serialize(CurrentUser);

            await _localStorageService.SetItemAsync(
                "currentUser",
                currentUserJson
            );

            // Báo cho Blazor biết User đã Login
            _authenticationStateProvider
                .MarkUserAsAuthenticated(CurrentUser);

            // =================================================
            // GẮN JWT VÀO HTTP CLIENT
            // =================================================

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AccessToken
                );


            // Báo cho UI biết state đã thay đổi
            StateHasChanged();


            // Chuyển trang theo role
            RedirectByRole();
        }
        catch (Exception)
        {
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
            await _localStorageService.GetItemAsync(
                "accessToken"
            );

        var refreshToken =
            await _localStorageService.GetItemAsync(
                "refreshToken"
            );

        var currentUserJson =
            await _localStorageService.GetItemAsync(
                "currentUser"
            );


        if (string.IsNullOrWhiteSpace(accessToken))
        {
            ClearState();

            return;
        }


        AccessToken = accessToken;

        RefreshToken = refreshToken ?? string.Empty;


        // Khôi phục CurrentUser
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


        // Gắn JWT vào HttpClient
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