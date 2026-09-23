using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;

namespace ClinicManagementSystem.Web.Services;


// =====================================================
// PATIENT INVOICE STATE SERVICE
// =====================================================

public class PatientInvoiceStateService
{
    private readonly AuthorizedApiService _authorizedApiService;


    // =================================================
    // STATE
    // =================================================

    public List<PatientInvoiceDTO> Invoices { get; private set; } = new();

    public bool IsLoading { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public Action? OnChange { get; set; }


    // =================================================
    // CONSTRUCTOR
    // =================================================

    public PatientInvoiceStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService = authorizedApiService;
    }


    // =================================================
    // LOAD INVOICES
    // =================================================

    public async Task LoadInvoicesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        StateHasChanged();

        try
        {
            var response = await _authorizedApiService.GetAsync(
                "/api/patients/me/invoices"
            );

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Không thể tải danh sách hóa đơn.";
                Invoices = new List<PatientInvoiceDTO>();
                return;
            }

            var result = await response.Content
                .ReadFromJsonAsync<HttpResponseData<List<PatientInvoiceDTO>>>();

            if (result?.Content == null)
            {
                Invoices = new List<PatientInvoiceDTO>();
                return;
            }

            Invoices = result.Content;
        }
        catch
        {
            ErrorMessage = "Đã xảy ra lỗi khi tải danh sách hóa đơn.";
            Invoices = new List<PatientInvoiceDTO>();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }


    // =================================================
    // RESET
    // =================================================

    public void Reset()
    {
        Invoices = new List<PatientInvoiceDTO>();
        ErrorMessage = string.Empty;
        IsLoading = false;

        StateHasChanged();
    }


    // =================================================
    // STATE CHANGED
    // =================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}