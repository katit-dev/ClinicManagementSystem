using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Medicine;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IMedicineService
{
    Task<HttpResponseData<List<MedicineSearchDTO>>>
        SearchMedicinesAsync(
            string? keyword,
            int patientId);
}

public class MedicineService : IMedicineService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<MedicineService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicineService(
        IUnitOfWork unitOfWork,
        ILogger<MedicineService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }


    // =====================================================
    // SEARCH MEDICINES
    //
    // GET:
    // /api/medicines/search?keyword=&patientId=
    // =====================================================

    public async Task<
        HttpResponseData<List<MedicineSearchDTO>>>
        SearchMedicinesAsync(
            string? keyword,
            int patientId)
    {
        try
        {
            // =================================================
            // VALIDATE PATIENT ID
            // =================================================

            if (patientId <= 0)
            {
                return new HttpResponseData<
                    List<MedicineSearchDTO>>
                {
                    StatusCode = 400,
                    Message =
                        "Mã bệnh nhân không hợp lệ."
                };
            }


            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.Id == patientId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return new HttpResponseData<
                    List<MedicineSearchDTO>>
                {
                    StatusCode = 404,
                    Message =
                        "Không tìm thấy bệnh nhân."
                };
            }


            // =================================================
            // NORMALIZE KEYWORD
            // =================================================

            keyword =
                string.IsNullOrWhiteSpace(keyword)
                    ? null
                    : keyword.Trim();


            // =================================================
            // GET MEDICINES
            //
            // Chỉ lấy thuốc đang active.
            // =================================================

            var medicineQuery =
                _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m => m.IsActive
                    );


            // =================================================
            // SEARCH
            //
            // Tìm theo:
            // - Name
            // - ActiveIngredient
            // =================================================

            if (keyword != null)
            {
                medicineQuery =
                    medicineQuery.Where(
                        m =>
                            m.Name.Contains(keyword) ||
                            (
                                m.ActiveIngredient != null &&
                                m.ActiveIngredient.Contains(keyword)
                            )
                    );
            }


            // =================================================
            // GET MEDICINES
            // =================================================

            var medicines =
                await medicineQuery
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.Id)
                    .ToListAsync();


            // =================================================
            // GET PATIENT ALLERGIES
            // =================================================

            var allergens =
                await _unitOfWork
                    .PatientAllergyRepository
                    .WhereSql(
                        a =>
                            a.PatientId == patientId
                    )
                    .Select(
                        a => a.Allergen
                    )
                    .Where(
                        a =>
                            !string.IsNullOrWhiteSpace(a)
                    )
                    .ToListAsync();


            // =================================================
            // NORMALIZE ALLERGIES
            // =================================================

            var normalizedAllergens =
                allergens
                    .Select(
                        a => a.Trim()
                    )
                    .Where(
                        a =>
                            a.Length > 0
                    )
                    .ToList();


            // =================================================
            // CURRENT DATE
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            // =================================================
            // MAP RESULT
            // =================================================

            var result =
                new List<MedicineSearchDTO>();


            foreach (var medicine in medicines)
            {
                // =============================================
                // GET VALID BATCHES
                //
                // Chỉ tính:
                // - Quantity > 0
                // - Chưa hết hạn
                // =============================================

                var validBatches =
                    await _unitOfWork
                        .MedicineBatchRepository
                        .WhereSql(
                            b =>
                                b.MedicineId ==
                                    medicine.Id &&

                                b.Quantity > 0 &&

                                b.ExpiryDate >= today
                        )
                        .OrderBy(
                            b => b.ExpiryDate
                        )
                        .ThenBy(
                            b => b.Id
                        )
                        .ToListAsync();


                // =============================================
                // AVAILABLE QUANTITY
                // =============================================

                var availableQuantity =
                    validBatches.Sum(
                        b => b.Quantity
                    );


                // =============================================
                // NEAREST EXPIRY BATCH
                // =============================================

                var nearestBatch =
                    validBatches
                        .FirstOrDefault();


                // =============================================
                // ALLERGY CHECK
                //
                // So sánh active ingredient với allergen.
                // =============================================

                var hasAllergyWarning = false;

                string? allergyWarning = null;


                if (
                    !string.IsNullOrWhiteSpace(
                        medicine.ActiveIngredient
                    ) &&
                    normalizedAllergens.Count > 0
                )
                {
                    var activeIngredient =
                        medicine.ActiveIngredient
                            .Trim();


                    var matchedAllergen =
                        normalizedAllergens
                            .FirstOrDefault(
                                allergen =>
                                    string.Equals(
                                        allergen,
                                        activeIngredient,
                                        StringComparison
                                            .OrdinalIgnoreCase
                                    )
                            );


                    if (matchedAllergen != null)
                    {
                        hasAllergyWarning = true;

                        allergyWarning =
                            $"Bệnh nhân có ghi nhận dị ứng với " +
                            $"{matchedAllergen}.";
                    }
                }


                // =============================================
                // ADD RESULT
                // =============================================

                result.Add(
                    new MedicineSearchDTO
                    {
                        Id =
                            medicine.Id,

                        Name =
                            medicine.Name,

                        Unit =
                            medicine.Unit,

                        Price =
                            medicine.Price,

                        ActiveIngredient =
                            medicine.ActiveIngredient,

                        Concentration =
                            medicine.Concentration,

                        AvailableQuantity =
                            availableQuantity,

                        BatchNo =
                            nearestBatch?.BatchNo,

                        ExpiryDate =
                            nearestBatch?.ExpiryDate,

                        HasAllergyWarning =
                            hasAllergyWarning,

                        AllergyWarning =
                            allergyWarning
                    }
                );
            }


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                List<MedicineSearchDTO>>
            {
                StatusCode = 200,

                Message =
                    "Tìm thuốc thành công.",

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to search medicines. " +
                "Keyword: {Keyword}, " +
                "PatientId: {PatientId}",
                keyword,
                patientId
            );


            return new HttpResponseData<
                List<MedicineSearchDTO>>
            {
                StatusCode = 500,

                Message =
                    "Không thể tìm kiếm thuốc."
            };
        }
    }
}