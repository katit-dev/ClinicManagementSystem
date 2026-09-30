using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// DOCTOR EXAM STATE SERVICE
// =====================================================

public class DoctorExamStateService
{
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // MEDICAL RECORD
    // =====================================================

    public MedicalRecordExamDTO? MedicalRecord
    {
        get;
        private set;
    }


    public int? MedicalRecordId
    {
        get;
        private set;
    }


    // =====================================================
    // LOAD STATE
    // =====================================================

    public bool IsLoading
    {
        get;
        private set;
    }


    public string ErrorMessage
    {
        get;
        private set;
    } = string.Empty;


    // =====================================================
    // STATE CHANGE
    // =====================================================

    public event Action? OnChange;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorExamStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // LOAD MEDICAL RECORD
    //
    // GET:
    // /api/medical-records/{id}
    //
    // Response:
    // MedicalRecordExamDTO
    //
    // Bao gồm:
    // - Medical Record
    // - Patient Vital
    // =====================================================

    public async Task<bool> LoadMedicalRecordAsync(
        int medicalRecordId)
    {
        // =================================================
        // RESET
        // =================================================

        MedicalRecord = null;

        MedicalRecordId =
            medicalRecordId;

        ErrorMessage =
            string.Empty;


        // =================================================
        // VALIDATE ID
        // =================================================

        if (medicalRecordId <= 0)
        {
            ErrorMessage =
                "Mã bệnh án không hợp lệ.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/medical-records/{medicalRecordId}"
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            MedicalRecordExamDTO?>>();


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải bệnh án.";

                return false;
            }


            // =================================================
            // EMPTY CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ErrorMessage =
                    "Không tìm thấy bệnh án.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            // =================================================

            MedicalRecord =
                responseData.Content;


            return true;
        }
        catch
        {
            ErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsLoading = false;

            StateHasChanged();
        }
    }


    // =====================================================
    // CLEAR MEDICAL RECORD
    // =====================================================

    public void ClearMedicalRecord()
    {
        MedicalRecord = null;

        MedicalRecordId = null;

        IsLoading = false;

        ErrorMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // STATE CHANGE
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}