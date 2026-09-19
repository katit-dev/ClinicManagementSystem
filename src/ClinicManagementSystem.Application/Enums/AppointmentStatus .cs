namespace ClinicManagementSystem.Application.Enums;


// =====================================================
// APPOINTMENT STATUS
// =====================================================

public enum AppointmentStatus : byte
{
    Pending = 0,

    Confirmed = 1,

    CheckedIn = 2,

    Completed = 3,

    Cancelled = 4,

    NoShow = 5
}