using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// SERVICE CATALOG SERVICE CONTRACT
// =====================================================

public interface IServiceCatalogService
{
    Task<HttpResponseData<List<ServiceDTO>>>
        GetActiveServicesAsync();
}


// =====================================================
// SERVICE CATALOG SERVICE
// =====================================================

public class ServiceCatalogService
    : IServiceCatalogService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<ServiceCatalogService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public ServiceCatalogService(
        IUnitOfWork unitOfWork,
        ILogger<ServiceCatalogService> logger)
    {
        _unitOfWork =
            unitOfWork;

        _logger =
            logger;
    }


    // =====================================================
    // GET ACTIVE SERVICES
    //
    // GET:
    // /api/services
    //
    // Chỉ lấy service đang Active.
    // =====================================================

    public async Task<
        HttpResponseData<List<ServiceDTO>>>
        GetActiveServicesAsync()
    {
        try
        {
            // =================================================
            // GET ACTIVE SERVICES
            // =================================================

            var services =
                await _unitOfWork
                    .ServiceRepository
                    .WhereSql(
                        s => s.IsActive
                    )
                    .OrderBy(
                        s => s.Name
                    )
                    .ToListAsync();


            // =================================================
            // MAP DTO
            // =================================================

            var result =
                services
                    .Select(
                        service =>
                            new ServiceDTO
                            {
                                Id =
                                    service.Id,

                                Code =
                                    service.Code,

                                Name =
                                    service.Name,

                                Description =
                                    service.Description,

                                Price =
                                    service.Price,

                                IsActive =
                                    service.IsActive
                            }
                    )
                    .ToList();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<List<ServiceDTO>>
            {
                StatusCode = 200,

                Message =
                    "Lấy danh sách dịch vụ thành công.",

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get active services."
            );


            // =================================================
            // FAILED
            // =================================================

            return new HttpResponseData<List<ServiceDTO>>
            {
                StatusCode = 500,

                Message =
                    "Không thể lấy danh sách dịch vụ.",

                Content = null
            };
        }
    }




}