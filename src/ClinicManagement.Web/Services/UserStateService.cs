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
    private readonly CustomAuthenticationStateProvider
        _authenticationStateProvider;


    // =====================================================
    // STATE CỦA USER
    // =====================================================

    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public AuthUserDTO? CurrentUser { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    // =====================================================
    // MESSAGE
    // =====================================================

    public string ErrorMessage { get; private set; }
        = string.Empty;

    public string SuccessMessage { get; private set; }
        = string.Empty;

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

        _httpClient =
            httpClientFactory.CreateClient("ClinicApi");

        _navigationManager = navigationManager;

        _authenticationStateProvider =
            authenticationStateProvider;
    }

    // =====================================================
    // FORGOT PASSWORD
    // =====================================================

    public async Task<bool> ForgotPasswordAsync(
        ForgotPasswordRequestDTO request)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/api/auth/forgot-password",
                    request
                );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<JsonElement>>();


            // =================================================
            // RESPONSE NULL
            // =================================================

            if (responseData == null)
            {
                ErrorMessage =
                    "Không nhận được phản hồi từ hệ thống.";

                StateHasChanged();

                return false;
            }


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData.Message;

                StateHasChanged();

                return false;
            }


            // =================================================
            // SUCCESS
            // =================================================

            SuccessMessage =
                responseData.Message;

            StateHasChanged();

            return true;
        }
        catch
        {
            ErrorMessage =
                "Không thể kết nối đến hệ thống. " +
                "Vui lòng thử lại.";

            StateHasChanged();

            return false;
        }
    }

    // =====================================================
    // REGISTER
    // =====================================================

    public async Task<bool> RegisterAsync(
        UserRegisterDTO request)
    {
        ErrorMessage = string.Empty;

        try
        {
            // =================================================
            // GỌI API REGISTER
            // =================================================

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/api/auth/register",
                    request
                );


            // =================================================
            // ĐỌC RESPONSE
            //
            // Dùng JsonElement vì hiện tại Register chỉ cần
            // StatusCode + Message, chưa cần dùng Content.
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<JsonElement>>();


            // =================================================
            // RESPONSE NULL
            // =================================================

            if (responseData == null)
            {
                ErrorMessage =
                    "Không nhận được phản hồi từ hệ thống.";

                StateHasChanged();

                return false;
            }


            // =================================================
            // REGISTER FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData.Message;

                StateHasChanged();

                return false;
            }


            // =================================================
            // REGISTER SUCCESS
            // =================================================

            ErrorMessage = string.Empty;

            StateHasChanged();

            return true;
        }
        catch
        {
            ErrorMessage =
                "Không thể kết nối đến hệ thống. " +
                "Vui lòng thử lại.";

            StateHasChanged();

            return false;
        }
    }

    // =====================================================
    // LOGIN
    // =====================================================

    public async Task LoginAsync(
        LoginRequestDTO loginRequest)
    {
        ErrorMessage = string.Empty;

        try
        {
            // =================================================
            // GỌI BACKEND API
            // =================================================

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/api/auth/login",
                    loginRequest
                );


            // =================================================
            // ĐỌC RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<AuthResponseDTO>>();


            // =================================================
            // KHÔNG NHẬN ĐƯỢC RESPONSE
            // =================================================

            if (responseData == null)
            {
                ErrorMessage =
                    "Không nhận được phản hồi từ hệ thống";

                StateHasChanged();

                return;
            }


            // =================================================
            // LOGIN THẤT BẠI
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                ErrorMessage =
                    responseData.Message;

                StateHasChanged();

                return;
            }


            // =================================================
            // LOGIN THÀNH CÔNG
            // =================================================

            var authData =
                responseData.Content;


            AccessToken =
                authData.AccessToken;

            RefreshToken =
                authData.RefreshToken;

            CurrentUser =
                authData.User;


            // =================================================
            // LƯU LOCAL STORAGE
            // =================================================

            await _localStorageService.SetItemAsync(
                "accessToken",
                AccessToken
            );


            await _localStorageService.SetItemAsync(
                "refreshToken",
                RefreshToken
            );


            var currentUserJson =
                JsonSerializer.Serialize(
                    CurrentUser
                );


            await _localStorageService.SetItemAsync(
                "currentUser",
                currentUserJson
            );


            // =================================================
            // THÔNG BÁO CHO BLAZOR USER ĐÃ LOGIN
            // =================================================

            _authenticationStateProvider
                .MarkUserAsAuthenticated(
                    CurrentUser
                );


            // =================================================
            // GẮN JWT VÀO HTTP CLIENT
            // =================================================

            _httpClient
                .DefaultRequestHeaders
                .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        AccessToken
                    );


            // =================================================
            // THÔNG BÁO UI STATE ĐÃ THAY ĐỔI
            // =================================================

            StateHasChanged();


            // =================================================
            // CHUYỂN TRANG THEO ROLE
            // =================================================

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
    //
    // Dùng khi:
    // - F5
    // - Mở lại website
    //
    // Khôi phục trạng thái đăng nhập từ LocalStorage
    // =====================================================

    public async Task LoadUserStateAsync()
    {
        var accessToken =
            await _localStorageService
                .GetItemAsync<string>(
                    "accessToken"
                );


        var refreshToken =
            await _localStorageService
                .GetItemAsync<string>(
                    "refreshToken"
                );


        var currentUserJson =
            await _localStorageService
                .GetItemAsync<string>(
                    "currentUser"
                );


        // =================================================
        // KHÔNG CÓ ACCESS TOKEN
        // => CHƯA ĐĂNG NHẬP
        // =================================================

        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            ClearState();


            _authenticationStateProvider
                .MarkUserAsLoggedOut();


            return;
        }


        // =================================================
        // KHÔI PHỤC TOKEN
        // =================================================

        AccessToken =
            accessToken;

        RefreshToken =
            refreshToken ?? string.Empty;


        // =================================================
        // KHÔI PHỤC CURRENT USER
        // =================================================

        if (!string.IsNullOrWhiteSpace(
            currentUserJson))
        {
            try
            {
                CurrentUser =
                    JsonSerializer
                        .Deserialize<AuthUserDTO>(
                            currentUserJson
                        );
            }
            catch
            {
                CurrentUser = null;
            }
        }


        // =================================================
        // KHÔI PHỤC AUTHENTICATION STATE
        // =================================================

        if (CurrentUser != null)
        {
            _authenticationStateProvider
                .MarkUserAsAuthenticated(
                    CurrentUser
                );
        }


        // =================================================
        // GẮN ACCESS TOKEN VÀO HTTP CLIENT
        // =================================================

        _httpClient
            .DefaultRequestHeaders
            .Authorization =
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
        // =================================================
        // XÓA LOCAL STORAGE
        // =================================================

        await _localStorageService.RemoveItemAsync(
            "accessToken"
        );


        await _localStorageService.RemoveItemAsync(
            "refreshToken"
        );


        await _localStorageService.RemoveItemAsync(
            "currentUser"
        );


        // =================================================
        // XÓA STATE TRONG RAM
        // =================================================

        ClearState();


        // =================================================
        // XÓA BEARER TOKEN KHỎI HTTP CLIENT
        // =================================================

        _httpClient
            .DefaultRequestHeaders
            .Authorization = null;


        // =================================================
        // THÔNG BÁO CHO BLAZOR USER ĐÃ LOGOUT
        // =================================================

        _authenticationStateProvider
            .MarkUserAsLoggedOut();


        StateHasChanged();


        // =================================================
        // CHUYỂN VỀ LOGIN
        // =================================================

        _navigationManager.NavigateTo(
            "/login"
        );
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


        // =================================================
        // ADMIN
        // =================================================

        if (CurrentUser.Roles.Any(
            role =>
                role.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )))
        {
            _navigationManager.NavigateTo(
                "/admin"
            );

            return;
        }


        // =================================================
        // DOCTOR
        // =================================================

        if (CurrentUser.Roles.Any(
            role =>
                role.Equals(
                    "Doctor",
                    StringComparison.OrdinalIgnoreCase
                )))
        {
            _navigationManager.NavigateTo(
                "/doctor"
            );

            return;
        }


        // =================================================
        // RECEPTIONIST
        // =================================================

        if (CurrentUser.Roles.Any(
            role =>
                role.Equals(
                    "Receptionist",
                    StringComparison.OrdinalIgnoreCase
                )))
        {
            _navigationManager.NavigateTo(
                "/reception"
            );

            return;
        }


        // =================================================
        // PATIENT
        // =================================================

        if (CurrentUser.Roles.Any(
            role =>
                role.Equals(
                    "Patient",
                    StringComparison.OrdinalIgnoreCase
                )))
        {
            _navigationManager.NavigateTo(
                "/patient"
            );

            return;
        }


        // =================================================
        // KHÔNG XÁC ĐỊNH ĐƯỢC ROLE
        // =================================================

        _navigationManager.NavigateTo("/");
    }


    // =====================================================
    // CLEAR STATE TRONG RAM
    // =====================================================

    private void ClearState()
    {
        AccessToken =
            string.Empty;

        RefreshToken =
            string.Empty;

        CurrentUser =
            null;

        ErrorMessage =
            string.Empty;
    }
}