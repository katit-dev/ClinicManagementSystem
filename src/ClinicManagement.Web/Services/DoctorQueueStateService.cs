using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
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