using System;
using System.Collections.Generic;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Data;

public partial class ClinicManagementDbContext : DbContext
{
    public ClinicManagementDbContext(DbContextOptions<ClinicManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentStatusHistory> AppointmentStatusHistories { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<DoctorSchedule> DoctorSchedules { get; set; }

    public virtual DbSet<DoctorTimeOff> DoctorTimeOffs { get; set; }

    public virtual DbSet<InsurancePolicy> InsurancePolicies { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<LabResult> LabResults { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<MedicalRecordService> MedicalRecordServices { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<MedicineBatch> MedicineBatches { get; set; }

    public virtual DbSet<MedicineStockTransaction> MedicineStockTransactions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientAllergy> PatientAllergies { get; set; }

    public virtual DbSet<PatientVital> PatientVitals { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<PrescriptionItem> PrescriptionItems { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Specialty> Specialties { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_appointments");

            entity.ToTable("appointments", "scheduling");

            entity.HasIndex(e => new { e.DoctorId, e.StartTime }, "IX_appointments_doctor_start");

            entity.HasIndex(e => e.PatientId, "IX_appointments_patient_id");

            entity.HasIndex(e => e.StartTime, "IX_appointments_start_time");

            entity.HasIndex(e => new { e.DoctorId, e.StartTime }, "UX_appointments_doctor_slot")
                .IsUnique()
                .HasFilter("([status]<(4))");

            entity.HasIndex(e => e.AppointmentCode, "UX_scheduling_appointments_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("appointment_code");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(500)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CheckedInAt).HasColumnName("checked_in_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_appointments_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.FeeSnapshot)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("fee_snapshot");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.QueueNumber).HasColumnName("queue_number");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Source).HasColumnName("source");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_scheduling_appointments_users");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_appointments_doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_appointments_patients");
        });

        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_appointment_status_history");

            entity.ToTable("appointment_status_history", "scheduling");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_appointment_status_history_changed_at")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.FromStatus).HasColumnName("from_status");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.ToStatus).HasColumnName("to_status");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentStatusHistories)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_appointment_status_history_appointments");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.AppointmentStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK_scheduling_appointment_status_history_users");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_attachments");

            entity.ToTable("attachments", "clinical");

            entity.HasIndex(e => e.MedicalRecordId, "IX_clinical_attachments_record");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("file_type");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(1000)
                .HasColumnName("file_url");
            entity.Property(e => e.MedicalRecordId).HasColumnName("medical_record_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_attachments_uploaded_at")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_attachments_records");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK_clinical_attachments_users");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_audit_logs");

            entity.ToTable("audit_logs", "auth");

            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.OccurredAt }, "IX_auth_audit_logs_entity");

            entity.HasIndex(e => new { e.UserId, e.OccurredAt }, "IX_auth_audit_logs_user_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityName)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("entity_name");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_auth_audit_logs_occurred_at")
                .HasColumnName("occurred_at");
            entity.Property(e => e.Succeeded)
                .HasDefaultValue(true, "DF_auth_audit_logs_succeeded")
                .HasColumnName("succeeded");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_auth_audit_logs_users");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_doctors");

            entity.ToTable("doctors", "scheduling");

            entity.HasIndex(e => e.UserId, "UQ_scheduling_doctors_user_id").IsUnique();

            entity.HasIndex(e => e.LicenseNumber, "UX_scheduling_doctors_license_number")
                .IsUnique()
                .HasFilter("([license_number] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Bio)
                .HasMaxLength(2000)
                .HasColumnName("bio");
            entity.Property(e => e.ConsultationFee)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("consultation_fee");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_doctors_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.ExperienceYears).HasColumnName("experience_years");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_scheduling_doctors_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("license_number");
            entity.Property(e => e.MaxPatientsPerDay).HasColumnName("max_patients_per_day");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Room)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("room");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Specialty).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_doctors_specialties");

            entity.HasOne(d => d.User).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_doctors_users");
        });

        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_doctor_schedules");

            entity.ToTable("doctor_schedules", "scheduling");

            entity.HasIndex(e => new { e.DoctorId, e.DayOfWeek, e.StartTime, e.EffectiveFrom }, "UX_scheduling_schedules_version").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BreakEnd).HasColumnName("break_end");
            entity.Property(e => e.BreakStart).HasColumnName("break_start");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_scheduling_doctor_schedules_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.SlotMinutes)
                .HasDefaultValue(30, "DF_scheduling_doctor_schedules_slot_minutes")
                .HasColumnName("slot_minutes");
            entity.Property(e => e.StartTime).HasColumnName("start_time");

            entity.HasOne(d => d.Doctor).WithMany(p => p.DoctorSchedules)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_scheduling_doctor_schedules_doctors");
        });

        modelBuilder.Entity<DoctorTimeOff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_doctor_time_off");

            entity.ToTable("doctor_time_off", "scheduling");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_doctor_time_off_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.IsFullDay).HasColumnName("is_full_day");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasColumnName("reason");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.DoctorTimeOffs)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_scheduling_time_off_approved_by");

            entity.HasOne(d => d.Doctor).WithMany(p => p.DoctorTimeOffs)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_scheduling_doctor_time_off_doctors");
        });

        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_billing_insurance_policies");

            entity.ToTable("insurance_policies", "billing");

            entity.HasIndex(e => e.PatientId, "IX_billing_insurance_patient");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoveragePercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("coverage_percent");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_billing_insurance_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_billing_insurance_active")
                .HasColumnName("is_active");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.PolicyNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("policy_number");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(255)
                .HasColumnName("provider_name");
            entity.Property(e => e.RouteType).HasColumnName("route_type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");

            entity.HasOne(d => d.Patient).WithMany(p => p.InsurancePolicies)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_billing_insurance_patients");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_billing_invoices");

            entity.ToTable("invoices", "billing");

            entity.HasIndex(e => e.AppointmentId, "IX_invoices_appointment_id");

            entity.HasIndex(e => e.PatientId, "IX_invoices_patient_id");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_invoices_status_created_at");

            entity.HasIndex(e => e.InvoiceNo, "UX_billing_invoices_no").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(500)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_billing_invoices_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.InsuranceAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("insurance_amount");
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("invoice_no");
            entity.Property(e => e.MedicalRecordId).HasColumnName("medical_record_id");
            entity.Property(e => e.PaidAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("paid_amount");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.PatientName)
                .HasMaxLength(255)
                .HasColumnName("patient_name");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("tax_amount");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK_billing_invoices_appointments");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_billing_invoices_users");

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.MedicalRecordId)
                .HasConstraintName("FK_billing_invoices_medical_records");

            entity.HasOne(d => d.Patient).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_billing_invoices_patients");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_billing_invoice_items");

            entity.ToTable("invoice_items", "billing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.MedicalRecordServiceId).HasColumnName("medical_record_service_id");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1, "DF_billing_invoice_items_quantity")
                .HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_billing_invoice_items_invoices");

            entity.HasOne(d => d.MedicalRecordService).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.MedicalRecordServiceId)
                .HasConstraintName("FK_billing_invoice_items_order");

            entity.HasOne(d => d.Medicine).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK_billing_invoice_items_medicines");
        });

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_lab_results");

            entity.ToTable("lab_results", "clinical");

            entity.HasIndex(e => e.MedicalRecordServiceId, "UQ_clinical_lab_results_order").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Conclusion)
                .HasMaxLength(4000)
                .HasColumnName("conclusion");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_lab_results_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.MedicalRecordServiceId).HasColumnName("medical_record_service_id");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by");
            entity.Property(e => e.ReferenceRange)
                .HasMaxLength(500)
                .HasColumnName("reference_range");
            entity.Property(e => e.ResultValue)
                .HasMaxLength(4000)
                .HasColumnName("result_value");
            entity.Property(e => e.ResultedAt).HasColumnName("resulted_at");

            entity.HasOne(d => d.MedicalRecordService).WithOne(p => p.LabResult)
                .HasForeignKey<LabResult>(d => d.MedicalRecordServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_lab_results_order");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.LabResults)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_clinical_lab_results_users");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_medical_records");

            entity.ToTable("medical_records", "clinical", tb => tb.HasTrigger("trg_medical_records_no_edit_finalized"));

            entity.HasIndex(e => e.AppointmentId, "UQ_clinical_medical_records_appointment_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_medical_records_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Diagnosis)
                .HasMaxLength(4000)
                .HasColumnName("diagnosis");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.FinalizedAt).HasColumnName("finalized_at");
            entity.Property(e => e.FollowUpDate).HasColumnName("follow_up_date");
            entity.Property(e => e.Icd10Code)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("icd10_code");
            entity.Property(e => e.Note)
                .HasMaxLength(4000)
                .HasColumnName("note");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Symptoms)
                .HasMaxLength(4000)
                .HasColumnName("symptoms");
            entity.Property(e => e.TreatmentPlan)
                .HasMaxLength(4000)
                .HasColumnName("treatment_plan");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Appointment).WithOne(p => p.MedicalRecord)
                .HasForeignKey<MedicalRecord>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_medical_records_appointments");

            entity.HasOne(d => d.Doctor).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_medical_records_doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_medical_records_patients");
        });

        modelBuilder.Entity<MedicalRecordService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_medical_record_services");

            entity.ToTable("medical_record_services", "clinical");

            entity.HasIndex(e => new { e.MedicalRecordId, e.Status }, "IX_clinical_mrs_record_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.MedicalRecordId).HasColumnName("medical_record_id");
            entity.Property(e => e.OrderedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_mrs_ordered_at")
                .HasColumnName("ordered_at");
            entity.Property(e => e.PerformedBy).HasColumnName("performed_by");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1, "DF_clinical_mrs_quantity")
                .HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UnitPriceSnapshot)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("unit_price_snapshot");

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.MedicalRecordServices)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_mrs_records");

            entity.HasOne(d => d.PerformedByNavigation).WithMany(p => p.MedicalRecordServices)
                .HasForeignKey(d => d.PerformedBy)
                .HasConstraintName("FK_clinical_mrs_performed_by");

            entity.HasOne(d => d.Service).WithMany(p => p.MedicalRecordServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_mrs_services");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_medicines");

            entity.ToTable("medicines", "clinical");

            entity.HasIndex(e => e.Code, "UX_clinical_medicines_code")
                .IsUnique()
                .HasFilter("([code] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActiveIngredient)
                .HasMaxLength(255)
                .HasColumnName("active_ingredient");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Concentration)
                .HasMaxLength(100)
                .HasColumnName("concentration");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("cost_price");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_medicines_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_clinical_medicines_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.MinStock).HasColumnName("min_stock");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("price");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<MedicineBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_medicine_batches");

            entity.ToTable("medicine_batches", "clinical");

            entity.HasIndex(e => e.ExpiryDate, "IX_clinical_batches_expiry");

            entity.HasIndex(e => new { e.Id, e.MedicineId }, "UQ_clinical_batches_id_medicine").IsUnique();

            entity.HasIndex(e => new { e.MedicineId, e.BatchNo }, "UQ_clinical_batches_medicine_no").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("batch_no");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ImportPrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("import_price");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Medicine).WithMany(p => p.MedicineBatches)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_batches_medicines");
        });

        modelBuilder.Entity<MedicineStockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_medicine_stock_transactions");

            entity.ToTable("medicine_stock_transactions", "clinical");

            entity.HasIndex(e => new { e.BatchId, e.CreatedAt }, "IX_clinical_stock_tx_batch_time");

            entity.HasIndex(e => e.PrescriptionId, "IX_clinical_stock_tx_prescription");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_stock_tx_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PrescriptionId).HasColumnName("prescription_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.QuantityAfter).HasColumnName("quantity_after");
            entity.Property(e => e.QuantityBefore).HasColumnName("quantity_before");
            entity.Property(e => e.ReversalOfTransactionId).HasColumnName("reversal_of_transaction_id");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MedicineStockTransactions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_stock_tx_users");

            entity.HasOne(d => d.Medicine).WithMany(p => p.MedicineStockTransactions)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_stock_tx_medicines");

            entity.HasOne(d => d.Prescription).WithMany(p => p.MedicineStockTransactions)
                .HasForeignKey(d => d.PrescriptionId)
                .HasConstraintName("FK_clinical_stock_tx_prescriptions");

            entity.HasOne(d => d.ReversalOfTransaction).WithMany(p => p.InverseReversalOfTransaction)
                .HasForeignKey(d => d.ReversalOfTransactionId)
                .HasConstraintName("FK_clinical_stock_tx_reversal");

            entity.HasOne(d => d.MedicineBatch).WithMany(p => p.MedicineStockTransactions)
                .HasPrincipalKey(p => new { p.Id, p.MedicineId })
                .HasForeignKey(d => new { d.BatchId, d.MedicineId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_stock_tx_batch_medicine");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_notifications");

            entity.ToTable("notifications", "auth");

            entity.HasIndex(e => e.AppointmentId, "IX_auth_notifications_appointment");

            entity.HasIndex(e => new { e.Status, e.ScheduledAt }, "IX_auth_notifications_status_scheduled");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("channel");
            entity.Property(e => e.Content)
                .HasMaxLength(2000)
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_auth_notifications_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000)
                .HasColumnName("error_message");
            entity.Property(e => e.Recipient)
                .HasMaxLength(255)
                .HasColumnName("recipient");
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_auth_notifications_appointments");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_auth_notifications_users");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens", "auth");

            entity.HasIndex(e => e.UserId, "IX_password_reset_tokens_user_id");

            entity.HasIndex(e => e.TokenHash, "UQ_password_reset_tokens_token_hash").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_password_reset_tokens_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_password_reset_tokens_users");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_patients");

            entity.ToTable("patients", "scheduling");

            entity.HasIndex(e => e.FullName, "IX_patients_full_name");

            entity.HasIndex(e => e.Phone, "IX_patients_phone");

            entity.HasIndex(e => e.PatientCode, "UX_scheduling_patients_code").IsUnique();

            entity.HasIndex(e => e.UserId, "UX_scheduling_patients_user_id")
                .IsUnique()
                .HasFilter("([user_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.BloodType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("blood_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_patients_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmergencyContactName)
                .HasMaxLength(255)
                .HasColumnName("emergency_contact_name");
            entity.Property(e => e.EmergencyContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("emergency_contact_phone");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.InsuranceNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("insurance_number");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_scheduling_patients_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.NationalId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("national_id");
            entity.Property(e => e.PatientCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("patient_code");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.UserId)
                .HasConstraintName("FK_scheduling_patients_users");
        });

        modelBuilder.Entity<PatientAllergy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_patient_allergies");

            entity.ToTable("patient_allergies", "clinical");

            entity.HasIndex(e => e.PatientId, "IX_clinical_patient_allergies_patient");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Allergen)
                .HasMaxLength(255)
                .HasColumnName("allergen");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.Severity).HasColumnName("severity");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientAllergies)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_patient_allergies_patients");
        });

        modelBuilder.Entity<PatientVital>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_patient_vitals");

            entity.ToTable("patient_vitals", "clinical");

            entity.HasIndex(e => e.MedicalRecordId, "UQ_clinical_patient_vitals_record").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BloodPressure)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("blood_pressure");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("height");
            entity.Property(e => e.MedicalRecordId).HasColumnName("medical_record_id");
            entity.Property(e => e.Pulse).HasColumnName("pulse");
            entity.Property(e => e.Temperature)
                .HasColumnType("decimal(4, 1)")
                .HasColumnName("temperature");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(6, 2)")
                .HasColumnName("weight");

            entity.HasOne(d => d.MedicalRecord).WithOne(p => p.PatientVital)
                .HasForeignKey<PatientVital>(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_patient_vitals_records");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_billing_payments");

            entity.ToTable("payments", "billing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.IsRefund).HasColumnName("is_refund");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_billing_payments_paid_at")
                .HasColumnName("paid_at");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_code");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_billing_payments_invoices");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceivedBy)
                .HasConstraintName("FK_billing_payments_received_by");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_prescriptions");

            entity.ToTable("prescriptions", "clinical");

            entity.HasIndex(e => e.MedicalRecordId, "UQ_clinical_prescriptions_medical_record_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_clinical_prescriptions_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DispensedAt).HasColumnName("dispensed_at");
            entity.Property(e => e.DispensedBy).HasColumnName("dispensed_by");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.MedicalRecordId).HasColumnName("medical_record_id");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.DispensedByNavigation).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.DispensedBy)
                .HasConstraintName("FK_clinical_prescriptions_dispensed_by");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_prescriptions_doctors");

            entity.HasOne(d => d.MedicalRecord).WithOne(p => p.Prescription)
                .HasForeignKey<Prescription>(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_prescriptions_medical_records");
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_clinical_prescription_items");

            entity.ToTable("prescription_items", "clinical");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dosage)
                .HasMaxLength(255)
                .HasColumnName("dosage");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.Frequency)
                .HasMaxLength(50)
                .HasColumnName("frequency");
            entity.Property(e => e.Instruction)
                .HasMaxLength(255)
                .HasColumnName("instruction");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.MedicineNameSnapshot)
                .HasMaxLength(255)
                .HasColumnName("medicine_name_snapshot");
            entity.Property(e => e.PrescriptionId).HasColumnName("prescription_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1, "DF_clinical_prescription_items_quantity")
                .HasColumnName("quantity");
            entity.Property(e => e.UnitPriceSnapshot)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("unit_price_snapshot");

            entity.HasOne(d => d.Medicine).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_prescription_items_medicines");

            entity.HasOne(d => d.Prescription).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_clinical_prescription_items_prescriptions");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_refresh_tokens");

            entity.ToTable("refresh_tokens", "auth");

            entity.HasIndex(e => e.TokenHash, "UQ_auth_refresh_tokens_token_hash").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_auth_refresh_tokens_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.ReplacedByTokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("replaced_by_token_hash");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_auth_refresh_tokens_users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_roles");

            entity.ToTable("roles", "auth");

            entity.HasIndex(e => e.Name, "UQ_auth_roles_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsSystem).HasColumnName("is_system");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_billing_services");

            entity.ToTable("services", "billing");

            entity.HasIndex(e => e.Code, "UX_billing_services_code")
                .IsUnique()
                .HasFilter("([code] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_billing_services_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_billing_services_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Specialty).WithMany(p => p.Services)
                .HasForeignKey(d => d.SpecialtyId)
                .HasConstraintName("FK_billing_services_specialties");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_scheduling_specialties");

            entity.ToTable("specialties", "scheduling");

            entity.HasIndex(e => e.Code, "UX_scheduling_specialties_code")
                .IsUnique()
                .HasFilter("([code] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_scheduling_specialties_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_scheduling_specialties_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_users");

            entity.ToTable("users", "auth");

            entity.HasIndex(e => e.Username, "UQ_auth_users_username").IsUnique();

            entity.HasIndex(e => e.Email, "UX_auth_users_email")
                .IsUnique()
                .HasFilter("([email] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_auth_users_created_at")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.FailedLoginCount).HasColumnName("failed_login_count");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true, "DF_auth_users_is_active")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_auth_user_roles");

            entity.ToTable("user_roles", "auth");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "UQ_user_roles").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_auth_user_roles_assigned_at")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_auth_user_roles_assigned_by");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_auth_user_roles_roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_auth_user_roles_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
