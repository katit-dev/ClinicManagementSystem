using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
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
    // DOCTOR
    // =====================================================

    public DoctorDTO? CurrentDoctor
    {
        get;
        private set;
    }


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
    } = DateOnly.FromDateTime(DateTime.Now);


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
    // LOAD DOCTOR QUEUE
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
            // GET CURRENT DOCTOR
            //
            // GET:
            // /api/doctors/me
            // =================================================

            var doctorResponse =
                await _authorizedApiService
                    .GetAsync(
                        "/api/doctors/me"
                    );


            // =================================================
            // READ DOCTOR RESPONSE
            // =================================================

            var doctorResponseData =
                await doctorResponse.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<DoctorDTO>>();


            // =================================================
            // DOCTOR API FAILED
            // =================================================

            if (!doctorResponse.IsSuccessStatusCode ||
                doctorResponseData == null ||
                doctorResponseData.StatusCode < 200 ||
                doctorResponseData.StatusCode >= 300)
            {
                ErrorMessage =
                    doctorResponseData?.Message
                    ?? "Không thể lấy thông tin bác sĩ.";

                return false;
            }


            // =================================================
            // DOCTOR CONTENT EMPTY
            // =================================================

            if (doctorResponseData.Content == null)
            {
                ErrorMessage =
                    "Không tìm thấy thông tin bác sĩ.";

                return false;
            }


            // =================================================
            // UPDATE CURRENT DOCTOR
            // =================================================

            CurrentDoctor =
                doctorResponseData.Content;


            // =================================================
            // BUILD QUEUE ENDPOINT
            //
            // GET:
            // /api/appointments/queue
            // ?doctorId=5
            // &date=2026-09-29
            // =================================================

            var endpoint =
                $"/api/appointments/queue" +
                $"?doctorId={CurrentDoctor.Id}" +
                $"&date={date:yyyy-MM-dd}";


            // =================================================
            // CALL QUEUE API
            // =================================================

            var queueResponse =
                await _authorizedApiService
                    .GetAsync(endpoint);


            // =================================================
            // READ QUEUE RESPONSE
            // =================================================

            var queueResponseData =
                await queueResponse.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<QueueDTO>>();


            // =================================================
            // QUEUE API FAILED
            // =================================================

            if (!queueResponse.IsSuccessStatusCode ||
                queueResponseData == null ||
                queueResponseData.StatusCode < 200 ||
                queueResponseData.StatusCode >= 300)
            {
                ErrorMessage =
                    queueResponseData?.Message
                    ?? "Không thể tải hàng chờ.";

                return false;
            }


            // =================================================
            // QUEUE CONTENT EMPTY
            // =================================================

            if (queueResponseData.Content == null)
            {
                ErrorMessage =
                    "Không nhận được dữ liệu hàng chờ.";

                return false;
            }


            // =================================================
            // UPDATE QUEUE
            // =================================================

            Queue =
                queueResponseData.Content;


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
        CurrentDoctor = null;

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