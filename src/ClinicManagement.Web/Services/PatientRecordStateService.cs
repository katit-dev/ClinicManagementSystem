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
    // DOWNLOAD ATTACHMENT
    // =====================================================

    public async Task<(byte[] FileBytes, string FileName, string ContentType)?> DownloadAttachmentAsync(
        int attachmentId)
    {
        try
        {
            var response = await _authorizedApiService.GetAsync(
                $"/api/attachments/{attachmentId}/download"
            );

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();

            var fileName =
                response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? $"attachment-{attachmentId}";

            fileName = fileName.Trim('"');

            var contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return (
                fileBytes,
                fileName,
                contentType
            );
        }
        catch
        {
            return null;
        }
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