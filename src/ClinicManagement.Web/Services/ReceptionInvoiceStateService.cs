using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;

namespace ClinicManagementSystem.Web.Services;

public class ReceptionInvoiceStateService
{
    private readonly AuthorizedApiService _authorizedApiService;

    // =====================================================
    // INVOICE
    // =====================================================

    public InvoiceDTO? Invoice
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
    } = string.Empty;


    public event Action? OnChange;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ReceptionInvoiceStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }


    // =====================================================
    // LOAD INVOICE
    // =====================================================

    public async Task<bool> LoadInvoiceAsync(
        int invoiceId)
    {
        Invoice = null;

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
                        $"/api/invoices/{invoiceId}"
                    );


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<InvoiceDTO?>>();


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
                    ?? "Không thể tải thông tin hóa đơn.";

                return false;
            }


            // =================================================
            // SUCCESS
            // =================================================

            if (responseData.Content == null)
            {
                ErrorMessage =
                    "Không nhận được thông tin hóa đơn.";

                return false;
            }


            Invoice =
                responseData.Content;

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
    // RESET INVOICE
    // =====================================================

    public void ResetInvoice()
    {
        Invoice = null;

        ErrorMessage = string.Empty;

        IsLoading = false;

        StateHasChanged();
    }


    // =====================================================
    // STATE CHANGE
    // =====================================================

    private void StateHasChanged()
    {
        OnChange?.Invoke();
    }
}