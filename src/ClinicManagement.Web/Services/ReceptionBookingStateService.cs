using System.Net.Http.Json;
using System.Text.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Application.DTOs.Patient;


namespace ClinicManagementSystem.Web.Services;


public class ReceptionBookingStateService
{

    private readonly AuthorizedApiService
        _authorizedApiService;



    // =====================================================
    // SELECTED PATIENT
    // =====================================================

    public PatientLookupDTO? SelectedPatient
    {
        get;
        private set;
    }



    // =====================================================
    // SPECIALTIES
    // =====================================================

    public List<SpecialtyDTO> Specialties
    {
        get;
        private set;
    }
    = new();



    // =====================================================
    // DOCTORS
    // =====================================================

    public List<DoctorDTO> Doctors
    {
        get;
        private set;
    }
    = new();



    // =====================================================
    // SLOTS
    // =====================================================

    public List<SlotDTO> Slots
    {
        get;
        private set;
    }
    = new();



    // =====================================================
    // SELECTED
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
    =
    DateOnly.FromDateTime(
        DateTime.Now
    );



    public DateTime? SelectedSlot
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


    public string ErrorMessage
    {
        get;
        private set;
    }
    = string.Empty;



    public Action? OnChange
    {
        get;
        set;
    }



    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReceptionBookingStateService(
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
        Specialties.Clear();

        ErrorMessage = string.Empty;

        IsLoading = true;

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
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<List<SpecialtyDTO>>>();



            // =================================================
            // FAILED
            // =================================================

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



            // =================================================
            // SUCCESS
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

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }

}