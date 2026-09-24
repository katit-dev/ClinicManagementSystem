using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// RECEPTION APPOINTMENT STATE SERVICE
// =====================================================

public class ReceptionAppointmentStateService
{
    private readonly AuthorizedApiService _authorizedApiService;

    // APPOINTMENTS
    public List<ReceptionAppointmentDTO> Appointments { get; private set; } = new();

    // FILTER STATE
    public DateOnly SelectedDate { get; private set; } =
        DateOnly.FromDateTime(DateTime.Now);

    public int? SelectedDoctorId { get; private set; }

    public int? SelectedSpecialtyId { get; private set; }

    public AppointmentStatus? SelectedStatus { get; private set; }

    // UI STATE
    public bool IsLoading { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;



    // =====================================================
    // EVENT
    // =====================================================
    public Action? OnChange { get; set; }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================
    public ReceptionAppointmentStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }


    // =====================================================
    // LOAD APPOINTMENTS
    // =====================================================

    public async Task<bool> LoadAppointmentsAsync()
    {
        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;
        ErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // BUILD QUERY
            // =================================================

            var date =
                SelectedDate.ToString(
                    "yyyy-MM-dd"
                );

            var endpoint =
                $"/api/appointments?date={date}";


            // =================================================
            // DOCTOR FILTER
            // =================================================

            if (SelectedDoctorId.HasValue)
            {
                endpoint +=
                    $"&doctorId={SelectedDoctorId.Value}";
            }


            // =================================================
            // SPECIALTY FILTER
            // =================================================

            if (SelectedSpecialtyId.HasValue)
            {
                endpoint +=
                    $"&specialtyId={SelectedSpecialtyId.Value}";
            }


            // =================================================
            // STATUS FILTER
            // =================================================

            if (SelectedStatus.HasValue)
            {
                endpoint +=
                    $"&status={(byte)SelectedStatus.Value}";
            }


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(endpoint);


            // =================================================
            // API FAILED
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
                            List<ReceptionAppointmentDTO>>>();


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
            // UPDATE STATE
            // =================================================

            Appointments =
                responseData.Content
                ?? new List<ReceptionAppointmentDTO>();


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
    // SET DATE
    // =====================================================

    public void SetDate(DateOnly date)
    {
        SelectedDate = date;

        StateHasChanged();
    }


    // =====================================================
    // SET DOCTOR
    // =====================================================

    public void SetDoctor(int? doctorId)
    {
        SelectedDoctorId = doctorId;

        StateHasChanged();
    }


    // =====================================================
    // SET SPECIALTY
    // =====================================================

    public void SetSpecialty(int? specialtyId)
    {
        SelectedSpecialtyId = specialtyId;

        StateHasChanged();
    }


    // =====================================================
    // SET STATUS
    // =====================================================

    public void SetStatus(AppointmentStatus? status)
    {
        SelectedStatus = status;

        StateHasChanged();
    }


    // =====================================================
    // RESET FILTERS
    // =====================================================

    public void ResetFilters()
    {
        SelectedDate =
            DateOnly.FromDateTime(
                DateTime.Now
            );

        SelectedDoctorId = null;
        SelectedSpecialtyId = null;
        SelectedStatus = null;

        StateHasChanged();
    }


    // =====================================================
    // RESET
    // ====================================================
    public void Reset()
    {
        Appointments.Clear();

        SelectedDate =
            DateOnly.FromDateTime(
                DateTime.Now
            );

        SelectedDoctorId = null;
        SelectedSpecialtyId = null;
        SelectedStatus = null;

        IsLoading = false;
        ErrorMessage = string.Empty;

        StateHasChanged();
    }




    // =====================================================
    // NOTIFY STATE CHANGED
    // =====================================================
    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}