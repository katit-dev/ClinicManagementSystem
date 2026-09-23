using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.PatientRecord;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// PATIENT RECORD STATE SERVICE
// =====================================================

public class PatientRecordStateService
{
    private readonly AuthorizedApiService _authorizedApiService;


    // =====================================================
    // STATE
    // =====================================================

    public List<PatientRecordDTO> Records { get; private set; } = new();

    public bool IsLoading { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public Action? OnChange { get; set; }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientRecordStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }


    // =====================================================
    // LOAD PATIENT RECORDS
    // =====================================================

    public async Task LoadRecordsAsync()
    {
        IsLoading = true;

        ErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            //
            // GET:
            // /api/patients/me/records
            // =================================================

            var response = await _authorizedApiService.GetAsync(
                "/api/patients/me/records"
            );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content.ReadFromJsonAsync<
                    HttpResponseData<List<PatientRecordDTO>>>();


            // =================================================
            // FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                Records.Clear();

                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải lịch sử khám.";

                return;
            }


            // =================================================
            // SUCCESS
            // =================================================

            Records = responseData.Content
                ?? new List<PatientRecordDTO>();
        }
        catch
        {
            Records.Clear();

            ErrorMessage =
                "Không thể kết nối đến hệ thống.";
        }
        finally
        {
            IsLoading = false;

            StateHasChanged();
        }
    }


    // =====================================================
    // RESET
    // =====================================================

    public void Reset()
    {
        Records.Clear();

        IsLoading = false;

        ErrorMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // STATE CHANGED
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}