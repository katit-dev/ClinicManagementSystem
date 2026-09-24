using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Application.DTOs.Doctor;

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

    public List<SpecialtyDTO> Specialties { get; private set; } = new();

    public List<DoctorDTO> Doctors { get; private set; } = new();

    // ACTION STATE
    public bool IsSubmitting { get; private set; }

    public int? ProcessingAppointmentId { get; private set; }

    public string ActionMessage { get; private set; } = string.Empty;

    public string ActionErrorMessage { get; private set; } = string.Empty;



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
    // LOAD SPECIALTIES
    // =====================================================

    public async Task<bool> LoadSpecialtiesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        StateHasChanged();

        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        "/api/specialties?active=true"
                    );


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage =
                    "Không thể tải danh sách chuyên khoa.";

                return false;
            }


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<SpecialtyDTO>>>();


            // =================================================
            // INVALID RESPONSE
            // =================================================

            if (responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không nhận được dữ liệu chuyên khoa.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            // =================================================

            Specialties =
                responseData.Content
                ?? new List<SpecialtyDTO>();


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
    // LOAD DOCTORS
    // =====================================================

    public async Task<bool> LoadDoctorsAsync()
    {
        // =================================================
        // VALIDATE SPECIALTY
        // =================================================

        if (!SelectedSpecialtyId.HasValue)
        {
            Doctors.Clear();
            SelectedDoctorId = null;

            StateHasChanged();

            return true;
        }


        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;
        ErrorMessage = string.Empty;

        Doctors.Clear();
        SelectedDoctorId = null;

        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/doctors?specialtyId={SelectedSpecialtyId.Value}"
                    );


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage =
                    "Không thể tải danh sách bác sĩ.";

                return false;
            }


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<DoctorDTO>>>();


            // =================================================
            // INVALID RESPONSE
            // =================================================

            if (responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không nhận được dữ liệu bác sĩ.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            // =================================================

            Doctors =
                responseData.Content
                ?? new List<DoctorDTO>();


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

        // =================================================
        // SPECIALTY CHANGED
        //
        // Doctor cũ không còn đáng tin cậy.
        // =================================================

        SelectedDoctorId = null;
        Doctors.Clear();

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

        Doctors.Clear();

        StateHasChanged();
    }

    // =====================================================
    // CHECK IN APPOINTMENT
    // =====================================================

    public async Task<bool> CheckInAppointmentAsync(int appointmentId)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // START SUBMIT
        // =================================================

        IsSubmitting = true;
        ProcessingAppointmentId = appointmentId;

        ActionMessage = string.Empty;
        ActionErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            //
            // PATCH:
            // /api/appointments/{id}/check-in
            // =================================================

            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/appointments/{appointmentId}/check-in",
                        new StringContent(string.Empty)
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            ReceptionAppointmentDTO>>();


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
                    ?? "Không thể check-in bệnh nhân.";

                return false;
            }


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;


            // =================================================
            // RELOAD APPOINTMENTS
            //
            // Backend vừa thay:
            // Status
            // QueueNumber
            // CheckedInAt
            // =================================================

            await LoadAppointmentsAsync();


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
            IsSubmitting = false;
            ProcessingAppointmentId = null;

            StateHasChanged();
        }
    }

    // =====================================================
    // MARK APPOINTMENT AS NO SHOW
    // =====================================================

    public async Task<bool> MarkNoShowAsync(
        int appointmentId,
        string? note = null)
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // START SUBMIT
        // =================================================

        IsSubmitting = true;
        ProcessingAppointmentId = appointmentId;

        ActionMessage = string.Empty;
        ActionErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // CREATE REQUEST
            // =================================================

            var request =
                new NoShowAppointmentRequestDTO
                {
                    Note =
                        string.IsNullOrWhiteSpace(note)
                            ? null
                            : note.Trim()
                };


            // =================================================
            // CREATE HTTP CONTENT
            // =================================================

            var content =
                JsonContent.Create(
                    request
                );


            // =================================================
            // CALL API
            //
            // PATCH:
            // /api/appointments/{id}/no-show
            // =================================================

            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/appointments/{appointmentId}/no-show",
                        content
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            ReceptionAppointmentDTO>>();


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
                    ?? "Không thể đánh dấu bệnh nhân không đến.";

                return false;
            }


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;


            // =================================================
            // RELOAD APPOINTMENTS
            // =================================================

            await LoadAppointmentsAsync();


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
            IsSubmitting = false;
            ProcessingAppointmentId = null;

            StateHasChanged();
        }
    }


    // =====================================================
    // RESET
    // ====================================================
    public void Reset()
    {
        Appointments.Clear();
        Specialties.Clear();
        Doctors.Clear();

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