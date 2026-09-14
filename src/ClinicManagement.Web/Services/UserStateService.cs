using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace ClinicManagement.Web.Services
{
    public class UserStateService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;

        public string AccessToken { get; private set; } = "";

        public Action? OnChange { get; set; }


        public UserStateService(
            ILocalStorageService localStorageService,
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager)
        {
            _localStorageService = localStorageService;

            _httpClient =
                httpClientFactory.CreateClient("ClinicApi");

            _navigationManager = navigationManager;
        }


        // =====================================================
        // Thông báo cho component rằng state đã thay đổi
        // =====================================================
        public void StateHasChanged()
        {
            OnChange?.Invoke();
        }


        // =====================================================
        // Lấy token đã lưu trong LocalStorage
        // Dùng khi F5 / mở lại trang
        // =====================================================
        public async Task LoadTokenAsync()
        {
            var token =
                await _localStorageService.GetItemAsync(
                    "accessToken"
                );

            if (!string.IsNullOrEmpty(token))
            {
                AccessToken = token;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token
                    );
            }
            else
            {
                AccessToken = "";
            }

            StateHasChanged();
        }


        // =====================================================
        // LOGOUT
        // =====================================================
        public async Task LogoutAsync()
        {
            AccessToken = "";

            await _localStorageService.RemoveItemAsync(
                "accessToken"
            );

            _httpClient.DefaultRequestHeaders.Authorization = null;

            StateHasChanged();

            _navigationManager.NavigateTo("/login");
        }
    }
}