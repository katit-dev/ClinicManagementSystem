using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.Enums;

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
    // ACTION STATE
    // =====================================================

    public bool IsSubmitting
    {
        get;
        private set;
    }


    public string ActionMessage
    {
        get;
        private set;
    } = string.Empty;


    public string ActionErrorMessage
    {
        get;
        private set;
    } = string.Empty;

    // =====================================================
    // FINALIZE STATE
    // =====================================================

    public bool IsFinalizing
    {
        get;
        private set;
    }


    public InvoiceDTO? FinalizedInvoice
    {
        get;
        private set;
    }


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
    // FINALIZE MEDICAL RECORD
    //
    // POST:
    // /api/medical-records/{id}/finalize
    //
    // Response:
    // InvoiceDTO
    //
    // Không gửi request body.
    // Backend tự validate:
    // - Diagnosis
    // - CLS
    // - MedicalRecord status
    // - tạo Invoice
    // - finalize MedicalRecord
    // - complete Appointment
    // =====================================================

    public async Task<bool> FinalizeMedicalRecordAsync(
        int medicalRecordId)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsFinalizing)
        {
            return false;
        }


        // =================================================
        // VALIDATE ID
        // =================================================

        if (medicalRecordId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được bệnh án.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // PREVENT FINALIZE AGAIN
        // =================================================

        if (MedicalRecord != null &&
            MedicalRecord.Status ==
                (byte)MedicalRecordStatus.Finalized)
        {
            ActionErrorMessage =
                "Bệnh án đã được chốt.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START FINALIZE
        // =================================================

        IsFinalizing = true;

        FinalizedInvoice = null;

        ActionMessage =
            string.Empty;

        ActionErrorMessage =
            string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/medical-records/{medicalRecordId}/finalize",
                        new StringContent(string.Empty)
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<InvoiceDTO?>>();


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể chốt bệnh án.";

                return false;
            }


            // =================================================
            // CHECK INVOICE
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Chốt bệnh án thành công nhưng không nhận được hóa đơn.";

                return false;
            }


            // =================================================
            // SAVE FINALIZED INVOICE
            // =================================================

            FinalizedInvoice =
                responseData.Content;


            // =================================================
            // UPDATE LOCAL MEDICAL RECORD STATE
            //
            // API đã finalize thành công.
            //
            // Đổi state local để UI:
            // - hiển thị "Đã chốt"
            // - khóa form
            // - khóa Save Draft
            // - khóa Finalize
            // =================================================

            if (MedicalRecord != null)
            {
                MedicalRecord.Status =
                    (byte)MedicalRecordStatus.Finalized;
            }


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;

            return true;
        }
        catch
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsFinalizing = false;

            StateHasChanged();
        }
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
    // SAVE MEDICAL RECORD DRAFT
    //
    // PUT:
    // /api/medical-records/{id}
    //
    // Request:
    // MedicalRecordDraftRequestDTO
    //
    // Response:
    // MedicalRecordDraftResponseDTO
    //
    // Bao gồm:
    // - Medical Record
    // - Patient Vital
    // =====================================================

    public async Task<bool> SaveDraftAsync(
        MedicalRecordDraftRequestDTO request)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // CHECK MEDICAL RECORD
        // =================================================

        if (MedicalRecordId == null ||
            MedicalRecordId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được bệnh án.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START SUBMIT
        // =================================================

        IsSubmitting = true;

        ActionMessage =
            string.Empty;

        ActionErrorMessage =
            string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // CREATE REQUEST CONTENT
            // =================================================

            var content =
                JsonContent.Create(request);


            // =================================================
            // CALL API
            //
            // PUT:
            // /api/medical-records/{id}
            // =================================================

            var response =
                await _authorizedApiService
                    .PutAsync(
                        $"/api/medical-records/{MedicalRecordId}",
                        content
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            MedicalRecordDraftResponseDTO?>>();


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể lưu bệnh án.";

                return false;
            }


            // =================================================
            // EMPTY CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được dữ liệu bệnh án sau khi lưu.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            //
            // Backend trả:
            // MedicalRecordDraftResponseDTO
            //
            // State đang dùng:
            // MedicalRecordExamDTO
            //
            // → map response sang Exam DTO
            // =================================================

            MedicalRecord =
                MapDraftResponseToExam(
                    responseData.Content
                );


            // =================================================
            // SUCCESS MESSAGE
            // =================================================

            ActionMessage =
                responseData.Message;

            return true;
        }
        catch
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsSubmitting = false;

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

    // =====================================================
    // MAP DRAFT RESPONSE → EXAM DTO
    // =====================================================

    private static MedicalRecordExamDTO
        MapDraftResponseToExam(
            MedicalRecordDraftResponseDTO draft)
    {
        return new MedicalRecordExamDTO
        {
            Id =
                draft.Id,

            AppointmentId =
                draft.AppointmentId,

            DoctorName =
                draft.DoctorName,

            Symptoms =
                draft.Symptoms,

            Diagnosis =
                draft.Diagnosis,

            Icd10Code =
                draft.Icd10Code,

            TreatmentPlan =
                draft.TreatmentPlan,

            Note =
                draft.Note,

            FollowUpDate =
                draft.FollowUpDate,

            Status =
                draft.Status,

            FinalizedAt =
                draft.FinalizedAt,

            CreatedAt =
                draft.CreatedAt,

            Vital =
                draft.Vitals == null
                    ? null
                    : new PatientVitalDTO
                    {
                        Id =
                            draft.Vitals.Id,

                        MedicalRecordId =
                            draft.Vitals.MedicalRecordId,

                        Temperature =
                            draft.Vitals.Temperature,

                        Pulse =
                            draft.Vitals.Pulse,

                        BloodPressure =
                            draft.Vitals.BloodPressure,

                        Weight =
                            draft.Vitals.Weight,

                        Height =
                            draft.Vitals.Height
                    }
        };
    }
}