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
    // QUEUE
    // =====================================================

    public QueueDTO? Queue
    {
        get;
        private set;
    }


    // =====================================================
    // DATE
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
    // DoctorId được backend lấy từ JWT.
    // =====================================================

    public async Task<bool> LoadQueueAsync(
        DateOnly date)
    {
        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage =
            string.Empty;

        Queue = null;

        SelectedDate =
            date;

        StateHasChanged();


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
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<QueueDTO>>();


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
                    ?? "Không thể tải hàng chờ.";

                return false;
            }


            // =================================================
            // CONTENT EMPTY
            // =================================================

            if (responseData.Content == null)
            {
                ErrorMessage =
                    "Không nhận được dữ liệu hàng chờ.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            // =================================================

            Queue =
                responseData.Content;


            // =================================================
            // SUCCESS
            // =================================================

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
    // RESET
    // =====================================================

    public void Reset()
    {
        Queue = null;

        SelectedDate =
            DateOnly.FromDateTime(
                DateTime.Now
            );

        IsLoading = false;

        ErrorMessage =
            string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // STATE HAS CHANGED
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}