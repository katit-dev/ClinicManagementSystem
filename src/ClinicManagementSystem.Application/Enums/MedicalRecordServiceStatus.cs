namespace ClinicManagementSystem.Application.Enums;

public enum MedicalRecordServiceStatus : byte
{
    Ordered = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}