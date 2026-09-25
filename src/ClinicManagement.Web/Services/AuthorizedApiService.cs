using System.Net;
using System.Net.Http.Headers;

namespace ClinicManagementSystem.Web.Services;


public class AuthorizedApiService
{
    private readonly HttpClient _httpClient;

    private readonly UserStateService
        _userStateService;


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
    // PREPARE TOKEN
    // =====================================================

    private async Task<bool> PrepareAuthorizationAsync()
    {
        var accessToken =
            _userStateService.AccessToken;


        if (string.IsNullOrWhiteSpace(accessToken))
        {
            await _userStateService
                .LoadUserStateAsync();

            accessToken =
                _userStateService.AccessToken;
        }


        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }


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
    // HANDLE RESPONSE
    // =====================================================

    private async Task<HttpResponseMessage>
        SendWithRefreshAsync(
            Func<Task<HttpResponseMessage>> request)
    {

        var response =
            await request();


        // =================================================
        // TOKEN CÒN HẠN
        // =================================================

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }



        // =================================================
        // TOKEN HẾT HẠN
        // THỬ REFRESH
        // =================================================

        var refreshed =
            await _userStateService
                .RefreshTokenAsync();


        if (!refreshed)
        {
            return response;
        }



        // =================================================
        // GẮN TOKEN MỚI
        // =================================================

        _httpClient
            .DefaultRequestHeaders
            .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _userStateService.AccessToken
                );



        // =================================================
        // RETRY 1 LẦN
        // =================================================

        return await request();
    }



    // =====================================================
    // GET
    // =====================================================

    public async Task<HttpResponseMessage> GetAsync(
        string requestUri)
    {
        var authorized =
            await PrepareAuthorizationAsync();


        if (!authorized)
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized
            );
        }


        return await SendWithRefreshAsync(
            () =>
                _httpClient.GetAsync(requestUri)
        );
    }



    // =====================================================
    // POST
    // =====================================================

    public async Task<HttpResponseMessage> PostAsync(
        string requestUri,
        HttpContent content)
    {
        var authorized =
            await PrepareAuthorizationAsync();


        if (!authorized)
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized
            );
        }


        return await SendWithRefreshAsync(
            () =>
                _httpClient.PostAsync(
                    requestUri,
                    content
                )
        );
    }



    // =====================================================
    // PUT
    // =====================================================

    public async Task<HttpResponseMessage> PutAsync(
        string requestUri,
        HttpContent content)
    {
        var authorized =
            await PrepareAuthorizationAsync();


        if (!authorized)
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized
            );
        }


        return await SendWithRefreshAsync(
            () =>
                _httpClient.PutAsync(
                    requestUri,
                    content
                )
        );
    }



    // =====================================================
    // PATCH
    // =====================================================

    public async Task<HttpResponseMessage> PatchAsync(
        string requestUri,
        HttpContent content)
    {
        var authorized =
            await PrepareAuthorizationAsync();


        if (!authorized)
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized
            );
        }


        return await SendWithRefreshAsync(
            () =>
                _httpClient.PatchAsync(
                    requestUri,
                    content
                )
        );
    }



    // =====================================================
    // DELETE
    // =====================================================

    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri)
    {
        var authorized =
            await PrepareAuthorizationAsync();


        if (!authorized)
        {
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized
            );
        }


        return await SendWithRefreshAsync(
            () =>
                _httpClient.DeleteAsync(requestUri)
        );
    }
}