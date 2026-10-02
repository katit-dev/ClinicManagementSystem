namespace ClinicManagementSystem.Application.Enums;

public enum PrescriptionStatus : byte
{
    Draft = 0,
    Finalized = 1,
    Dispensed = 2,
    Cancelled = 3
}