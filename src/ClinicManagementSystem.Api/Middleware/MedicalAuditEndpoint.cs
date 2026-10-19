using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ClinicManagementSystem.Api.Middleware;

// Explicit controller/action mapping avoids recording catalogs, raw URLs, or
// mistaking an appointment/patient ID for a medical-record ID.
public sealed record MedicalAuditEndpoint(string Action, string Entity,
    string? TargetRoute = null, string IdProperty = "Id", string? CollectionProperty = null,
    string? ParentReference = null)
{
    public static MedicalAuditEndpoint? Resolve(ControllerActionDescriptor? action) =>
        (action?.ControllerName, action?.ActionName) switch
        {
            ("MedicalRecord", "GetMedicalRecord") => new("VIEW_RECORD", "medical_records"),
            ("MedicalRecord", "GetPatientMedicalRecords") => new("VIEW_RECORD_HISTORY", "medical_records"),
            ("MedicalRecord", "GetMedicalRecordByIdForDoctor") => new("VIEW_RECORD", "medical_records", "id"),
            ("MedicalRecord", "UpdateMedicalRecordDraft") => new("UPDATE_RECORD", "medical_records", "id"),
            ("MedicalRecord", "FinalizeMedicalRecord") => new("FINALIZE_RECORD", "medical_records", "id"),
            ("MedicalRecord", "AddMedicalRecordService") => new("CREATE_RECORD_SERVICE", "medical_record_services", ParentReference: "medicalRecordId"),
            ("MedicalRecord", "CancelMedicalRecordService") => new("CANCEL_RECORD_SERVICE", "medical_record_services", "id"),
            ("MedicalRecord", "AddMedicalRecordServiceResult") => new("UPSERT_LAB_RESULT", "lab_results", IdProperty: "Result.Id", ParentReference: "medicalRecordServiceId"),
            ("MedicalRecord", "UploadMedicalRecordAttachment") => new("CREATE_ATTACHMENT", "attachments", ParentReference: "medicalRecordId"),
            ("MedicalRecord", "CreatePrescription") => new("UPSERT_PRESCRIPTION", "prescriptions", ParentReference: "medicalRecordId"),
            ("MedicalRecord", "GetPrescriptionPdf") => new("DOWNLOAD_PRESCRIPTION", "prescriptions", "id"),
            ("PatientRecord", "GetPatientRecords" or "GetPatientRecordsForReception") => new("VIEW_RECORD_HISTORY", "medical_records", ParentReference: "patientId"),
            ("Attachment", "DownloadAttachment") => new("DOWNLOAD_ATTACHMENT", "attachments", "id"),
            ("Patient", "LookupByPhone" or "SearchPatients") => new("VIEW_PATIENT", "patients", CollectionProperty: "Items"),
            ("Patient", "CreatePatient" or "CreateQuickPatient") => new("CREATE_PATIENT", "patients"),
            ("Patient", "UpdatePatient") => new("UPDATE_PATIENT", "patients", "id"),
            ("Patient", "GetPatientAllergies") => new("VIEW_ALLERGY", "patient_allergies", ParentReference: "patientId"),
            ("Patient", "AddPatientAllergy") => new("CREATE_ALLERGY", "patient_allergies", ParentReference: "patientId"),
            ("Patient", "DeletePatientAllergy") => new("DELETE_ALLERGY", "patient_allergies", "id"),
            ("Pharmacy", "GetPendingPrescriptions") => new("VIEW_PRESCRIPTION", "prescriptions", IdProperty: "PrescriptionId", CollectionProperty: "Items"),
            ("Pharmacy", "GetPrescriptionDetail") => new("VIEW_PRESCRIPTION", "prescriptions", "id", "PrescriptionId"),
            ("Pharmacy", "Dispense") => new("DISPENSE_PRESCRIPTION", "prescriptions", "id"),
            ("Pharmacy", "ReportShortage") => new("REPORT_SHORTAGE", "prescriptions", "id"),
            ("Appointment", "StartExam") => new("START_EXAM", "medical_records", ParentReference: "appointmentId"),
            ("Appointment", "CreateAppointment" or "CreateReceptionAppointment") => new("CREATE_APPOINTMENT", "appointments"),
            ("Appointment", "GetMyAppointments" or "GetReceptionAppointments") => new("VIEW_APPOINTMENT", "appointments", CollectionProperty: "Items"),
            ("Appointment", "CancelAppointment") => new("CANCEL_APPOINTMENT", "appointments", "id"),
            ("Appointment", "RescheduleAppointment") => new("RESCHEDULE_APPOINTMENT", "appointments", "id"),
            ("Appointment", "CheckInAppointment") => new("CHECK_IN_APPOINTMENT", "appointments", "id"),
            ("Appointment", "MarkNoShow") => new("MARK_NO_SHOW", "appointments", "id"),
            ("ReceptionAppointment", "GetAffected") => new("VIEW_APPOINTMENT", "appointments", CollectionProperty: "Items"),
            ("ReceptionAppointment", "Reschedule") => new("RESCHEDULE_APPOINTMENT", "appointments", "id"),
            ("ReceptionAppointment", "Cancel") => new("CANCEL_APPOINTMENT", "appointments", "id"),
            ("DoctorMedicalRecords", "GetMyMedicalRecords") => new("VIEW_RECORD_HISTORY", "medical_records", CollectionProperty: "Items"),
            ("Doctor", "GetMyQueue") => new("VIEW_QUEUE", "appointments", IdProperty: "AppointmentId"),
            ("Queue", "GetQueue") => new("VIEW_QUEUE", "appointments", IdProperty: "AppointmentId", CollectionProperty: "Waiting"),
            ("Queue", "Recall") => new("RECALL_APPOINTMENT", "appointments", "appointmentId"),
            ("Queue", "Defer") => new("DEFER_APPOINTMENT", "appointments", "appointmentId"),
            ("PatientInvoice", "GetPatientInvoices" or "GetPatientInvoicesAsync") => new("VIEW_INVOICE", "invoices"),
            ("AdminScheduling", "CreateSchedule" or "UpdateSchedule" or "CreateTimeOff") => new("VIEW_AFFECTED_APPOINTMENTS", "appointments", CollectionProperty: "AffectedAppointments"),
            ("Invoice", "GetInvoice") => new("VIEW_INVOICE", "invoices", "id"),
            ("Invoice", "GetInvoicePdf") => new("DOWNLOAD_INVOICE", "invoices", "id"),
            ("Invoice", "CreatePayment") => new("PAY_INVOICE", "invoices", "id"),
            ("Invoice", "RefundPayment") => new("REFUND_INVOICE", "invoices", "id"),
            ("Invoice", "CancelInvoice") => new("CANCEL_INVOICE", "invoices", "id"),
            ("Notification", "GetMyNotifications") => new("VIEW_NOTIFICATIONS", "notifications"),
            _ => null
        };

    public IReadOnlyList<int> GetReturnedIds(object? envelope)
    {
        // Read only the response envelope and whitelisted numeric ID fields.
        // Do not serialize/reflection-walk medical or identifying text fields.
        var content = Property(envelope, "Content");
        if (content == null) return Array.Empty<int>();
        var values = new HashSet<int>();
        Collect(content, values);
        if (CollectionProperty != null) Collect(Property(content, CollectionProperty), values);
        if (Entity == "appointments" && Action == "VIEW_QUEUE")
        {
            Collect(Property(content, "Current"), values);
            Collect(Property(content, "Next"), values);
        }
        return values.Order().ToArray();
    }

    private void Collect(object? content, HashSet<int> ids)
    {
        if (content is IEnumerable items && content is not string)
        {
            foreach (var item in items) AddId(item, ids);
        }
        else AddId(content, ids);
    }

    private void AddId(object? value, HashSet<int> ids)
    {
        if (Property(value, IdProperty) is int id && id > 0) ids.Add(id);
    }

    private static object? Property(object? value, string name)
    {
        foreach (var segment in name.Split('.'))
            value = value?.GetType().GetProperty(segment)?.GetValue(value);
        return value;
    }

    public Dictionary<string, int> GetReferences(HttpContext context)
    {
        var result = new Dictionary<string, int>();
        foreach (var key in new[] { "patientId", "appointmentId", "doctorId", "medicalRecordId" })
        {
            if (TryId(context.Request.RouteValues[key], out var id) ||
                TryId(context.Request.Query[key].ToString(), out id)) result[key] = id;
        }
        if (ParentReference != null && TryId(context.Request.RouteValues["id"], out var parent))
            result[ParentReference] = parent;
        return result;
    }

    public static bool TryId(object? value, out int id) =>
        int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.None,
            CultureInfo.InvariantCulture, out id) && id > 0;
}
