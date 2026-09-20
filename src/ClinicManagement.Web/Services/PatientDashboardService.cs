using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;

namespace ClinicManagementSystem.Web.Services;

// =====================================================
// PATIENT DASHBOARD DATA
// =====================================================

public class PatientDashboardData
{
    public List<MyAppointmentDTO>?
        UpcomingAppointments
    { get; set; }

    public List<MyAppointmentDTO>?
        CompletedAppointments
    { get; set; }

    public List<MyAppointmentDTO>?
        CancelledAppointments
    { get; set; }
}


// =====================================================
// PATIENT DASHBOARD SERVICE CONTRACT
// =====================================================
public interface IPatientDashboardService
{
    Task<List<MyAppointmentDTO>?> GetAppointmentsAsync(
        MyAppointmentFilter filter);

    Task<PatientDashboardData>
        GetDashboardDataAsync();
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
    // GET DASHBOARD DATA
    // =====================================================

    public async Task<PatientDashboardData>
        GetDashboardDataAsync()
    {
        // =================================================
        // UPCOMING APPOINTMENTS
        // =================================================

        var upcomingAppointments =
            await GetAppointmentsAsync(
                MyAppointmentFilter.Upcoming
            );


        // =================================================
        // COMPLETED APPOINTMENTS
        // =================================================

        var completedAppointments =
            await GetAppointmentsAsync(
                MyAppointmentFilter.Completed
            );


        // =================================================
        // CANCELLED APPOINTMENTS
        // =================================================

        var cancelledAppointments =
            await GetAppointmentsAsync(
                MyAppointmentFilter.Cancelled
            );


        // =================================================
        // RETURN DASHBOARD DATA
        // =================================================

        return new PatientDashboardData
        {
            UpcomingAppointments =
                upcomingAppointments,

            CompletedAppointments =
                completedAppointments,

            CancelledAppointments =
                cancelledAppointments
        };
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

}