using System.Net.Http.Headers;

namespace ClinicManagementSystem.Web.Services;

public class AuthorizedApiService
{
    private readonly HttpClient _httpClient;

    private readonly UserStateService
        _userStateService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AuthorizedApiService(
        IHttpClientFactory httpClientFactory,
        UserStateService userStateService)
    {
        _httpClient =
            httpClientFactory.CreateClient(
                "ClinicAuthorizedApi"
            );

        _userStateService =
            userStateService;
    }


    // =====================================================
    // PREPARE AUTHORIZATION
    // =====================================================

    private async Task<bool> PrepareAuthorizationAsync()
    {
        // =================================================
        // GET ACCESS TOKEN FROM CURRENT STATE
        // =================================================

        var accessToken =
            _userStateService.AccessToken;


        // =================================================
        // STATE CHƯA ĐƯỢC KHÔI PHỤC
        //
        // Có thể xảy ra khi user F5.
        // =================================================

        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            await _userStateService
                .LoadUserStateAsync();

            accessToken =
                _userStateService.AccessToken;
        }


        // =================================================
        // ACCESS TOKEN VẪN KHÔNG TỒN TẠI
        // =================================================

        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            return false;
        }


        // =================================================
        // SET BEARER TOKEN
        // =================================================

        _httpClient
            .DefaultRequestHeaders
            .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );


        return true;
    }


    // =====================================================
    // GET
    // =====================================================

    public async Task<HttpResponseMessage> GetAsync(
        string requestUri)
    {
        var isAuthorized =
            await PrepareAuthorizationAsync();


        if (!isAuthorized)
        {
            return new HttpResponseMessage(
                System.Net.HttpStatusCode.Unauthorized
            );
        }


        return await _httpClient.GetAsync(
            requestUri
        );
    }


    // =====================================================
    // POST
    // =====================================================

    public async Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent content)
    {
        var isAuthorized =
            await PrepareAuthorizationAsync();


        if (!isAuthorized)
        {
            return new HttpResponseMessage(
                System.Net.HttpStatusCode.Unauthorized
            );
        }


        return await _httpClient.PostAsync(
            requestUri,
            content
        );
    }


    // =====================================================
    // PUT
    // =====================================================

    public async Task<HttpResponseMessage> PutAsync(
        string requestUri,
        HttpContent content)
    {
        var isAuthorized =
            await PrepareAuthorizationAsync();


        if (!isAuthorized)
        {
            return new HttpResponseMessage(
                System.Net.HttpStatusCode.Unauthorized
            );
        }


        return await _httpClient.PutAsync(
            requestUri,
            content
        );
    }

    // =====================================================
    // PATCH
    // =====================================================

    public async Task<HttpResponseMessage> PatchAsync(
        string requestUri,
        HttpContent content)
    {
        var isAuthorized =
            await PrepareAuthorizationAsync();


        if (!isAuthorized)
        {
            return new HttpResponseMessage(
                System.Net.HttpStatusCode.Unauthorized
            );
        }


        return await _httpClient
            .PatchAsync(
                requestUri,
                content
            );
    }


    // =====================================================
    // DELETE
    // =====================================================

    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri)
    {
        var isAuthorized =
            await PrepareAuthorizationAsync();


        if (!isAuthorized)
        {
            return new HttpResponseMessage(
                System.Net.HttpStatusCode.Unauthorized
            );
        }


        return await _httpClient.DeleteAsync(
            requestUri
        );
    }
}