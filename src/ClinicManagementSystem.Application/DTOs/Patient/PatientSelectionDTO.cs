namespace ClinicManagementSystem.Application.DTOs.Patient;


public class PatientSelectionDTO
{
    // =====================================================
    // PATIENT ID
    // =====================================================

    public int Id { get; set; }



    // =====================================================
    // DISPLAY
    // =====================================================

    public string PatientCode { get; set; }
        = string.Empty;


    public string FullName { get; set; }
        = string.Empty;


    public string Phone { get; set; }
        = string.Empty;


    public DateOnly? DateOfBirth { get; set; }
}