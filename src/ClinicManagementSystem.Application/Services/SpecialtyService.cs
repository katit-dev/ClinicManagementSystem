using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


public interface ISpecialtyService
{
    Task<HttpResponseData<List<SpecialtyDTO>>>
        GetSpecialtiesAsync(bool? active);
}


public class SpecialtyService : ISpecialtyService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<SpecialtyService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public SpecialtyService(
        IUnitOfWork unitOfWork,
        ILogger<SpecialtyService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }


    // =====================================================
    // GET SPECIALTIES
    //
    // active = true
    // → chỉ lấy chuyên khoa đang hoạt động
    //
    // active = false
    // → chỉ lấy chuyên khoa ngừng hoạt động
    //
    // active = null
    // → lấy tất cả
    // =====================================================

    public async Task<
        HttpResponseData<List<SpecialtyDTO>>>
        GetSpecialtiesAsync(bool? active)
    {
        try
        {
            // =================================================
            // QUERY DATABASE
            // =================================================

            var specialties =
                await _unitOfWork.SpecialtyRepository
                    .WhereSql(
                        s =>
                            !active.HasValue ||
                            s.IsActive == active.Value
                    )
                    .OrderBy(s => s.Name)
                    .Select(
                        s => new SpecialtyDTO
                        {
                            Id = s.Id,
                            Name = s.Name
                        }
                    )
                    .ToListAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                200,
                SpecialtyResponseMessageDTO
                    .GetListSuccess,
                specialties
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get specialties."
            );


            // =================================================
            // FAILED
            // =================================================

            return Response(
                500,
                SpecialtyResponseMessageDTO
                    .GetListFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static HttpResponseData<List<SpecialtyDTO>>
        Response(
            int statusCode,
            string message,
            List<SpecialtyDTO>? content = null)
    {
        return new HttpResponseData<List<SpecialtyDTO>>
        {
            StatusCode = statusCode,

            Message = message,

            Content =
                content ??
                new List<SpecialtyDTO>()
        };
    }
}