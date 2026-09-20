using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;

namespace ClinicManagementSystem.Web.Services;

public class BookingStateService
{
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // BOOKING STEP
    // =====================================================

    public int CurrentStep { get; private set; } = 1;


    // =====================================================
    // DOCTORS
    // =====================================================

    public List<DoctorDTO> Doctors { get; private set; }
        = new();


    // =====================================================
    // AVAILABLE SLOTS
    // =====================================================

    public List<SlotDTO> AvailableSlots { get; private set; }
        = new();


    // =====================================================
    // SELECTED SPECIALTY
    // =====================================================

    public int? SelectedSpecialtyId { get; private set; }


    // =====================================================
    // SELECTED DOCTOR
    // =====================================================

    public DoctorDTO? SelectedDoctor { get; private set; }


    // =====================================================
    // SELECTED DATE
    // =====================================================

    public DateOnly? SelectedDate { get; private set; }


    // =====================================================
    // SELECTED SLOT
    // =====================================================

    public SlotDTO? SelectedSlot { get; private set; }


    // =====================================================
    // REASON
    // =====================================================

    public string Reason { get; private set; }
        = string.Empty;


    // =====================================================
    // UI STATE
    // =====================================================

    public bool IsLoading { get; private set; }

    public string ErrorMessage { get; private set; }
        = string.Empty;

    public string SuccessMessage { get; private set; }
        = string.Empty;


    // =====================================================
    // EVENT
    // =====================================================

    public Action? OnChange { get; set; }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public BookingStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // NOTIFY STATE CHANGED
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }


    // =====================================================
    // SELECT SPECIALTY
    // =====================================================

    public void SelectSpecialty(
        int specialtyId)
    {
        SelectedSpecialtyId =
            specialtyId;


        // =================================================
        // SPECIALTY THAY ĐỔI
        // => RESET CÁC BƯỚC PHÍA SAU
        // =================================================

        SelectedDoctor = null;

        SelectedDate = null;

        SelectedSlot = null;

        Doctors.Clear();

        AvailableSlots.Clear();


        CurrentStep = 2;


        StateHasChanged();
    }


    // =====================================================
    // SELECT DOCTOR
    // =====================================================

    public void SelectDoctor(
        DoctorDTO doctor)
    {
        SelectedDoctor =
            doctor;


        // =================================================
        // DOCTOR THAY ĐỔI
        // => RESET DATE + SLOT
        // =================================================

        SelectedDate = null;

        SelectedSlot = null;

        AvailableSlots.Clear();


        CurrentStep = 3;


        StateHasChanged();
    }


    // =====================================================
    // SELECT DATE
    // =====================================================

    public void SelectDate(
        DateOnly date)
    {
        SelectedDate =
            date;


        // =================================================
        // DATE THAY ĐỔI
        // => SLOT CŨ KHÔNG CÒN HỢP LỆ
        // =================================================

        SelectedSlot = null;

        AvailableSlots.Clear();


        StateHasChanged();
    }


    // =====================================================
    // SELECT SLOT
    // =====================================================

    public void SelectSlot(
        SlotDTO slot)
    {
        SelectedSlot =
            slot;


        CurrentStep = 4;


        StateHasChanged();
    }


    // =====================================================
    // SET REASON
    // =====================================================

    public void SetReason(
        string reason)
    {
        Reason =
            reason?.Trim()
            ?? string.Empty;


        StateHasChanged();
    }


    // =====================================================
    // RESET BOOKING
    // =====================================================

    public void Reset()
    {
        CurrentStep = 1;

        SelectedSpecialtyId = null;

        SelectedDoctor = null;

        SelectedDate = null;

        SelectedSlot = null;

        Reason = string.Empty;

        Doctors.Clear();

        AvailableSlots.Clear();

        IsLoading = false;

        ErrorMessage = string.Empty;

        SuccessMessage = string.Empty;


        StateHasChanged();
    }
}