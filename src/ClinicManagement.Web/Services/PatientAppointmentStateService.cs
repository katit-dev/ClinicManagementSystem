using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;


namespace ClinicManagementSystem.Web.Services;


// =====================================================
// PATIENT APPOINTMENT STATE SERVICE
// =====================================================

public class PatientAppointmentStateService
{
    private readonly AuthorizedApiService _authorizedApiService;


    // =====================================================
    // APPOINTMENTS
    // =====================================================
    public List<MyAppointmentDTO> Appointments { get; private set; } = new();

    // CANCEL ERROR MESSAGE
    public string CancelErrorMessage { get; private set; } = string.Empty;
    public int? CancellingAppointmentId { get; private set; }


    public bool IsCancelling { get; private set; }


    // =====================================================
    // CURRENT FILTER
    // =====================================================

    public MyAppointmentFilter CurrentFilter
    {
        get;
        private set;
    } = MyAppointmentFilter.All;


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
    // STATE CHANGE EVENT
    // =====================================================

    public Action? OnChange
    {
        get;
        set;
    }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientAppointmentStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // LOAD APPOINTMENTS
    // =====================================================

    public async Task<bool> LoadAppointmentsAsync(
        MyAppointmentFilter filter =
            MyAppointmentFilter.All)
    {
        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage =
            string.Empty;


        CurrentFilter =
            filter;


        Appointments.Clear();


        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            //
            // GET:
            // /api/appointments/my?filter=All
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/appointments/my" +
                        $"?filter={filter}"
                    );


            // =================================================
            // HTTP FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage =
                    "Không thể tải danh sách lịch hẹn.";

                return false;
            }


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<MyAppointmentDTO>>>();


            // =================================================
            // INVALID RESPONSE
            // =================================================

            if (responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không nhận được dữ liệu lịch hẹn.";

                return false;
            }


            // =================================================
            // UPDATE APPOINTMENTS
            //
            // [] là hợp lệ:
            // Patient chưa có lịch hẹn.
            // =================================================

            Appointments =
                responseData.Content
                ?? new List<MyAppointmentDTO>();


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
    // CANCEL APPOINTMENT
    // =====================================================

    public async Task<bool> CancelAppointmentAsync(int appointmentId, string reason)
    {
        // =================================================
        // CLEAR PREVIOUS CANCEL ERROR
        // =================================================

        CancelErrorMessage =
            string.Empty;


        // =================================================
        // VALIDATE REASON
        // =================================================

        if (string.IsNullOrWhiteSpace(
            reason))
        {
            CancelErrorMessage =
                "Vui lòng nhập lý do hủy lịch.";

            StateHasChanged();

            return false;
        }


        if (reason.Trim().Length > 500)
        {
            CancelErrorMessage =
                "Lý do hủy không được vượt quá 500 ký tự.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START CANCELLING
        // =================================================

        IsCancelling = true;

        CancellingAppointmentId =
            appointmentId;

        CancelErrorMessage =
            string.Empty;


        StateHasChanged();


        try
        {
            var request =
                new CancelAppointmentRequestDTO
                {
                    CancelReason = reason.Trim()
                };


            var content =
                JsonContent.Create(
                    request
                );


            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/appointments/{appointmentId}/cancel",
                        content
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<object>>();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                CancelErrorMessage =
                    responseData?.Message
                    ?? "Không thể hủy lịch hẹn.";

                return false;
            }


            return true;
        }
        catch
        {
            CancelErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsCancelling = false;

            CancellingAppointmentId =
                null;

            StateHasChanged();
        }
    }

    // =====================================================
    // CLEAR CANCEL ERROR
    // =====================================================

    public void ClearCancelError()
    {
        CancelErrorMessage =
            string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // RESET
    // =====================================================

    public void Reset()
    {
        Appointments.Clear();

        CurrentFilter =
            MyAppointmentFilter.All;

        IsLoading = false;

        IsCancelling = false;

        CancellingAppointmentId =
            null;

        ErrorMessage =
            string.Empty;

        CancelErrorMessage = string.Empty;


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