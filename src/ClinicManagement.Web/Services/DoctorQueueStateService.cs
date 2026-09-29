using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Queue;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// DOCTOR QUEUE STATE SERVICE
// =====================================================

public class DoctorQueueStateService
{
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // QUEUE ITEMS
    // =====================================================

    public List<DoctorQueueItemDTO> QueueItems
    {
        get;
        private set;
    } = new();


    // =====================================================
    // SELECTED DATE
    // =====================================================

    public DateOnly SelectedDate
    {
        get;
        private set;
    } = DateOnly.FromDateTime(
        DateTime.Now
    );


    // =====================================================
    // STATE
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
    // START EXAM STATE
    // =====================================================

    public bool IsStartingExam
    {
        get;
        private set;
    }

    public int? StartingAppointmentId
    {
        get;
        private set;
    }

    public MedicalRecordDTO? MedicalRecord
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
    // EVENT
    // =====================================================

    public Action? OnChange
    {
        get;
        set;
    }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorQueueStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // LOAD MY QUEUE
    //
    // GET:
    // /api/doctors/me/queue?date=2026-09-29
    //
    // DoctorId:
    // Backend lấy từ JWT
    //
    // Response:
    // HttpResponseData<List<DoctorQueueItemDTO>>
    // =====================================================

    public async Task<bool> LoadQueueAsync(
        DateOnly date)
    {
        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage = string.Empty;

        QueueItems = new();

        SelectedDate = date;

        NotifyStateChanged();


        try
        {
            // =================================================
            // BUILD ENDPOINT
            // =================================================

            var endpoint =
                $"/api/doctors/me/queue" +
                $"?date={date:yyyy-MM-dd}";


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(endpoint);


            // =================================================
            // HTTP ERROR
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage =
                    $"Không thể tải hàng chờ. " +
                    $"HTTP {(int)response.StatusCode} " +
                    $"({response.StatusCode}).";

                return false;
            }


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<DoctorQueueItemDTO>
                        >
                    >();


            // =================================================
            // INVALID RESPONSE
            // =================================================

            if (responseData == null)
            {
                ErrorMessage =
                    "Không nhận được dữ liệu hàng chờ.";

                return false;
            }


            // =================================================
            // API RESPONSE FAILED
            // =================================================

            if (responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData.Message
                    ?? "Lấy hàng chờ thất bại.";

                return false;
            }


            // =================================================
            // CONTENT
            // =================================================

            QueueItems =
                responseData.Content
                ?? new List<DoctorQueueItemDTO>();


            // =================================================
            // SUCCESS
            // =================================================

            return true;
        }
        catch (Exception ex)
        {
            // =================================================
            // TEMPORARY DEBUG
            // =================================================

            ErrorMessage =
                $"Không thể kết nối đến hệ thống: " +
                $"{ex.Message}";

            return false;
        }
        finally
        {
            IsLoading = false;

            NotifyStateChanged();
        }
    }

    // =====================================================
    // START EXAM
    //
    // POST:
    // /api/appointments/{id}/start-exam
    //
    // Response:
    // MedicalRecordDTO
    // =====================================================

    public async Task<bool> StartExamAsync(
        int appointmentId)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsStartingExam)
        {
            return false;
        }


        // =================================================
        // RESET STATE
        // =================================================

        IsStartingExam = true;

        StartingAppointmentId =
            appointmentId;

        MedicalRecord = null;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

        NotifyStateChanged();


        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/appointments/{appointmentId}/start-exam",
                        new StringContent(string.Empty)
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<MedicalRecordDTO>>();


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
                    ?? "Không thể bắt đầu khám.";

                return false;
            }


            // =================================================
            // CHECK CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được thông tin bệnh án.";

                return false;
            }


            // =================================================
            // UPDATE MEDICAL RECORD
            // =================================================

            MedicalRecord =
                responseData.Content;


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
            IsStartingExam = false;

            StartingAppointmentId = null;

            NotifyStateChanged();
        }
    }

    // =====================================================
    // RESET
    // =====================================================

    public void Reset()
    {
        QueueItems = new();

        SelectedDate =
            DateOnly.FromDateTime(
                DateTime.Now
            );

        IsLoading = false;

        ErrorMessage = string.Empty;

        IsStartingExam = false;

        StartingAppointmentId = null;

        MedicalRecord = null;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

        NotifyStateChanged();
    }


    // =====================================================
    // NOTIFY
    // =====================================================

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}