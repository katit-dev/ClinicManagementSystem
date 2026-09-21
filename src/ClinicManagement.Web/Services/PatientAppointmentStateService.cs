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
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // APPOINTMENTS
    // =====================================================

    public List<MyAppointmentDTO> Appointments
    {
        get;
        private set;
    } = new();


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
    // RESET
    // =====================================================

    public void Reset()
    {
        Appointments.Clear();

        CurrentFilter =
            MyAppointmentFilter.All;

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