using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Patient;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// RECEPTION PATIENT STATE SERVICE
// =====================================================

public class ReceptionPatientStateService
{
    private readonly AuthorizedApiService _authorizedApiService;


    // =====================================================
    // PATIENTS
    // =====================================================

    public List<PatientDTO> Patients { get; private set; } = new();

    public PatientDTO? SelectedPatient { get; private set; }


    // =====================================================
    // SEARCH
    // =====================================================

    public string Keyword { get; private set; } = string.Empty;


    // =====================================================
    // PAGINATION
    // =====================================================

    public int CurrentPage { get; private set; } = 1;

    public int PageSize { get; private set; } = 10;

    public int TotalItems { get; private set; }

    public int TotalPages { get; private set; }


    // =====================================================
    // STATE
    // =====================================================

    public bool IsLoading { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public Action? OnChange { get; set; }

    // ACTION STATE
    public bool IsSubmitting { get; private set; }

    public string ActionMessage { get; private set; } = string.Empty;

    public string ActionErrorMessage { get; private set; } = string.Empty;

    public bool IsEditing { get; private set; }

    public bool IsAddingAllergy { get; private set; }


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReceptionPatientStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }

    // =====================================================
    // ADD PATIENT ALLERGY
    // =====================================================

    public async Task<bool> AddPatientAllergyAsync(
        int patientId,
        PatientAllergyRequestDTO request)
    {
        try
        {
            IsAddingAllergy = true;

            ActionMessage = string.Empty;
            ActionErrorMessage = string.Empty;

            StateHasChanged();


            // =================================================
            // REQUEST
            // =================================================

            var content =
                JsonContent.Create(request);


            var response =
                await _authorizedApiService.PostAsync(
                    $"/api/patients/{patientId}/allergies",
                    content
                );


            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<PatientAllergyDTO>>();




            // =================================================
            // ERROR
            // =================================================

            if (!response.IsSuccessStatusCode ||
                result?.Content == null)
            {
                ActionErrorMessage =
                    result?.Message ??
                    "Không thể thêm dị ứng.";

                return false;
            }




            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                result.Message;


            return true;
        }
        catch (Exception)
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsAddingAllergy = false;

            StateHasChanged();
        }
    }

    // =====================================================
    // CREATE PATIENT
    // =====================================================

    public async Task<bool> CreatePatientAsync(
        PatientRequestDTO request)
    {
        try
        {
            // =================================================
            // START
            // =================================================

            IsSubmitting = true;

            ActionMessage = string.Empty;
            ActionErrorMessage = string.Empty;

            StateHasChanged();


            // =================================================
            // REQUEST
            // =================================================

            var content = JsonContent.Create(request);

            var response =
                await _authorizedApiService.PostAsync(
                    "/api/patients",
                    content
                );


            // =================================================
            // READ RESPONSE
            // =================================================

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<PatientDTO>>();


            // =================================================
            // ERROR
            // =================================================

            if (!response.IsSuccessStatusCode ||
                result?.Content == null)
            {
                ActionErrorMessage =
                    result?.Message ??
                    "Không thể tạo hồ sơ bệnh nhân.";

                return false;
            }


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage = result.Message;

            SelectedPatient = result.Content;


            // =================================================
            // RELOAD PATIENT LIST
            // =================================================

            CurrentPage = 1;

            await LoadPatientsAsync();

            return true;
        }
        catch (Exception)
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsSubmitting = false;

            StateHasChanged();
        }
    }

    // =====================================================
    // UPDATE PATIENT
    // =====================================================

    public async Task<bool> UpdatePatientAsync(
        int patientId,
        PatientRequestDTO request)
    {
        try
        {
            IsEditing = true;

            ActionMessage = string.Empty;
            ActionErrorMessage = string.Empty;

            StateHasChanged();


            // =================================================
            // REQUEST
            // =================================================

            var content =
                JsonContent.Create(request);


            var response =
                await _authorizedApiService.PutAsync(
                    $"/api/patients/{patientId}",
                    content
                );


            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<PatientDTO>>();



            // =================================================
            // ERROR
            // =================================================

            if (!response.IsSuccessStatusCode ||
                result?.Content == null)
            {
                ActionErrorMessage =
                    result?.Message ??
                    "Không thể cập nhật hồ sơ bệnh nhân.";

                return false;
            }



            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                result.Message;


            SelectedPatient =
                result.Content;



            // =================================================
            // RELOAD LIST
            // =================================================

            await LoadPatientsAsync();


            return true;
        }
        catch (Exception)
        {
            ActionErrorMessage =
                "Không thể kết nối đến hệ thống.";

            return false;
        }
        finally
        {
            IsEditing = false;

            StateHasChanged();
        }
    }

    // =====================================================
    // LOAD PATIENTS
    // =====================================================

    public async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            StateHasChanged();


            // =================================================
            // BUILD URL
            // =================================================

            var url = $"/api/patients?page={CurrentPage}";

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                url +=
                    $"&keyword={Uri.EscapeDataString(Keyword.Trim())}";
            }


            // =================================================
            // REQUEST
            // =================================================

            var response =
                await _authorizedApiService.GetAsync(url);

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<PagedResult<PatientDTO>>>();


            // =================================================
            // ERROR
            // =================================================

            if (!response.IsSuccessStatusCode ||
                result?.Content == null)
            {
                Patients.Clear();

                ErrorMessage =
                    result?.Message ??
                    "Không thể tải danh sách bệnh nhân.";

                return;
            }


            // =================================================
            // SUCCESS
            // =================================================

            Patients = result.Content.Items;

            CurrentPage = result.Content.Page;
            PageSize = result.Content.PageSize;
            TotalItems = result.Content.TotalItems;
            TotalPages = result.Content.TotalPages;
        }
        catch (Exception)
        {
            Patients.Clear();

            ErrorMessage =
                "Không thể kết nối đến hệ thống.";
        }
        finally
        {
            IsLoading = false;

            StateHasChanged();
        }
    }


    // =====================================================
    // SET KEYWORD
    // =====================================================

    public void SetKeyword(string? keyword)
    {
        Keyword = keyword?.Trim() ?? string.Empty;

        CurrentPage = 1;

        StateHasChanged();
    }


    // =====================================================
    // SET PAGE
    // =====================================================

    public void SetPage(int page)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (TotalPages > 0 && page > TotalPages)
        {
            page = TotalPages;
        }

        CurrentPage = page;

        StateHasChanged();
    }


    // =====================================================
    // SELECT PATIENT
    // =====================================================

    public void SelectPatient(PatientDTO patient)
    {
        SelectedPatient = patient;

        StateHasChanged();
    }


    // =====================================================
    // CLEAR SELECTED PATIENT
    // =====================================================

    public void ClearSelectedPatient()
    {
        SelectedPatient = null;

        StateHasChanged();
    }


    // =====================================================
    // RESET
    // =====================================================

    public void Reset()
    {
        Patients.Clear();

        SelectedPatient = null;

        Keyword = string.Empty;

        CurrentPage = 1;
        PageSize = 10;
        TotalItems = 0;
        TotalPages = 0;

        ErrorMessage = string.Empty;
        IsLoading = false;

        ActionMessage = string.Empty;
        ActionErrorMessage = string.Empty;
        IsSubmitting = false;
        IsEditing = false;

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