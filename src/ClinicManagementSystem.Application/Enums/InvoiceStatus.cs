namespace ClinicManagementSystem.Application.Enums;


// =====================================================
// INVOICE STATUS
// =====================================================

public enum InvoiceStatus : byte
{
    Unpaid = 0,

    PartiallyPaid = 1,

    Paid = 2,

    Cancelled = 3
}