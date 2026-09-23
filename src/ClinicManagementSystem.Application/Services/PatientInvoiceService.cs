using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PATIENT INVOICE SERVICE CONTRACT
// =====================================================

public interface IPatientInvoiceService
{
    Task<HttpResponseData<List<PatientInvoiceDTO>>> GetPatientInvoicesAsync(
        int currentUserId
    );
}