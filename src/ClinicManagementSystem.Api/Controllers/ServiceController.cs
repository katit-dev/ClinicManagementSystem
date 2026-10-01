using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// SERVICE CONTROLLER
// =====================================================

[ApiController]
[Route("api/services")]
public class ServiceController : ControllerBase
{
    private readonly IServiceCatalogService
        _serviceCatalogService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ServiceController(
        IServiceCatalogService serviceCatalogService)
    {
        _serviceCatalogService =
            serviceCatalogService;
    }


    // =====================================================
    // GET ACTIVE SERVICES
    //
    // GET:
    // /api/services
    //
    // Doctor dùng API này để lấy danh sách
    // dịch vụ có thể chỉ định.
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetActiveServices()
    {
        // =================================================
        // GET ACTIVE SERVICES
        // =================================================

        var result =
            await _serviceCatalogService
                .GetActiveServicesAsync();


        // =================================================
        // RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}