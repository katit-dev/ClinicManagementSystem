using System.Security.Claims;

using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Application.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// MEDICINE CONTROLLER
// =====================================================

[ApiController]
[Route("api/medicines")]
[Authorize(Roles = "Pharmacist")]
public class MedicineController
    : ControllerBase
{
    private readonly IMedicineInventoryService
        _medicineInventoryService;


    public MedicineController(
        IMedicineInventoryService medicineInventoryService)
    {
        _medicineInventoryService =
            medicineInventoryService;
    }


    // =====================================================
    // GET INVENTORY
    //
    // GET:
    // /api/medicines/inventory
    //
    // Example:
    // /api/medicines/inventory?lowStock=false&nearExpiry=30
    // =====================================================

    [HttpGet("inventory")]
    public async Task<IActionResult>
        GetInventory(
            [FromQuery] bool lowStock = false,
            [FromQuery] int nearExpiry = 30)
    {
        var result =
            await _medicineInventoryService
                .GetInventoryAsync(
                    lowStock,
                    nearExpiry
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }


    // =====================================================
    // ADJUST STOCK
    //
    // POST:
    // /api/medicines/{id}/adjust
    // =====================================================

    [HttpPost("{id:int}/adjust")]
    public async Task<IActionResult>
        AdjustStock(
            int id,
            [FromBody]
            AdjustMedicineStockRequestDTO request)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        var result =
            await _medicineInventoryService
                .AdjustStockAsync(
                    id,
                    currentUserId,
                    request
                );


        return StatusCode(
            result.StatusCode,
            result
        );
    }


    // =====================================================
    // GET STOCK TRANSACTIONS
    //
    // GET:
    // /api/medicines/{id}/transactions
    //
    // Example:
    // /api/medicines/1/transactions
    //
    // Optional:
    // ?from=2026-10-01
    // &to=2026-10-02
    // =====================================================

    [HttpGet("{id:int}/transactions")]
    public async Task<IActionResult>
        GetTransactions(
            int id,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
    {
        var result =
            await _medicineInventoryService
                .GetTransactionsAsync(
                    id,
                    from,
                    to
                );


        return StatusCode(
            result.StatusCode,
            result
        );
    }
}