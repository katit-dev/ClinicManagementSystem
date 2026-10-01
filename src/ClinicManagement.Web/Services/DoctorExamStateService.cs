using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Medicine;
using ClinicManagementSystem.Application.DTOs.Service;
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
    // SERVICE CATALOG
    // =====================================================

    public List<ServiceDTO> AvailableServices
    {
        get;
        private set;
    } = new();


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
    // SEARCH MEDICINES
    //
    // GET:
    // /api/medicines/search?keyword={keyword}&patientId={patientId}
    //
    // Response:
    // MedicineSearchDTO[]
    //
    // =====================================================

    public async Task<bool> SearchMedicinesAsync(
        string keyword)
    {
        // =================================================
        // CLEAR OLD RESULT
        // =================================================

        MedicineSearchResults.Clear();

        ActionErrorMessage =
            string.Empty;


        // =================================================
        // VALIDATE MEDICAL RECORD
        // =================================================

        if (MedicalRecord == null)
        {
            ActionErrorMessage =
                "Không xác định được bệnh án.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // GET PATIENT ID
        // =================================================

        if (MedicalRecord.PatientId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được bệnh nhân.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // EMPTY KEYWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(keyword))
        {
            StateHasChanged();

            return true;
        }


        // =================================================
        // START SEARCH
        // =================================================

        IsSearchingMedicines = true;

        StateHasChanged();


        try
        {
            var encodedKeyword =
                Uri.EscapeDataString(
                    keyword.Trim()
                );


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/medicines/search" +
                        $"?keyword={encodedKeyword}" +
                        $"&patientId={MedicalRecord.PatientId}"
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<MedicineSearchDTO>?
                        >
                    >();


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
                    ?? "Không thể tìm thuốc.";

                return false;
            }


            // =================================================
            // UPDATE RESULT
            // =================================================

            MedicineSearchResults =
                responseData.Content
                ?? new List<MedicineSearchDTO>();


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
            IsSearchingMedicines = false;

            StateHasChanged();
        }
    }

    // =====================================================
    // MEDICINE SEARCH
    // =====================================================

    public List<MedicineSearchDTO> MedicineSearchResults
    {
        get;
        private set;
    } = new();


    public bool IsSearchingMedicines
    {
        get;
        private set;
    }

    // =====================================================
    // UPLOAD MEDICAL RECORD ATTACHMENT
    //
    // POST:
    // /api/medical-records/{id}/attachments
    //
    // Request:
    // multipart/form-data
    //
    // Field:
    // file
    //
    // Allowed:
    // jpg
    // jpeg
    // png
    // pdf
    // dcm
    // dicom
    //
    // Max size:
    // 10 MB
    // =====================================================

    public async Task<bool> UploadMedicalRecordAttachmentAsync(
        int medicalRecordId,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // VALIDATE MEDICAL RECORD
        // =================================================

        if (medicalRecordId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được bệnh án.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALIDATE FILE
        // =================================================

        if (fileStream == null ||
            fileStream.Length <= 0 ||
            string.IsNullOrWhiteSpace(fileName))
        {
            ActionErrorMessage =
                "Vui lòng chọn file.";

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
            // CREATE MULTIPART FORM
            // =================================================

            using var form =
                new MultipartFormDataContent();


            // =================================================
            // FILE CONTENT
            // =================================================

            var fileContent =
                new StreamContent(fileStream);


            if (!string.IsNullOrWhiteSpace(contentType))
            {
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        contentType
                    );
            }


            // =================================================
            // IMPORTANT
            //
            // Backend parameter:
            //
            // IFormFile file
            //
            // Therefore multipart name MUST be:
            //
            // "file"
            // =================================================

            form.Add(
                fileContent,
                "file",
                fileName
            );


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/medical-records/{medicalRecordId}/attachments",
                        form
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<AttachmentDTO?>>();


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
                    ?? "Không thể tải file lên.";

                return false;
            }


            // =================================================
            // CHECK RESPONSE
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Tải file thành công nhưng không nhận được dữ liệu file.";

                return false;
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
            IsSubmitting = false;

            StateHasChanged();
        }
    }

    // =====================================================
    // ADD MEDICAL RECORD SERVICE RESULT
    //
    // POST:
    // /api/medical-record-services/{id}/result
    //
    // Request:
    // - ResultValue
    // - ReferenceRange
    // - Conclusion
    // - ResultedAt
    // =====================================================

    public async Task<bool> AddMedicalRecordServiceResultAsync(
        int medicalRecordServiceId,
        string resultValue,
        string? referenceRange,
        string? conclusion,
        DateTime resultedAt)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // VALIDATE SERVICE ID
        // =================================================

        if (medicalRecordServiceId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được chỉ định.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALIDATE RESULT
        // =================================================

        if (string.IsNullOrWhiteSpace(resultValue))
        {
            ActionErrorMessage =
                "Vui lòng nhập kết quả.";

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
            // REQUEST
            // =================================================

            var request =
                new LabResultRequestDTO
                {
                    ResultValue =
                        resultValue,

                    ReferenceRange =
                        referenceRange,

                    Conclusion =
                        conclusion,

                    ResultedAt =
                        resultedAt
                };


            // =================================================
            // CREATE JSON CONTENT
            // =================================================

            var content =
                JsonContent.Create(request);


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/medical-record-services/{medicalRecordServiceId}/result",
                        content
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            MedicalRecordServiceDTO?>>();


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
                    ?? "Không thể lưu kết quả.";

                return false;
            }


            // =================================================
            // SUCCESS MESSAGE
            // =================================================

            ActionMessage =
                responseData.Message;


            // =================================================
            // RELOAD MEDICAL RECORD
            //
            // Để cập nhật:
            // - Status = Completed
            // - Result != null
            // - CompletedAt
            // =================================================

            if (MedicalRecordId.HasValue)
            {
                var reloadSuccess =
                    await LoadMedicalRecordAsync(
                        MedicalRecordId.Value
                    );

                if (!reloadSuccess)
                {
                    ActionErrorMessage =
                        "Đã lưu kết quả nhưng không thể tải lại dữ liệu.";

                    return false;
                }
            }


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
    // CANCEL MEDICAL RECORD SERVICE
    //
    // PATCH:
    // /api/medical-record-services/{id}/cancel
    // =====================================================

    public async Task<bool> CancelMedicalRecordServiceAsync(
        int medicalRecordServiceId)
    {
        if (IsSubmitting)
        {
            return false;
        }

        if (medicalRecordServiceId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được chỉ định.";

            StateHasChanged();

            return false;
        }

        IsSubmitting = true;

        ActionMessage =
            string.Empty;

        ActionErrorMessage =
            string.Empty;

        StateHasChanged();

        try
        {
            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/medical-record-services/{medicalRecordServiceId}/cancel",
                        null
                    );

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            MedicalRecordServiceDTO?>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể hủy chỉ định.";

                return false;
            }

            ActionMessage =
                responseData.Message;

            if (MedicalRecordId.HasValue)
            {
                var reloadSuccess =
                    await LoadMedicalRecordAsync(
                        MedicalRecordId.Value
                    );

                if (!reloadSuccess)
                {
                    ActionErrorMessage =
                        "Đã hủy chỉ định nhưng không thể tải lại dữ liệu.";

                    return false;
                }
            }

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
    // ADD MEDICAL RECORD SERVICE
    //
    // POST:
    // /api/medical-records/{id}/services
    //
    // Request:
    // - ServiceId
    // - Quantity
    //
    // Response:
    // MedicalRecordServiceDTO
    // =====================================================

    public async Task<bool> AddMedicalRecordServiceAsync(
        int medicalRecordId,
        int serviceId,
        int quantity)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // VALIDATE MEDICAL RECORD ID
        // =================================================

        if (medicalRecordId <= 0)
        {
            ActionErrorMessage =
                "Không xác định được bệnh án.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALIDATE SERVICE
        // =================================================

        if (serviceId <= 0)
        {
            ActionErrorMessage =
                "Vui lòng chọn dịch vụ.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALIDATE QUANTITY
        // =================================================

        if (quantity <= 0)
        {
            ActionErrorMessage =
                "Số lượng phải lớn hơn 0.";

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
            // REQUEST
            // =================================================

            var request =
                new MedicalRecordServiceRequestDTO
                {
                    ServiceId =
                        serviceId,

                    Quantity =
                        quantity
                };


            // =================================================
            // CREATE JSON CONTENT
            // =================================================

            var content =
                JsonContent.Create(request);


            // =================================================
            // CALL API
            //
            // POST:
            // /api/medical-records/{id}/services
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/medical-records/{medicalRecordId}/services",
                        content
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            MedicalRecordServiceDTO?>>();


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
                    ?? "Không thể thêm chỉ định.";

                return false;
            }


            // =================================================
            // EMPTY CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được dữ liệu chỉ định sau khi thêm.";

                return false;
            }


            // =================================================
            // SUCCESS MESSAGE
            // =================================================

            ActionMessage =
                responseData.Message;


            // =================================================
            // RELOAD MEDICAL RECORD
            //
            // GET lại để cập nhật:
            // MedicalRecord.Services
            // =================================================

            var reloadSuccess =
                await LoadMedicalRecordAsync(
                    medicalRecordId
                );

            if (!reloadSuccess)
            {
                ActionErrorMessage =
                    "Đã thêm chỉ định nhưng không thể tải lại dữ liệu.";

                return false;
            }


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
    // LOAD SERVICE CATALOG
    //
    // GET:
    // /api/services
    //
    // Chỉ lấy các service active từ backend.
    // =====================================================

    public async Task<bool> LoadAvailableServicesAsync()
    {
        try
        {
            var response =
                await _authorizedApiService
                    .GetAsync(
                        "/api/services"
                    );

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<ServiceDTO>?>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải danh sách dịch vụ.";

                StateHasChanged();

                return false;
            }

            AvailableServices =
                responseData.Content
                ?? new List<ServiceDTO>();

            StateHasChanged();

            return true;
        }
        catch
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            StateHasChanged();

            return false;
        }
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
            // RELOAD MEDICAL RECORD
            //
            // MedicalRecordDraftResponseDTO không có Services.
            // Vì vậy sau khi Save Draft thành công,
            // gọi lại GET để lấy MedicalRecordExamDTO đầy đủ:
            //
            // - Medical Record
            // - Vital
            // - Services
            // =================================================

            var reloadSuccess =
                await LoadMedicalRecordAsync(
                    MedicalRecordId.Value
                );

            if (!reloadSuccess)
            {
                ActionErrorMessage =
                    "Đã lưu bệnh án nhưng không thể tải lại dữ liệu.";

                return false;
            }


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

            PatientId =
    draft.PatientId,

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