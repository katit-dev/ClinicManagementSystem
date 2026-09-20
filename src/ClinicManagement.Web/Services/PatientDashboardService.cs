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
    Task<int> GetAppointmentCountAsync(
        MyAppointmentFilter filter);
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
    // GET APPOINTMENT COUNT
    // =====================================================

    public async Task<int> GetAppointmentCountAsync(
        MyAppointmentFilter filter)
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
            return 0;
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
        // RESPONSE NULL
        // =================================================

        if (responseData?.Content == null)
        {
            return 0;
        }


        // =================================================
        // RETURN COUNT
        // =================================================

        return responseData.Content.Count;
    }
}