using System.Net;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Specialty;

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
    // SPECIALTIES
    // =====================================================

    public List<SpecialtyDTO> Specialties { get; private set; } = new();

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
    // SUBMIT STATE
    // =====================================================
    public bool IsSubmitting { get; private set; }

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
    // LOAD SPECIALTIES
    // =====================================================

    public async Task<bool> LoadSpecialtiesAsync()
    {
        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage =
            string.Empty;

        SuccessMessage =
            string.Empty;

        Specialties.Clear();


        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            //
            // Chỉ lấy Specialty đang hoạt động.
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
            //
            // [] là hợp lệ:
            // API thành công nhưng chưa có Specialty.
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
        // VALIDATE SELECTED SPECIALTY
        // =================================================

        if (!SelectedSpecialtyId.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn chuyên khoa trước.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage =
            string.Empty;

        SuccessMessage =
            string.Empty;


        Doctors.Clear();

        SelectedDoctor = null;

        SelectedDate = null;

        SelectedSlot = null;

        AvailableSlots.Clear();


        StateHasChanged();


        try
        {
            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/doctors" +
                        $"?specialtyId={SelectedSpecialtyId.Value}"
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
            //
            // []:
            // API thành công nhưng chuyên khoa hiện chưa
            // có bác sĩ phù hợp.
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
    // LOAD AVAILABLE SLOTS
    // =====================================================

    public async Task<bool> LoadAvailableSlotsAsync()
    {
        // =================================================
        // VALIDATE SELECTED DOCTOR
        // =================================================

        if (SelectedDoctor == null)
        {
            ErrorMessage =
                "Vui lòng chọn bác sĩ trước.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALIDATE SELECTED DATE
        // =================================================

        if (!SelectedDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày khám trước.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // START LOADING
        // =================================================

        IsLoading = true;

        ErrorMessage =
            string.Empty;

        SuccessMessage =
            string.Empty;


        SelectedSlot = null;

        AvailableSlots.Clear();


        StateHasChanged();


        try
        {
            // =================================================
            // GET DOCTOR ID
            // =================================================

            var doctorId =
                SelectedDoctor.Id;


            // =================================================
            // FORMAT DATE
            //
            // yyyy-MM-dd
            // =================================================

            var date =
                SelectedDate.Value
                    .ToString(
                        "yyyy-MM-dd"
                    );


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        $"/api/doctors/{doctorId}" +
                        $"/available-slots" +
                        $"?date={date}"
                    );


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage =
                    "Không thể tải khung giờ khám.";

                return false;
            }


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<SlotDTO>>>();


            // =================================================
            // INVALID RESPONSE
            // =================================================

            if (responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không nhận được dữ liệu khung giờ.";

                return false;
            }


            // =================================================
            // UPDATE STATE
            //
            // []:
            // - bác sĩ không làm ngày này
            // - hết slot
            // - tất cả slot đã được đặt
            //
            // Đây không phải lỗi API.
            // =================================================

            AvailableSlots =
                responseData.Content
                ?? new List<SlotDTO>();


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
    // VALIDATE BOOKING
    // =====================================================

    public bool ValidateBooking()
    {
        ErrorMessage =
            string.Empty;

        SuccessMessage =
            string.Empty;


        // =================================================
        // SPECIALTY
        // =================================================

        if (!SelectedSpecialtyId.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn chuyên khoa.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // DOCTOR
        // =================================================

        if (SelectedDoctor == null)
        {
            ErrorMessage =
                "Vui lòng chọn bác sĩ.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // DATE
        // =================================================

        if (!SelectedDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày khám.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // SLOT
        // =================================================

        if (SelectedSlot == null)
        {
            ErrorMessage =
                "Vui lòng chọn khung giờ khám.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // REASON LENGTH
        //
        // Database: reason nvarchar(500)
        // =================================================

        if (Reason.Length > 500)
        {
            ErrorMessage =
                "Lý do khám không được vượt quá 500 ký tự.";

            StateHasChanged();

            return false;
        }


        // =================================================
        // VALID
        // =================================================

        return true;
    }

    // =====================================================
    // CREATE APPOINTMENT
    // =====================================================

    public async Task<AppointmentDTO?>
        CreateAppointmentAsync()
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return null;
        }


        // =================================================
        // VALIDATE BOOKING
        // =================================================

        if (!ValidateBooking())
        {
            return null;
        }


        // =================================================
        // START SUBMIT
        // =================================================

        IsSubmitting = true;

        ErrorMessage =
            string.Empty;

        SuccessMessage =
            string.Empty;


        StateHasChanged();


        try
        {
            // =================================================
            // CREATE REQUEST
            //
            // PatientId không gửi từ Frontend.
            // Backend lấy Patient từ JWT.
            // =================================================

            var request =
                new CreateAppointmentRequestDTO
                {
                    DoctorId =
                        SelectedDoctor!.Id,

                    StartTime =
                        SelectedSlot!.StartTime,

                    Reason =
                        string.IsNullOrWhiteSpace(
                            Reason)
                            ? null
                            : Reason
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
            // POST /api/appointments
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        "/api/appointments",
                        content
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            AppointmentDTO?>>();


            // =================================================
            // SLOT CONFLICT
            //
            // Ví dụ:
            // User A và User B cùng chọn một slot.
            // User A submit trước.
            // User B submit sau => 409.
            // =================================================

            if (response.StatusCode ==
                HttpStatusCode.Conflict)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Khung giờ vừa được người khác đặt. " +
                       "Vui lòng chọn khung giờ khác.";


                // Slot hiện tại không còn đáng tin cậy nữa.
                SelectedSlot = null;

                AvailableSlots.Clear();


                return null;
            }


            // =================================================
            // API FAILED
            // =================================================

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300 ||
                responseData.Content == null)
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tạo lịch hẹn.";

                return null;
            }


            // =================================================
            // SUCCESS
            // =================================================

            SuccessMessage =
                responseData.Message;


            return responseData.Content;
        }
        catch
        {
            ErrorMessage =
                "Không thể kết nối đến hệ thống. " +
                "Vui lòng thử lại.";

            return null;
        }
        finally
        {
            IsSubmitting = false;

            StateHasChanged();
        }
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
        SelectedSpecialtyId = specialtyId;

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

        Specialties.Clear();

        IsLoading = false;

        ErrorMessage = string.Empty;

        SuccessMessage = string.Empty;

        IsSubmitting = false;

        StateHasChanged();
    }
}