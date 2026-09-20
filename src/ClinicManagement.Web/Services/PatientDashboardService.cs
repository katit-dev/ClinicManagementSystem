using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// PATIENT DASHBOARD SERVICE CONTRACT
// =====================================================

public interface IPatientDashboardService
{
    Task<List<MyAppointmentDTO>?> GetAppointmentsAsync(
        MyAppointmentFilter filter);

    Task<int?> GetAppointmentCountAsync(
        MyAppointmentFilter filter);

    Task<MyAppointmentDTO?> GetNearestUpcomingAppointmentAsync();
}


// =====================================================
// PATIENT DASHBOARD SERVICE
// =====================================================

public class PatientDashboardService
    : IPatientDashboardService
{
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientDashboardService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }

    // =====================================================
    // GET NEAREST UPCOMING APPOINTMENT
    // =====================================================

    public async Task<MyAppointmentDTO?>
        GetNearestUpcomingAppointmentAsync()
    {
        // =================================================
        // GET UPCOMING APPOINTMENTS
        // =================================================

        var appointments =
            await GetAppointmentsAsync(
                MyAppointmentFilter.Upcoming
            );


        // =================================================
        // API FAILED
        // =================================================

        if (appointments == null)
        {
            return null;
        }


        // =================================================
        // NO UPCOMING APPOINTMENT
        // =================================================

        if (appointments.Count == 0)
        {
            return null;
        }


        // =================================================
        // GET NEAREST APPOINTMENT
        // =================================================

        return appointments
            .OrderBy(
                appointment =>
                    appointment.StartTime
            )
            .FirstOrDefault();
    }

    // =====================================================
    // GET APPOINTMENTS
    // =====================================================

    public async Task<List<MyAppointmentDTO>?>
        GetAppointmentsAsync(
            MyAppointmentFilter filter)
    {
        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/appointments/my?filter={filter}"
                    );


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                return null;
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
                responseData.StatusCode >= 300 ||
                responseData.Content == null)
            {
                return null;
            }


            // =================================================
            // SUCCESS
            // =================================================

            return responseData.Content;
        }
        catch
        {
            return null;
        }
    }

    // =====================================================
    // GET APPOINTMENT COUNT
    // =====================================================

    // =====================================================
    // GET APPOINTMENT COUNT
    // =====================================================

    public async Task<int?> GetAppointmentCountAsync(
        MyAppointmentFilter filter)
    {
        var appointments =
            await GetAppointmentsAsync(
                filter
            );


        // =================================================
        // API FAILED
        // =================================================

        if (appointments == null)
        {
            return null;
        }


        // =================================================
        // RETURN COUNT
        // =================================================

        return appointments.Count;
    }
}