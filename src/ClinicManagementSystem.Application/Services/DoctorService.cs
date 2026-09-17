using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


public interface IDoctorService
{
    Task<HttpResponseData<List<DoctorDTO>>>GetDoctorsBySpecialtyAsync(int specialtyId);
    Task<HttpResponseData<List<SlotDTO>>>GetAvailableSlotsAsync(int doctorId, DateOnly date);
}


public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<DoctorService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorService(
        IUnitOfWork unitOfWork,
        ILogger<DoctorService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }


    // =====================================================
    // GET DOCTORS BY SPECIALTY
    // =====================================================

    public async Task<
        HttpResponseData<List<DoctorDTO>>>
        GetDoctorsBySpecialtyAsync(
            int specialtyId)
    {
        try
        {
            // =================================================
            // VALIDATE SPECIALTY ID
            // =================================================

            if (specialtyId <= 0)
            {
                return Response(
                    400,
                    DoctorResponseMessageDTO
                        .InvalidSpecialty
                );
            }


            // =================================================
            // QUERY DOCTORS
            //
            // Chỉ lấy:
            // - đúng chuyên khoa
            // - bác sĩ đang hoạt động
            // =================================================

            var doctors =
                await _unitOfWork.DoctorRepository
                    .WhereSql(
                        d =>
                            d.SpecialtyId == specialtyId &&
                            d.IsActive
                    )
                    .OrderBy(d => d.FullName)
                    .Select(
                        d => new DoctorDTO
                        {
                            Id = d.Id,

                            SpecialtyId =
                                d.SpecialtyId,

                            FullName =
                                d.FullName,

                            Title =
                                d.Title,

                            Room =
                                d.Room,

                            ConsultationFee =
                                d.ConsultationFee,

                            AvatarUrl =
                                d.AvatarUrl,

                            ExperienceYears =
                                d.ExperienceYears
                        }
                    )
                    .ToListAsync();


            // =================================================
            // SUCCESS
            //
            // Không có bác sĩ vẫn trả 200 + []
            // =================================================

            return Response(
                200,
                DoctorResponseMessageDTO
                    .GetListSuccess,
                doctors
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get doctors by specialty. " +
                "SpecialtyId: {SpecialtyId}",
                specialtyId
            );


            // =================================================
            // FAILED
            // =================================================

            return Response(
                500,
                DoctorResponseMessageDTO
                    .GetListFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static
        HttpResponseData<List<DoctorDTO>>
        Response(
            int statusCode,
            string message,
            List<DoctorDTO>? content = null)
    {
        return new HttpResponseData<List<DoctorDTO>>
        {
            StatusCode = statusCode,

            Message = message,

            Content =
                content ??
                new List<DoctorDTO>()
        };
    }
}