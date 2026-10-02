using System.Net.Http.Json;

using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Pharmacy;


namespace ClinicManagementSystem.Web.Services;


// =====================================================
// MEDICINE RECEIPT STATE SERVICE
// =====================================================

public class MedicineReceiptStateService
{
    private readonly AuthorizedApiService
        _authorizedApiService;


    // =====================================================
    // MEDICINES
    // =====================================================

    public List<MedicineInventoryDTO> Medicines
    {
        get;
        private set;
    } = [];

    // =====================================================
    // MEDICINE SEARCH
    // =====================================================

    public string SearchKeyword
    {
        get;
        set;
    } = string.Empty;


    public List<MedicineDTO> SearchResults
    {
        get;
        private set;
    } = [];


    public bool IsSearching
    {
        get;
        private set;
    }


    // =====================================================
    // LOAD STATE
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


    // =====================================================
    // FORM STATE
    // =====================================================

    public string SupplierName
    {
        get;
        set;
    } = string.Empty;


    public string ReferenceCode
    {
        get;
        set;
    } = string.Empty;


    public int SelectedMedicineId
    {
        get;
        set;
    }


    public string BatchNo
    {
        get;
        set;
    } = string.Empty;


    public DateTime ExpiryDate
    {
        get;
        set;
    } = DateTime.Today.AddYears(1);


    public int Quantity
    {
        get;
        set;
    }


    public decimal ImportPrice
    {
        get;
        set;
    }


    // =====================================================
    // CAN SUBMIT
    // =====================================================

    public bool CanSubmit =>
        SelectedMedicineId > 0 &&
        !string.IsNullOrWhiteSpace(BatchNo) &&
        ExpiryDate.Date > DateTime.Today &&
        Quantity > 0 &&
        ImportPrice >= 0;


    // =====================================================
    // STATE CHANGE
    // =====================================================

    public event Action? OnChange;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicineReceiptStateService(
        AuthorizedApiService authorizedApiService)
    {
        _authorizedApiService =
            authorizedApiService;
    }

    // =====================================================
    // SEARCH MEDICINES
    //
    // GET:
    // /api/medicines?keyword=
    //
    // Search theo:
    // - Code
    // - Name
    // - ActiveIngredient
    // =====================================================

    public async Task SearchMedicinesAsync()
    {
        var keyword =
            SearchKeyword.Trim();


        // =================================================
        // EMPTY KEYWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(keyword))
        {
            SearchResults = [];

            SelectedMedicineId = 0;

            NotifyStateChanged();

            return;
        }


        // =================================================
        // START SEARCH
        // =================================================

        IsSearching = true;

        ActionErrorMessage =
            string.Empty;

        NotifyStateChanged();


        try
        {
            var url =
                "/api/medicines?keyword=" +
                Uri.EscapeDataString(keyword);


            // =================================================
            // CALL API
            // =================================================

            var response =
                await _authorizedApiService
                    .GetAsync(url);


            // =================================================
            // READ RESPONSE
            // =================================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<MedicineDTO>
                        >
                    >();


            // =================================================
            // API ERROR
            // =================================================

            if (
                !response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300
            )
            {
                SearchResults = [];

                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể tìm thuốc.";

                return;
            }


            // =================================================
            // SUCCESS
            // =================================================

            SearchResults =
                responseData.Content
                ?? [];
        }
        catch (Exception ex)
        {
            SearchResults = [];

            ActionErrorMessage =
                $"Lỗi tìm thuốc: {ex.Message}";
        }
        finally
        {
            IsSearching = false;

            NotifyStateChanged();
        }
    }

    // =====================================================
    // SELECT MEDICINE
    // =====================================================

    public void SelectMedicine(
        MedicineDTO medicine)
    {
        SelectedMedicineId =
            medicine.Id;

        SearchKeyword =
            medicine.Name;

        SearchResults =
            [];

        ActionErrorMessage =
            string.Empty;

        NotifyStateChanged();
    }


    // =====================================================
    // RESET
    // =====================================================

    public void Reset()
    {
        Medicines = [];

        IsLoading = false;

        ErrorMessage =
            string.Empty;

        IsSubmitting = false;

        ActionMessage =
            string.Empty;

        ActionErrorMessage =
            string.Empty;


        // =================================================
        // RESET FORM
        // =================================================

        SupplierName =
            string.Empty;

        ReferenceCode =
            string.Empty;

        SelectedMedicineId =
            0;

        BatchNo =
            string.Empty;

        ExpiryDate =
            DateTime.Today.AddYears(1);

        Quantity =
            0;

        ImportPrice =
            0;

        SearchKeyword =
    string.Empty;

        SearchResults =
            [];

        IsSearching =
            false;
    }


    // =====================================================
    // LOAD MEDICINES
    //
    // GET:
    // /api/medicines/inventory
    //
    // =====================================================

    public async Task<bool> LoadMedicinesAsync()
    {
        if (IsLoading)
        {
            return false;
        }


        // =================================================
        // START LOADING
        // =================================================

        IsLoading =
            true;

        ErrorMessage =
            string.Empty;


        NotifyStateChanged();


        try
        {
            // =============================================
            // CALL API
            // =============================================

            var response =
                await _authorizedApiService
                    .GetAsync(
                        "/api/medicines/inventory" +
                        "?lowStock=false" +
                        "&nearExpiry=30"
                    );


            // =============================================
            // READ RESPONSE
            // =============================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<
                            List<MedicineInventoryDTO>
                        >
                    >();


            // =============================================
            // API FAILED
            // =============================================

            if (
                !response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300
            )
            {
                ErrorMessage =
                    responseData?.Message
                    ?? "Không thể tải danh sách thuốc.";

                Medicines =
                    [];

                return false;
            }


            // =============================================
            // UPDATE STATE
            // =============================================

            Medicines =
                responseData.Content
                ?? [];


            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Lỗi load thuốc: {ex.Message}";

            Medicines =
                [];

            return false;
        }
        finally
        {
            IsLoading =
                false;

            NotifyStateChanged();
        }
    }


    // =====================================================
    // SUBMIT MEDICINE RECEIPT
    //
    // POST:
    // /api/medicines/receipts
    //
    // Current UI:
    // chỉ có 1 dòng thuốc.
    //
    // =====================================================

    public async Task<bool> SubmitReceiptAsync()
    {
        // =================================================
        // PREVENT DOUBLE SUBMIT
        // =================================================

        if (IsSubmitting)
        {
            return false;
        }


        // =================================================
        // CLIENT VALIDATION
        // =================================================

        if (SelectedMedicineId <= 0)
        {
            ActionErrorMessage =
                "Vui lòng chọn thuốc.";

            NotifyStateChanged();

            return false;
        }


        if (string.IsNullOrWhiteSpace(BatchNo))
        {
            ActionErrorMessage =
                "Vui lòng nhập số lô.";

            NotifyStateChanged();

            return false;
        }


        if (ExpiryDate.Date <= DateTime.Today)
        {
            ActionErrorMessage =
                "Hạn sử dụng phải lớn hơn ngày hiện tại.";

            NotifyStateChanged();

            return false;
        }


        if (Quantity <= 0)
        {
            ActionErrorMessage =
                "Số lượng phải lớn hơn 0.";

            NotifyStateChanged();

            return false;
        }


        if (ImportPrice < 0)
        {
            ActionErrorMessage =
                "Giá nhập không được nhỏ hơn 0.";

            NotifyStateChanged();

            return false;
        }


        // =================================================
        // START SUBMIT
        // =================================================

        IsSubmitting =
            true;

        ActionMessage =
            string.Empty;

        ActionErrorMessage =
            string.Empty;


        NotifyStateChanged();


        try
        {
            // =============================================
            // CREATE REQUEST
            // =============================================

            var request =
                new MedicineReceiptRequestDTO
                {
                    SupplierName =
                        string.IsNullOrWhiteSpace(
                            SupplierName)
                            ? null
                            : SupplierName.Trim(),

                    ReferenceCode =
                        string.IsNullOrWhiteSpace(
                            ReferenceCode)
                            ? null
                            : ReferenceCode.Trim(),

                    Items =
                        new List<
                            MedicineReceiptItemRequestDTO
                        >
                        {
                            new MedicineReceiptItemRequestDTO
                            {
                                MedicineId =
                                    SelectedMedicineId,

                                BatchNo =
                                    BatchNo.Trim(),

                                ExpiryDate =
                                    ExpiryDate,

                                Quantity =
                                    Quantity,

                                ImportPrice =
                                    ImportPrice
                            }
                        }
                };


            // =============================================
            // CREATE JSON CONTENT
            // =============================================

            var content =
                JsonContent.Create(
                    request
                );


            // =============================================
            // CALL API
            //
            // POST:
            // /api/medicines/receipts
            // =============================================

            var response =
                await _authorizedApiService
                    .PostAsync(
                        "/api/prescriptions/medicines/receipts",
                        content
                    );


            // =============================================
            // READ RESPONSE
            // =============================================

            var responseData =
                await response.Content
                    .ReadFromJsonAsync<
                        HttpResponseData<bool>
                    >();


            // =============================================
            // API FAILED
            // =============================================

            if (
                !response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300
            )
            {
                ActionErrorMessage =
                    responseData?.Message
                    ?? "Không thể nhập thuốc.";

                return false;
            }


            // =============================================
            // SUCCESS
            // =============================================

            ActionMessage =
                responseData.Message
                ?? "Nhập thuốc thành công.";


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
            IsSubmitting =
                false;

            NotifyStateChanged();
        }
    }


    // =====================================================
    // STATE HAS CHANGED
    // =====================================================

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}