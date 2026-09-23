using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PATIENT INVOICE SERVICE CONTRACT
// =====================================================

public interface IPatientInvoiceService
{
    Task<HttpResponseData<List<PatientInvoiceDTO>>> GetPatientInvoicesAsync(int currentUserId);
}

// =====================================================
// PATIENT INVOICE SERVICE
// =====================================================

public class PatientInvoiceService : IPatientInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientInvoiceService> _logger;


    // =================================================
    // CONSTRUCTOR
    // =================================================

    public PatientInvoiceService(IUnitOfWork unitOfWork, ILogger<PatientInvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    // =================================================
    // GET PATIENT INVOICES
    // =================================================

    public async Task<HttpResponseData<List<PatientInvoiceDTO>>> GetPatientInvoicesAsync(
        int currentUserId)
    {
        try
        {
            // =============================================
            // RESOLVE CURRENT PATIENT
            // =============================================

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return new HttpResponseData<List<PatientInvoiceDTO>>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy thông tin bệnh nhân.",
                    Content = new List<PatientInvoiceDTO>()
                };
            }


            // =============================================
            // GET PATIENT INVOICES
            // =============================================

            var invoices = await _unitOfWork.InvoiceRepository
                .WhereSql(i => i.PatientId == patient.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();


            // =============================================
            // NO INVOICES
            // =============================================

            if (invoices.Count == 0)
            {
                return new HttpResponseData<List<PatientInvoiceDTO>>
                {
                    StatusCode = 200,
                    Message = "Bệnh nhân chưa có hóa đơn.",
                    Content = new List<PatientInvoiceDTO>()
                };
            }


            // =============================================
            // TEMPORARY RESPONSE
            // =============================================

            return new HttpResponseData<List<PatientInvoiceDTO>>
            {
                StatusCode = 200,
                Message = $"Tìm thấy {invoices.Count} hóa đơn.",
                Content = new List<PatientInvoiceDTO>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting invoices for user {UserId}",
                currentUserId
            );

            return new HttpResponseData<List<PatientInvoiceDTO>>
            {
                StatusCode = 500,
                Message = "Đã xảy ra lỗi khi lấy hóa đơn.",
                Content = new List<PatientInvoiceDTO>()
            };
        }
    }
}