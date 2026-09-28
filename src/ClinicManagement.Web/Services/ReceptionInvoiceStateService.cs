using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;

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

    // =====================================================
    // ACTION STATE
    // =====================================================

    public bool IsSubmitting
    {
        get;
        private set;
    }


    public string ActionMessage
    {
        get;
        private set;
    } = string.Empty;


    public string ActionErrorMessage
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
    // CREATE PAYMENT
    // =====================================================

    public async Task<bool> CreatePaymentAsync(
        decimal amount,
        byte method,
        string? referenceCode,
        string? note)
    {
        if (Invoice == null)
        {
            ActionErrorMessage =
                "Chưa có thông tin hóa đơn.";

            StateHasChanged();

            return false;
        }


        // =====================================================
        // RESET ACTION STATE
        // =====================================================

        IsSubmitting = true;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // BUILD REQUEST
            // =================================================

            var request =
                new PaymentRequestDTO
                {
                    Amount = amount,

                    Method = method,

                    ReferenceCode =
                        string.IsNullOrWhiteSpace(referenceCode)
                            ? null
                            : referenceCode.Trim(),

                    Note =
                        string.IsNullOrWhiteSpace(note)
                            ? null
                            : note.Trim(),

                    IsRefund = false
                };


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/invoices/{Invoice.Id}/payments",
                        JsonContent.Create(request)
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
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể thực hiện thu tiền.";

                return false;
            }


            // =================================================
            // CHECK CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được thông tin hóa đơn sau khi thu tiền.";

                return false;
            }


            // =================================================
            // UPDATE INVOICE
            // =================================================

            Invoice =
                responseData.Content;


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;

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

            StateHasChanged();
        }
    }

    // =====================================================
    // REFUND PAYMENT
    // =====================================================

    public async Task<bool> RefundInvoiceAsync(
        decimal amount,
        byte method,
        string? referenceCode,
        string? note)
    {
        if (Invoice == null)
        {
            ActionErrorMessage =
                "Chưa có thông tin hóa đơn.";

            StateHasChanged();

            return false;
        }


        // =====================================================
        // RESET ACTION STATE
        // =====================================================

        IsSubmitting = true;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // BUILD REQUEST
            // =================================================

            var request =
                new PaymentRequestDTO
                {
                    Amount = amount,

                    Method = method,

                    ReferenceCode =
                        string.IsNullOrWhiteSpace(referenceCode)
                            ? null
                            : referenceCode.Trim(),

                    Note =
                        string.IsNullOrWhiteSpace(note)
                            ? null
                            : note.Trim(),

                    IsRefund = true
                };


            // =================================================
            // CALL REFUND API
            // =================================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        $"/api/invoices/{Invoice.Id}/refunds",
                        JsonContent.Create(request)
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
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể thực hiện hoàn tiền.";

                return false;
            }


            // =================================================
            // CHECK CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được thông tin hóa đơn sau khi hoàn tiền.";

                return false;
            }


            // =================================================
            // UPDATE INVOICE
            // =================================================

            Invoice =
                responseData.Content;


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;

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

            StateHasChanged();
        }
    }

    // =====================================================
    // CANCEL INVOICE
    // =====================================================

    public async Task<bool> CancelInvoiceAsync(
        string reason)
    {
        // =====================================================
        // CHECK INVOICE
        // =====================================================

        if (Invoice == null)
        {
            ActionErrorMessage =
                "Chưa có thông tin hóa đơn.";

            StateHasChanged();

            return false;
        }


        // =====================================================
        // VALIDATE REASON
        // =====================================================

        if (string.IsNullOrWhiteSpace(reason))
        {
            ActionErrorMessage =
                "Lý do hủy hóa đơn không được để trống.";

            StateHasChanged();

            return false;
        }


        if (reason.Trim().Length > 500)
        {
            ActionErrorMessage =
                "Lý do hủy không được vượt quá 500 ký tự.";

            StateHasChanged();

            return false;
        }


        // =====================================================
        // START
        // =====================================================

        IsSubmitting = true;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

        StateHasChanged();


        try
        {
            // =================================================
            // BUILD REQUEST
            // =================================================

            var request =
                new CancelInvoiceRequestDTO
                {
                    CancelReason =
                        reason.Trim()
                };


            var content =
                JsonContent.Create(request);


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .PatchAsync(
                        $"/api/invoices/{Invoice.Id}/cancel",
                        content
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
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể hủy hóa đơn.";

                return false;
            }


            // =================================================
            // CHECK CONTENT
            // =================================================

            if (responseData.Content == null)
            {
                ActionErrorMessage =
                    "Không nhận được thông tin hóa đơn sau khi hủy.";

                return false;
            }


            // =================================================
            // UPDATE INVOICE
            // =================================================

            Invoice =
                responseData.Content;


            // =================================================
            // SUCCESS
            // =================================================

            ActionMessage =
                responseData.Message;

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

            StateHasChanged();
        }
    }

    // ===================================================
    // RESET INVOICE
    // =====================================================

    public void ResetInvoice()
    {
        Invoice = null;

        ErrorMessage = string.Empty;

        IsLoading = false;

        IsSubmitting = false;

        ActionMessage = string.Empty;

        ActionErrorMessage = string.Empty;

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