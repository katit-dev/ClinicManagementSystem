using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClinicManagementSystem.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository UserRepository { get; }
    IAppointmentRepository AppointmentRepository { get; }
    IAppointmentStatusHistoryRepository AppointmentStatusHistoryRepository { get; }
    IAttachmentRepository AttachmentRepository { get; }
    IAuditLogRepository AuditLogRepository { get; }
    IDoctorRepository DoctorRepository { get; }
    IDoctorScheduleRepository DoctorScheduleRepository { get; }
    IDoctorTimeOffRepository DoctorTimeOffRepository { get; }
    IInsurancePolicyRepository InsurancePolicyRepository { get; }
    IInvoiceRepository InvoiceRepository { get; }
    IInvoiceItemRepository InvoiceItemRepository { get; }
    ILabResultRepository LabResultRepository { get; }
    IMedicalRecordRepository MedicalRecordRepository { get; }
    IMedicalRecordServiceRepository MedicalRecordServiceRepository { get; }
    IMedicineRepository MedicineRepository { get; }
    IMedicineBatchRepository MedicineBatchRepository { get; }
    IMedicineStockTransactionRepository MedicineStockTransactionRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IPatientRepository PatientRepository { get; }
    IPatientAllergyRepository PatientAllergyRepository { get; }
    IPatientVitalRepository PatientVitalRepository { get; }
    IPaymentRepository PaymentRepository { get; }
    IPrescriptionRepository PrescriptionRepository { get; }
    IPrescriptionItemRepository PrescriptionItemRepository { get; }
    IRefreshTokenRepository RefreshTokenRepository { get; }
    IRoleRepository RoleRepository { get; }
    IServiceRepository ServiceRepository { get; }
    ISpecialtyRepository SpecialtyRepository { get; }
    IUserRoleRepository UserRoleRepository { get; }

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();

    Task SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ClinicManagementDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository UserRepository { get; private set; }
    public IAppointmentRepository AppointmentRepository { get; private set; }
    public IAppointmentStatusHistoryRepository AppointmentStatusHistoryRepository { get; private set; }
    public IAttachmentRepository AttachmentRepository { get; private set; }
    public IAuditLogRepository AuditLogRepository { get; private set; }
    public IDoctorRepository DoctorRepository { get; private set; }
    public IDoctorScheduleRepository DoctorScheduleRepository { get; private set; }
    public IDoctorTimeOffRepository DoctorTimeOffRepository { get; private set; }
    public IInsurancePolicyRepository InsurancePolicyRepository { get; private set; }
    public IInvoiceRepository InvoiceRepository { get; private set; }
    public IInvoiceItemRepository InvoiceItemRepository { get; private set; }
    public ILabResultRepository LabResultRepository { get; private set; }
    public IMedicalRecordRepository MedicalRecordRepository { get; private set; }
    public IMedicalRecordServiceRepository MedicalRecordServiceRepository { get; private set; }
    public IMedicineRepository MedicineRepository { get; private set; }
    public IMedicineBatchRepository MedicineBatchRepository { get; private set; }
    public IMedicineStockTransactionRepository MedicineStockTransactionRepository { get; private set; }
    public INotificationRepository NotificationRepository { get; private set; }
    public IPatientRepository PatientRepository { get; private set; }
    public IPatientAllergyRepository PatientAllergyRepository { get; private set; }
    public IPatientVitalRepository PatientVitalRepository { get; private set; }
    public IPaymentRepository PaymentRepository { get; private set; }
    public IPrescriptionRepository PrescriptionRepository { get; private set; }
    public IPrescriptionItemRepository PrescriptionItemRepository { get; private set; }
    public IRefreshTokenRepository RefreshTokenRepository { get; private set; }
    public IRoleRepository RoleRepository { get; private set; }
    public IServiceRepository ServiceRepository { get; private set; }
    public ISpecialtyRepository SpecialtyRepository { get; private set; }
    public IUserRoleRepository UserRoleRepository { get; private set; }

    public UnitOfWork(ClinicManagementDbContext context)
    {
        _context = context;

        UserRepository = new UserRepository(_context);
        AppointmentRepository = new AppointmentRepository(_context);
        AppointmentStatusHistoryRepository = new AppointmentStatusHistoryRepository(_context);
        AttachmentRepository = new AttachmentRepository(_context);
        AuditLogRepository = new AuditLogRepository(_context);
        DoctorRepository = new DoctorRepository(_context);
        DoctorScheduleRepository = new DoctorScheduleRepository(_context);
        DoctorTimeOffRepository = new DoctorTimeOffRepository(_context);
        InsurancePolicyRepository = new InsurancePolicyRepository(_context);
        InvoiceRepository = new InvoiceRepository(_context);
        InvoiceItemRepository = new InvoiceItemRepository(_context);
        LabResultRepository = new LabResultRepository(_context);
        MedicalRecordRepository = new MedicalRecordRepository(_context);
        MedicalRecordServiceRepository = new MedicalRecordServiceRepository(_context);
        MedicineRepository = new MedicineRepository(_context);
        MedicineBatchRepository = new MedicineBatchRepository(_context);
        MedicineStockTransactionRepository = new MedicineStockTransactionRepository(_context);
        NotificationRepository = new NotificationRepository(_context);
        PatientRepository = new PatientRepository(_context);
        PatientAllergyRepository = new PatientAllergyRepository(_context);
        PatientVitalRepository = new PatientVitalRepository(_context);
        PaymentRepository = new PaymentRepository(_context);
        PrescriptionRepository = new PrescriptionRepository(_context);
        PrescriptionItemRepository = new PrescriptionItemRepository(_context);
        RefreshTokenRepository = new RefreshTokenRepository(_context);
        RoleRepository = new RoleRepository(_context);
        ServiceRepository = new ServiceRepository(_context);
        SpecialtyRepository = new SpecialtyRepository(_context);
        UserRoleRepository = new UserRoleRepository(_context);
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException(
                "No transaction is in progress.");
        }

        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException(
                "No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}