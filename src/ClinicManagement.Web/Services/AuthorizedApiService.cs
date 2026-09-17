using System.Net.Http.Headers;

namespace ClinicManagementSystem.Web.Services;

public class AuthorizedApiService
{
    private readonly HttpClient _httpClient;

    private readonly UserStateService _userStateService;


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

    private bool PrepareAuthorization()
    {
        // =================================================
        // GET ACCESS TOKEN
        // =================================================

        var accessToken =
            _userStateService.AccessToken;


        // =================================================
        // ACCESS TOKEN KHÔNG TỒN TẠI
        // =================================================

        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            return false;
        }


        // =================================================
        // SET BEARER TOKEN
        //
        // Authorization: Bearer <accessToken>
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
        PrepareAuthorization();

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
        PrepareAuthorization();

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
        PrepareAuthorization();

        return await _httpClient.PutAsync(
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
        PrepareAuthorization();

        return await _httpClient.DeleteAsync(
            requestUri
        );
    }
}