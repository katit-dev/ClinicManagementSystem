using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Patient;

namespace ClinicManagementSystem.Web.Services;

public class PatientStateService
{
    private readonly HttpClient _httpClient;


    // =====================================================
    // STATE
    // =====================================================

    public PatientLookupDTO? LookupPatient { get; private set; }

    public string ErrorMessage { get; private set; }
        = string.Empty;


    // =====================================================
    // EVENT
    // =====================================================

    public Action? OnChange { get; set; }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientStateService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClient =
            httpClientFactory.CreateClient("ClinicApi");
    }


    // =====================================================
    // LOOKUP PATIENT BY PHONE
    // =====================================================

    public async Task<bool> LookupByPhoneAsync(
        string phone)
    {
        ErrorMessage = string.Empty;
        LookupPatient = null;

        try
        {
            var encodedPhone =
                Uri.EscapeDataString(phone.Trim());


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _httpClient.GetAsync(
                    $"/api/patients/lookup?phone={encodedPhone}"
                );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<PatientLookupDTO?>>();


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
            // LOOKUP SUCCESS
            //
            // Content có thể:
            //
            // null
            // → chưa có Patient
            //
            // PatientLookupDTO
            // → đã có Patient
            //
            // Cả hai đều là lookup thành công.
            // =================================================

            LookupPatient =
                responseData.Content;


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
    // CLEAR LOOKUP
    // =====================================================

    public void ClearLookup()
    {
        LookupPatient = null;
        ErrorMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // STATE CHANGED
    // =====================================================

    public void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}