using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Queue;
using ClinicManagementSystem.Application.DTOs.Specialty;

namespace ClinicManagementSystem.Web.Services;

public class ReceptionQueueStateService
{
    private readonly AuthorizedApiService _authorizedApiService;


    // =====================================================
    // FILTER
    // =====================================================

    public int? SelectedSpecialtyId
    {
        get;
        private set;
    }


    public int? SelectedDoctorId
    {
        get;
        private set;
    }


    public DateOnly SelectedDate
    {
        get;
        private set;
    }
        = DateOnly.FromDateTime(
            DateTime.Now
        );


    // =====================================================
    // DATA
    // =====================================================

    public List<SpecialtyDTO> Specialties
    {
        get;
        private set;
    }
        = new();


    public List<DoctorDTO> Doctors
    {
        get;
        private set;
    }
        = new();


    public QueueDTO? Queue
    {
        get;
        private set;
    }


    // =====================================================
    // STATE
    // =====================================================

    public bool IsLoading
    {
        get;
        private set;
    }


    public bool IsActionLoading
    {
        get;
        private set;
    }


    public string ErrorMessage
    {
        get;
        private set;
    }
        = string.Empty;


    public string ActionMessage
    {
        get;
        private set;
    }
        = string.Empty;


    public event Action? OnChange;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReceptionQueueStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // LOAD SPECIALTIES
    // =====================================================

    public async Task<bool> LoadSpecialtiesAsync()
    {
        ErrorMessage = string.Empty;

        IsLoading = true;

        StateHasChanged();


        try
        {
            var response =
                await _authorizedApiService
                    .GetAsync(
                        "/api/specialties?active=true"
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<SpecialtyDTO>
                        >
                    >();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải danh sách chuyên khoa.";

                return false;
            }


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
    // SELECT SPECIALTY
    // =====================================================

    public void SetSpecialty(int? specialtyId)
    {
        SelectedSpecialtyId =
            specialtyId;

        SelectedDoctorId = null;

        Doctors.Clear();

        Queue = null;

        ActionMessage = string.Empty;

        ErrorMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // LOAD DOCTORS
    // =====================================================

    public async Task<bool> LoadDoctorsAsync()
    {
        Doctors.Clear();

        SelectedDoctorId = null;

        Queue = null;

        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;


        if (!SelectedSpecialtyId.HasValue)
        {
            StateHasChanged();

            return true;
        }


        IsLoading = true;

        StateHasChanged();


        try
        {
            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/doctors?specialtyId={SelectedSpecialtyId.Value}"
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<DoctorDTO>
                        >
                    >();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải danh sách bác sĩ.";

                return false;
            }


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
    // SELECT DOCTOR
    // =====================================================

    public void SetDoctor(int? doctorId)
    {
        SelectedDoctorId =
            doctorId;

        Queue = null;

        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // SELECT DATE
    // =====================================================

    public void SetDate(DateOnly date)
    {
        SelectedDate = date;

        Queue = null;

        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;

        StateHasChanged();
    }


    // =====================================================
    // LOAD QUEUE
    // =====================================================

    public async Task<bool> LoadQueueAsync()
    {
        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;


        if (!SelectedDoctorId.HasValue)
        {
            Queue = null;

            StateHasChanged();

            return true;
        }


        IsLoading = true;

        StateHasChanged();


        try
        {
            var date =
                SelectedDate.ToString(
                    "yyyy-MM-dd"
                );


            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/queue" +
                        $"?doctorId={SelectedDoctorId.Value}" +
                        $"&date={date}"
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<QueueDTO>
                    >();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                Queue = null;

                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải hàng chờ.";

                return false;
            }


            Queue =
                responseData.Content;


            return true;
        }
        catch
        {
            Queue = null;

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
    // RECALL
    // =====================================================

    public async Task<bool> RecallAsync(
        int appointmentId)
    {
        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;

        IsActionLoading = true;

        StateHasChanged();


        try
        {
            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/queue/{appointmentId}/recall",
                        null
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<QueueItemDTO>
                    >();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể gọi số.";

                return false;
            }


            ActionMessage =
                responseData.Message
                ?? "Đã gọi bệnh nhân.";


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
            IsActionLoading = false;

            StateHasChanged();
        }
    }


    // =====================================================
    // DEFER
    // =====================================================

    public async Task<bool> DeferAsync(
        int appointmentId)
    {
        ErrorMessage = string.Empty;

        ActionMessage = string.Empty;

        IsActionLoading = true;

        StateHasChanged();


        try
        {
            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/queue/{appointmentId}/defer",
                        null
                    );


            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<QueueItemDTO>
                    >();


            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể chuyển bệnh nhân xuống cuối hàng.";

                return false;
            }


            ActionMessage =
                responseData.Message
                ?? "Đã chuyển bệnh nhân xuống cuối hàng.";


            // =================================================
            // RELOAD QUEUE
            // =================================================

            await LoadQueueAsync();


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
            IsActionLoading = false;

            StateHasChanged();
        }
    }


    // =====================================================
    // REFRESH
    // =====================================================

    public async Task RefreshAsync()
    {
        await LoadQueueAsync();
    }


    // =====================================================
    // STATE CHANGED
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}