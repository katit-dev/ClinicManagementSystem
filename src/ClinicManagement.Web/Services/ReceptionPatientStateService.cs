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


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReceptionPatientStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
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