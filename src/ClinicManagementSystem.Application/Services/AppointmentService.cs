using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Application.DTOs.Queue;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// APPOINTMENT SERVICE CONTRACT
// =====================================================

public interface IAppointmentService
{
    Task<HttpResponseData<AppointmentDTO>> CreateAppointmentAsync(CreateAppointmentRequestDTO request, int currentUserId);
    Task<HttpResponseData<List<MyAppointmentDTO>>> GetMyAppointmentsAsync(int currentUserId, MyAppointmentFilter filter);
    Task<HttpResponseData<AppointmentDTO>> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequestDTO request, int currentUserId);
    Task<HttpResponseData<AppointmentDTO>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequestDTO request, int currentUserId);

    // =====================================================
    // RECEPTION APPOINTMENTS
    // =====================================================
    Task<HttpResponseData<List<ReceptionAppointmentDTO>>> GetReceptionAppointmentsAsync(DateOnly date, int? doctorId, int? specialtyId, AppointmentStatus? status);

    Task<HttpResponseData<ReceptionAppointmentDTO>> CheckInAppointmentAsync(int appointmentId, int currentUserId);
    Task<HttpResponseData<ReceptionAppointmentDTO>> MarkNoShowAsync(int appointmentId, NoShowAppointmentRequestDTO request, int currentUserId);

    Task<HttpResponseData<AppointmentDTO>> CreateReceptionAppointmentAsync(CreateReceptionAppointmentRequestDTO request, int currentUserId);

    // =====================================================
    // QUEUE
    // =====================================================

    Task<HttpResponseData<QueueDTO>> GetQueueAsync(int doctorId, DateOnly date);

    Task<HttpResponseData<List<DoctorQueueItemDTO>>> GetMyDoctorQueueAsync(int currentUserId, DateOnly? date);
    Task<HttpResponseData<QueueItemDTO>> RecallQueueAsync(int appointmentId);

    Task<HttpResponseData<QueueItemDTO>> DeferQueueAsync(int appointmentId, int currentUserId);

    // =====================================================
    // START EXAM
    // =====================================================

    Task<HttpResponseData<MedicalRecordDTO>> StartExamAsync(int appointmentId, int currentUserId);



}


public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IDoctorService _doctorService;

    private readonly ILogger<AppointmentService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AppointmentService(IUnitOfWork unitOfWork, IDoctorService doctorService, ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;

        _doctorService = doctorService;

        _logger = logger;
    }

    // =====================================================
    // GET MY DOCTOR QUEUE
    //
    // VC-12
    //
    // Doctor đăng nhập
    //      ↓
    // currentUserId
    //      ↓
    // Doctor.UserId
    //      ↓
    // Doctor.Id
    //      ↓
    // Queue
    //
    // Response:
    // List<DoctorQueueItemDTO>
    // =====================================================

    public async Task<
        HttpResponseData<List<DoctorQueueItemDTO>>>
        GetMyDoctorQueueAsync(
            int currentUserId,
            DateOnly? date)
    {
        try
        {
            // =================================================
            // SELECTED DATE
            // =================================================

            var selectedDate =
                date
                ?? DateOnly.FromDateTime(
                    DateTime.Now
                );


            // =================================================
            // FIND CURRENT DOCTOR
            //
            // UserId trong JWT
            //      ↓
            // Doctor.UserId
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.UserId == currentUserId &&
                            d.IsActive
                    )
                    .Select(
                        d =>
                            new
                            {
                                d.Id
                            }
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // DOCTOR NOT FOUND
            // =================================================

            if (doctor == null)
            {
                return new HttpResponseData<
                    List<DoctorQueueItemDTO>>
                {
                    StatusCode = 404,

                    Message =
                        QueueResponseMessageDTO
                            .DoctorNotFound,

                    Content = null
                };
            }


            // =================================================
            // DATE RANGE
            // =================================================

            var startOfDay =
                selectedDate.ToDateTime(
                    TimeOnly.MinValue
                );

            var endOfDay =
                startOfDay.AddDays(1);


            // =================================================
            // GET QUEUE
            //
            // Chỉ lấy:
            // - đúng bác sĩ
            // - đúng ngày
            // - CheckedIn
            // - có QueueNumber
            // =================================================

            var rows =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId == doctor.Id &&

                            a.StartTime >= startOfDay &&

                            a.StartTime < endOfDay &&

                            a.Status ==
                                (byte)
                                AppointmentStatus.CheckedIn &&

                            a.QueueNumber.HasValue
                    )
                    .OrderBy(
                        a => a.QueueNumber
                    )
                    .Select(
                        a =>
                            new
                            {
                                AppointmentId =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                QueueNumber =
                                    a.QueueNumber!.Value,

                                PatientId =
                                    a.PatientId,

                                PatientCode =
                                    a.Patient.PatientCode,

                                PatientName =
                                    a.Patient.FullName,

                                DateOfBirth =
                                    a.Patient.DateOfBirth,

                                StartTime =
                                    a.StartTime,

                                Reason =
                                    a.Reason
                            }
                    )
                    .ToListAsync();


            // =================================================
            // GET PATIENT IDS
            // =================================================

            var patientIds =
                rows
                    .Select(
                        x => x.PatientId
                    )
                    .Distinct()
                    .ToList();


            // =================================================
            // ALLERGY LOOKUP
            // =================================================

            var allergyLookup =
                new Dictionary<
                    int,
                    List<string>
                >();


            if (patientIds.Count > 0)
            {
                var allergyRows =
                    await _unitOfWork
                        .PatientAllergyRepository
                        .WhereSql(
                            a =>
                                patientIds.Contains(
                                    a.PatientId
                                )
                        )
                        .Select(
                            a =>
                                new
                                {
                                    a.PatientId,
                                    a.Allergen
                                }
                        )
                        .ToListAsync();


                allergyLookup =
                    allergyRows
                        .GroupBy(
                            x =>
                                x.PatientId
                        )
                        .ToDictionary(
                            g =>
                                g.Key,

                            g =>
                                g.Select(
                                    x =>
                                        x.Allergen
                                )
                                .Where(
                                    x =>
                                        !string.IsNullOrWhiteSpace(
                                            x
                                        )
                                )
                                .Distinct()
                                .ToList()
                        );
            }


            // =================================================
            // MAP DOCTOR QUEUE DTO
            // =================================================

            var result =
                rows
                    .Select(
                        x =>
                        {
                            // =================================
                            // CALCULATE AGE
                            // =================================

                            var age =
                                CalculateAge(
                                    x.DateOfBirth,
                                    selectedDate
                                );


                            // =================================
                            // ALLERGY WARNING
                            // =================================

                            var hasAllergy =
                                allergyLookup.TryGetValue(
                                    x.PatientId,
                                    out var allergens
                                );


                            var allergyWarning =
                                hasAllergy &&
                                allergens != null &&
                                allergens.Count > 0
                                    ? $"Dị ứng: {string.Join(
                                        ", ",
                                        allergens
                                    )}"
                                    : null;


                            // =================================
                            // RETURN DTO
                            // =================================

                            return new DoctorQueueItemDTO
                            {
                                AppointmentId =
                                    x.AppointmentId,

                                AppointmentCode =
                                    x.AppointmentCode,

                                QueueNumber =
                                    x.QueueNumber,

                                PatientId =
                                    x.PatientId,

                                PatientCode =
                                    x.PatientCode,

                                PatientName =
                                    x.PatientName,

                                Age =
                                    age,

                                StartTime =
                                    x.StartTime,

                                Reason =
                                    x.Reason,

                                AllergyWarning =
                                    allergyWarning
                            };
                        }
                    )
                    .ToList();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                List<DoctorQueueItemDTO>>
            {
                StatusCode = 200,

                Message =
                    QueueResponseMessageDTO
                        .GetSuccess,

                Content = result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get doctor queue. " +
                "UserId: {UserId}, Date: {Date}",
                currentUserId,
                date
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<
                List<DoctorQueueItemDTO>>
            {
                StatusCode = 500,

                Message =
                    QueueResponseMessageDTO
                        .GetFailed,

                Content = null
            };
        }
    }


    public async Task<HttpResponseData<QueueDTO>> GetQueueAsync(
    int doctorId,
    DateOnly date)
    {
        try
        {
            // =====================================================
            // CHECK DOCTOR
            // =====================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == doctorId &&
                            d.IsActive
                    )
                    .Select(
                        d =>
                            new
                            {
                                d.Id,
                                d.FullName,
                                SpecialtyName = d.Specialty.Name
                            }
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return new HttpResponseData<QueueDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy bác sĩ.",
                    Content = null
                };
            }


            // =====================================================
            // DATE RANGE
            // =====================================================

            var startOfDay =
                date.ToDateTime(TimeOnly.MinValue);

            var endOfDay =
                startOfDay.AddDays(1);


            // =====================================================
            // GET QUEUE
            //
            // Chỉ lấy bệnh nhân đã CheckIn.
            // =====================================================

            var items =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId == doctorId &&

                            a.StartTime >= startOfDay &&

                            a.StartTime < endOfDay &&

                            a.Status ==
                                (byte)AppointmentStatus.CheckedIn &&

                            a.QueueNumber.HasValue
                    )
                    .OrderBy(
                        a => a.QueueNumber
                    )
                    .Select(
                        a =>
                            new QueueItemDTO
                            {
                                AppointmentId =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                QueueNumber =
                                    a.QueueNumber!.Value,

                                PatientId =
                                    a.PatientId,

                                PatientCode =
                                    a.Patient.PatientCode,

                                PatientName =
                                    a.Patient.FullName,

                                StartTime =
                                    a.StartTime,

                                CheckedInAt =
                                    a.CheckedInAt
                            }
                    )
                    .ToListAsync();


            // =====================================================
            // CURRENT
            //
            // Hiện tại hệ thống chưa có status "Serving".
            //
            // Vì vậy quy ước:
            // QueueNumber nhỏ nhất = đang khám.
            // =====================================================

            var current =
                items.FirstOrDefault();


            // =====================================================
            // NEXT
            // =====================================================

            var next =
                items
                    .Skip(1)
                    .FirstOrDefault();


            // =====================================================
            // WAITING
            //
            // Không bao gồm CURRENT và NEXT.
            // =====================================================

            var waiting =
                items
                    .Skip(2)
                    .ToList();


            // =====================================================
            // RESPONSE
            // =====================================================

            var result =
                new QueueDTO
                {
                    DoctorId =
                        doctor.Id,

                    DoctorName =
                        doctor.FullName,

                    SpecialtyName =
                        doctor.SpecialtyName,

                    Date =
                        date,

                    Current =
                        current,

                    Next =
                        next,

                    WaitingCount =
                        waiting.Count,

                    Waiting =
                        waiting
                };


            return new HttpResponseData<QueueDTO>
            {
                StatusCode = 200,
                Message = "Lấy hàng chờ thành công.",
                Content = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get queue. " +
                "DoctorId: {DoctorId}, Date: {Date}",
                doctorId,
                date
            );

            return new HttpResponseData<QueueDTO>
            {
                StatusCode = 500,
                Message = "Không thể lấy hàng chờ.",
                Content = null
            };
        }
    }

    // =====================================================
    // RECALL QUEUE
    // =====================================================

    public async Task<HttpResponseData<QueueItemDTO>> RecallQueueAsync(
        int appointmentId)
    {
        try
        {
            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId &&

                            a.Status ==
                                (byte)AppointmentStatus.CheckedIn &&

                            a.QueueNumber.HasValue
                    )
                    .Select(
                        a =>
                            new QueueItemDTO
                            {
                                AppointmentId =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                QueueNumber =
                                    a.QueueNumber!.Value,

                                PatientId =
                                    a.PatientId,

                                PatientCode =
                                    a.Patient.PatientCode,

                                PatientName =
                                    a.Patient.FullName,

                                StartTime =
                                    a.StartTime,

                                CheckedInAt =
                                    a.CheckedInAt
                            }
                    )
                    .FirstOrDefaultAsync();


            if (appointment == null)
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 404,
                    Message =
                        "Không tìm thấy bệnh nhân trong hàng chờ.",
                    Content = null
                };
            }


            return new HttpResponseData<QueueItemDTO>
            {
                StatusCode = 200,
                Message =
                    $"Mời bệnh nhân {appointment.PatientName}, " +
                    $"số {appointment.QueueNumber}.",

                Content = appointment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to recall queue. " +
                "AppointmentId: {AppointmentId}",
                appointmentId
            );

            return new HttpResponseData<QueueItemDTO>
            {
                StatusCode = 500,
                Message = "Không thể gọi lại số.",
                Content = null
            };
        }
    }

    // =====================================================
    // DEFER QUEUE
    // =====================================================

    public async Task<HttpResponseData<QueueItemDTO>> DeferQueueAsync(
        int appointmentId,
        int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // GET APPOINTMENT
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId
                    )
                    .FirstOrDefaultAsync();


            if (appointment == null)
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy lịch hẹn.",
                    Content = null
                };
            }


            // =================================================
            // CHECK STATUS
            // =================================================

            if (
                appointment.Status !=
                (byte)AppointmentStatus.CheckedIn
            )
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 400,
                    Message =
                        "Chỉ có thể chuyển bệnh nhân đang trong hàng chờ.",
                    Content = null
                };
            }


            // =================================================
            // CHECK QUEUE NUMBER
            // =================================================

            if (!appointment.QueueNumber.HasValue)
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 400,
                    Message = "Bệnh nhân chưa được cấp số thứ tự.",
                    Content = null
                };
            }

            // kiem tra so nho nhat
            var currentQueueNumber =
    await _unitOfWork
        .AppointmentRepository
        .WhereSql(
            a =>
                a.DoctorId == appointment.DoctorId &&
                a.StartTime >= appointment.StartTime.Date &&
                a.StartTime < appointment.StartTime.Date.AddDays(1) &&
                a.Status ==
                    (byte)AppointmentStatus.CheckedIn &&
                a.QueueNumber.HasValue
        )
        .MinAsync(a => a.QueueNumber);

            if (currentQueueNumber.HasValue &&
        appointment.QueueNumber.Value ==
        currentQueueNumber.Value)
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 400,
                    Message =
                        "Không thể chuyển bệnh nhân đang được khám xuống cuối hàng.",
                    Content = null
                };
            }


            // =================================================
            // CHECK TODAY
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );

            var appointmentDate =
                DateOnly.FromDateTime(
                    appointment.StartTime
                );


            if (appointmentDate != today)
            {
                return new HttpResponseData<QueueItemDTO>
                {
                    StatusCode = 400,
                    Message =
                        "Chỉ có thể chuyển hàng chờ trong ngày hôm nay.",
                    Content = null
                };
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // GET LAST QUEUE NUMBER
            // =================================================

            var startOfDay =
                today.ToDateTime(
                    TimeOnly.MinValue
                );

            var endOfDay =
                startOfDay.AddDays(1);


            var lastQueueNumber =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId ==
                                appointment.DoctorId &&

                            a.StartTime >=
                                startOfDay &&

                            a.StartTime <
                                endOfDay &&

                            a.QueueNumber.HasValue
                    )
                    .MaxAsync(
                        a => a.QueueNumber
                    )
                    ?? 0;


            // =================================================
            // MOVE TO END
            // =================================================

            appointment.QueueNumber =
                lastQueueNumber + 1;

            appointment.UpdatedAt =
                DateTime.Now;


            // =================================================
            // SAVE
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // GET UPDATED RESULT
            // =================================================

            var result =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointment.Id
                    )
                    .Select(
                        a =>
                            new QueueItemDTO
                            {
                                AppointmentId =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                QueueNumber =
                                    a.QueueNumber!.Value,

                                PatientId =
                                    a.PatientId,

                                PatientCode =
                                    a.Patient.PatientCode,

                                PatientName =
                                    a.Patient.FullName,

                                StartTime =
                                    a.StartTime,

                                CheckedInAt =
                                    a.CheckedInAt
                            }
                    )
                    .FirstOrDefaultAsync();


            return new HttpResponseData<QueueItemDTO>
            {
                StatusCode = 200,
                Message =
                    $"Đã chuyển bệnh nhân xuống cuối hàng. " +
                    $"Số mới: {appointment.QueueNumber}.",

                Content = result
            };
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback queue defer."
                    );
                }
            }


            _logger.LogError(
                ex,
                "Failed to defer queue. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return new HttpResponseData<QueueItemDTO>
            {
                StatusCode = 500,
                Message = "Không thể chuyển bệnh nhân xuống cuối hàng.",
                Content = null
            };
        }
    }


    // =====================================================
    // CREATE RECEPTION APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<AppointmentDTO>>
        CreateReceptionAppointmentAsync(
            CreateReceptionAppointmentRequestDTO request,
            int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.Id == request.PatientId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }



            // =================================================
            // CHECK DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == request.DoctorId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .DoctorNotFound
                );
            }

            // =====================================================
            // GET SPECIALTY
            // =====================================================

            var specialty =
                await _unitOfWork
                    .SpecialtyRepository
                    .WhereSql(
                        s =>
                            s.Id == doctor.SpecialtyId
                    )
                    .FirstOrDefaultAsync();

            // =================================================
            // CHECK APPOINTMENT TIME
            // =================================================

            if (request.StartTime <= DateTime.Now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }



            // =================================================
            // CHECK SLOT
            // =================================================

            var appointmentDate =
                DateOnly.FromDateTime(
                    request.StartTime
                );


            var availableSlots =
                await _doctorService
                    .GetAvailableSlotsAsync(
                        request.DoctorId,
                        appointmentDate
                    );


            var selectedSlot =
                availableSlots.Content?
                    .FirstOrDefault(
                        s =>
                            s.StartTime ==
                            request.StartTime
                    );


            if (selectedSlot == null)
            {
                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }



            var now =
                DateTime.UtcNow;



            // =================================================
            // CREATE APPOINTMENT
            // =================================================

            var appointment =
                new Appointment
                {
                    PatientId =
                        patient.Id,

                    DoctorId =
                        request.DoctorId,

                    StartTime =
                        selectedSlot.StartTime,

                    EndTime =
                        selectedSlot.EndTime,


                    Status =
                        (byte)AppointmentStatus.Pending,


                    Reason =
                        request.Reason,


                    AppointmentCode =
                        "APT" +
                        Guid.NewGuid()
                            .ToString("N")[..17]
                            .ToUpperInvariant(),


                    CreatedBy =
                        currentUserId,


                    CreatedAt =
                        now,


                    Source =
                        request.Source,


                    FeeSnapshot =
                        doctor.ConsultationFee
                };



            // =================================================
            // STATUS HISTORY
            // =================================================

            var history =
                new AppointmentStatusHistory
                {
                    Appointment =
                        appointment,

                    FromStatus =
                        null,

                    ToStatus =
                        (byte)AppointmentStatus.Pending,

                    ChangedBy =
                        currentUserId,

                    ChangedAt =
                        now
                };



            // =================================================
            // TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;



            // =================================================
            // CHECK CONFLICT AGAIN
            // =================================================

            var exists =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId ==
                                request.DoctorId &&

                            a.Status <
                                (byte)AppointmentStatus.Cancelled &&

                            a.StartTime <
                                appointment.EndTime &&

                            a.EndTime >
                                appointment.StartTime
                    )
                    .AnyAsync();


            if (exists)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;


                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }



            // =================================================
            // SAVE
            // =================================================

            await _unitOfWork
                .AppointmentRepository
                .AddAsync(
                    appointment
                );


            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(
                    history
                );


            await _unitOfWork
                .SaveChangesAsync();



            await _unitOfWork
                .CommitTransactionAsync();


            transactionStarted = false;



            // =====================================================
            // RESPONSE
            // =====================================================

            var result =
                new AppointmentDTO
                {
                    Id =
                        appointment.Id,

                    AppointmentCode =
                        appointment.AppointmentCode,

                    DoctorName =
                        doctor.FullName,

                    SpecialtyName =
                        specialty?.Name
                        ?? string.Empty,

                    StartTime =
                        appointment.StartTime,

                    Status =
                        appointment.Status,

                    QueueNumber =
                        appointment.QueueNumber,

                    FeeSnapshot =
                        appointment.FeeSnapshot
                };
            return Response(
                201,
                AppointmentResponseMessageDTO
                    .CreateSuccess,

                result
            );
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();
            }


            _logger.LogError(
                ex,
                "Failed to create reception appointment."
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .CreateFailed
            );
        }
    }

    // =====================================================
    // MARK APPOINTMENT AS NO SHOW
    // =====================================================

    public async Task<HttpResponseData<ReceptionAppointmentDTO>> MarkNoShowAsync(
        int appointmentId,
        NoShowAppointmentRequestDTO request,
        int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // GET APPOINTMENT
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(a => a.Id == appointmentId)
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy lịch hẹn.",
                    Content = null
                };
            }


            // =================================================
            // CHECK STATUS
            //
            // Chỉ cho đánh dấu NoShow khi:
            //
            // Pending
            // Confirmed
            // =================================================

            var canMarkNoShow =
                appointment.Status == (byte)AppointmentStatus.Pending ||
                appointment.Status == (byte)AppointmentStatus.Confirmed;

            if (!canMarkNoShow)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 400,
                    Message = "Trạng thái lịch hẹn không cho phép đánh dấu không đến.",
                    Content = null
                };
            }


            // =================================================
            // CHECK APPOINTMENT DATE
            // =================================================

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var appointmentDate = DateOnly.FromDateTime(appointment.StartTime);

            if (appointmentDate != today)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 400,
                    Message = "Chỉ có thể đánh dấu không đến cho lịch hẹn trong ngày hôm nay.",
                    Content = null
                };
            }


            // =================================================
            // CHECK APPOINTMENT TIME
            //
            // Không được NoShow trước giờ hẹn.
            // =================================================

            if (appointment.StartTime > now)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 400,
                    Message = "Chưa đến giờ hẹn nên chưa thể đánh dấu bệnh nhân không đến.",
                    Content = null
                };
            }


            // =================================================
            // CURRENT STATUS
            // =================================================

            var fromStatus = appointment.Status;


            // =================================================
            // PREPARE NOTE
            // =================================================

            var reason =
                string.IsNullOrWhiteSpace(request.Note)
                    ? null
                    : request.Note.Trim();


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork.BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            appointment.Status = (byte)AppointmentStatus.NoShow;

            appointment.QueueNumber = null;
            appointment.CheckedInAt = null;

            appointment.UpdatedAt = now;


            // =================================================
            // CREATE STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    AppointmentId = appointment.Id,
                    FromStatus = fromStatus,
                    ToStatus = (byte)AppointmentStatus.NoShow,
                    ChangedBy = currentUserId,
                    Reason = reason,
                    ChangedAt = now
                };


            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(statusHistory);


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // GET UPDATED APPOINTMENT
            // =================================================

            var result =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(a => a.Id == appointment.Id)
                    .Select(
                        a =>
                            new ReceptionAppointmentDTO
                            {
                                Id = a.Id,
                                AppointmentCode = a.AppointmentCode,

                                StartTime = a.StartTime,
                                EndTime = a.EndTime,

                                Status = a.Status,
                                QueueNumber = a.QueueNumber,
                                CheckedInAt = a.CheckedInAt,
                                Reason = a.Reason,

                                PatientId = a.PatientId,
                                PatientCode = a.Patient.PatientCode,
                                PatientName = a.Patient.FullName,

                                DoctorId = a.DoctorId,
                                DoctorName = a.Doctor.FullName,

                                SpecialtyId = a.Doctor.SpecialtyId,
                                SpecialtyName = a.Doctor.Specialty.Name
                            }
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<ReceptionAppointmentDTO>
            {
                StatusCode = 200,
                Message = "Đã đánh dấu bệnh nhân không đến.",
                Content = result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment no-show."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to mark appointment as no-show. " +
                "AppointmentId: {AppointmentId}, UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<ReceptionAppointmentDTO>
            {
                StatusCode = 500,
                Message = "Không thể đánh dấu bệnh nhân không đến.",
                Content = null
            };
        }
    }

    // =====================================================
    // CHECK IN APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<ReceptionAppointmentDTO>> CheckInAppointmentAsync(
        int appointmentId,
        int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // GET APPOINTMENT
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(a => a.Id == appointmentId)
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy lịch hẹn.",
                    Content = null
                };
            }


            // =================================================
            // CHECK STATUS
            //
            // Chỉ cho check-in khi:
            // Pending
            // Confirmed
            // =================================================

            var canCheckIn =
                appointment.Status == (byte)AppointmentStatus.Pending ||
                appointment.Status == (byte)AppointmentStatus.Confirmed;

            if (!canCheckIn)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 400,
                    Message = "Trạng thái lịch hẹn không cho phép check-in.",
                    Content = null
                };
            }


            // =================================================
            // CHECK APPOINTMENT DATE
            // =================================================

            var today = DateOnly.FromDateTime(DateTime.Now);
            var appointmentDate = DateOnly.FromDateTime(appointment.StartTime);

            if (appointmentDate != today)
            {
                return new HttpResponseData<ReceptionAppointmentDTO>
                {
                    StatusCode = 400,
                    Message = "Chỉ có thể check-in lịch hẹn trong ngày hôm nay.",
                    Content = null
                };
            }


            // =================================================
            // DATE RANGE
            // =================================================

            var startOfDay = today.ToDateTime(TimeOnly.MinValue);
            var endOfDay = startOfDay.AddDays(1);


            // =================================================
            // BEGIN TRANSACTION
            //
            // Việc lấy queue và update appointment
            // phải nằm trong cùng transaction.
            // =================================================

            await _unitOfWork.BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // GET LAST QUEUE NUMBER
            //
            // Queue được đánh riêng theo bác sĩ + ngày.
            // =================================================

            var lastQueueNumber =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId == appointment.DoctorId &&
                            a.StartTime >= startOfDay &&
                            a.StartTime < endOfDay &&
                            a.QueueNumber.HasValue
                    )
                    .MaxAsync(a => a.QueueNumber)
                ?? 0;


            // =================================================
            // NEXT QUEUE NUMBER
            // =================================================

            var nextQueueNumber = lastQueueNumber + 1;


            // =================================================
            // CURRENT STATUS
            // =================================================

            var fromStatus = appointment.Status;

            var now = DateTime.Now;


            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            appointment.Status = (byte)AppointmentStatus.CheckedIn;
            appointment.QueueNumber = nextQueueNumber;
            appointment.CheckedInAt = now;
            appointment.UpdatedAt = now;


            // =================================================
            // CREATE STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    AppointmentId = appointment.Id,
                    FromStatus = fromStatus,
                    ToStatus = (byte)AppointmentStatus.CheckedIn,
                    ChangedBy = currentUserId,
                    Reason = null,
                    ChangedAt = now
                };


            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(statusHistory);


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // GET UPDATED APPOINTMENT
            // =================================================

            var result =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(a => a.Id == appointment.Id)
                    .Select(
                        a =>
                            new ReceptionAppointmentDTO
                            {
                                Id = a.Id,
                                AppointmentCode = a.AppointmentCode,

                                StartTime = a.StartTime,
                                EndTime = a.EndTime,

                                Status = a.Status,
                                QueueNumber = a.QueueNumber,
                                CheckedInAt = a.CheckedInAt,
                                Reason = a.Reason,

                                PatientId = a.PatientId,
                                PatientCode = a.Patient.PatientCode,
                                PatientName = a.Patient.FullName,

                                DoctorId = a.DoctorId,
                                DoctorName = a.Doctor.FullName,

                                SpecialtyId = a.Doctor.SpecialtyId,
                                SpecialtyName = a.Doctor.Specialty.Name
                            }
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<ReceptionAppointmentDTO>
            {
                StatusCode = 200,
                Message = $"Check-in thành công. Số thứ tự: {nextQueueNumber}.",
                Content = result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment check-in."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to check in appointment. " +
                "AppointmentId: {AppointmentId}, UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<ReceptionAppointmentDTO>
            {
                StatusCode = 500,
                Message = "Không thể check-in lịch hẹn.",
                Content = null
            };
        }
    }

    // =====================================================
    // GET RECEPTION APPOINTMENTS
    // =====================================================

    public async Task<HttpResponseData<List<ReceptionAppointmentDTO>>> GetReceptionAppointmentsAsync(
        DateOnly date,
        int? doctorId,
        int? specialtyId,
        AppointmentStatus? status)
    {
        try
        {
            // =================================================
            // DATE RANGE
            // =================================================

            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            var endOfDay = startOfDay.AddDays(1);


            // =================================================
            // BASE QUERY
            // =================================================

            var query =
                _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.StartTime >= startOfDay &&
                            a.StartTime < endOfDay
                    );


            // =================================================
            // FILTER BY DOCTOR
            // =================================================

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);
            }


            // =================================================
            // FILTER BY SPECIALTY
            // =================================================

            if (specialtyId.HasValue)
            {
                query = query.Where(a => a.Doctor.SpecialtyId == specialtyId.Value);
            }


            // =================================================
            // FILTER BY STATUS
            // =================================================

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == (byte)status.Value);
            }


            // =================================================
            // QUERY + MAP DTO
            // =================================================

            var appointments =
                await query
                    .OrderBy(a => a.StartTime)
                    .Select(
                        a =>
                            new ReceptionAppointmentDTO
                            {
                                Id = a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                StartTime =
                                    a.StartTime,

                                EndTime =
                                    a.EndTime,

                                Status =
                                    a.Status,

                                QueueNumber =
                                    a.QueueNumber,

                                CheckedInAt =
                                    a.CheckedInAt,

                                Reason =
                                    a.Reason,

                                PatientId =
                                    a.PatientId,

                                PatientCode =
                                    a.Patient.PatientCode,

                                PatientName =
                                    a.Patient.FullName,

                                DoctorId =
                                    a.DoctorId,

                                DoctorName =
                                    a.Doctor.FullName,

                                SpecialtyId =
                                    a.Doctor.SpecialtyId,

                                SpecialtyName =
                                    a.Doctor.Specialty.Name
                            }
                    )
                    .ToListAsync();


            // =================================================
            // GET INVOICES
            //
            // Invoice được liên kết với Appointment bằng:
            // Invoice.AppointmentId
            // =================================================

            if (appointments.Count > 0)
            {
                var appointmentIds =
                    appointments
                        .Select(a => a.Id)
                        .ToList();


                var invoices =
                    await _unitOfWork
                        .InvoiceRepository
                        .WhereSql(
                            i =>
                                i.AppointmentId.HasValue &&
                                appointmentIds.Contains(
                                    i.AppointmentId.Value
                                )
                        )
                        .Select(
                            i =>
                                new
                                {
                                    i.Id,

                                    AppointmentId =
                                        i.AppointmentId!.Value,

                                    i.Status,

                                    i.TotalAmount,

                                    i.TaxAmount,

                                    i.DiscountAmount,

                                    i.InsuranceAmount,

                                    i.PaidAmount
                                }
                        )
                        .ToListAsync();


                // =================================================
                // MAP INVOICE TO APPOINTMENT
                // =================================================

                foreach (var appointment in appointments)
                {
                    var invoice =
                        invoices
                            .FirstOrDefault(
                                i =>
                                    i.AppointmentId ==
                                    appointment.Id
                            );


                    if (invoice == null)
                    {
                        continue;
                    }


                    // =================================================
                    // INVOICE
                    // =================================================

                    appointment.InvoiceId =
                        invoice.Id;

                    appointment.InvoiceStatus =
                        invoice.Status;


                    // =================================================
                    // CALCULATE REMAINING
                    //
                    // Giống cách InvoiceService đang tính:
                    //
                    // Total
                    // + Tax
                    // - Discount
                    // - Insurance
                    // - Paid
                    // =================================================

                    var payableAmount =
                        invoice.TotalAmount
                        + invoice.TaxAmount
                        - invoice.DiscountAmount
                        - invoice.InsuranceAmount;


                    var remainingAmount =
                        payableAmount
                        - invoice.PaidAmount;


                    // =================================================
                    // PROTECT AGAINST NEGATIVE VALUE
                    // =================================================

                    if (remainingAmount < 0)
                    {
                        remainingAmount = 0;
                    }


                    appointment.InvoiceRemainingAmount =
                        remainingAmount;
                }
            }

            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<List<ReceptionAppointmentDTO>>
            {
                StatusCode = 200,
                Message = "Lấy danh sách lịch hẹn thành công.",
                Content = appointments
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get reception appointments. " +
                "Date: {Date}, DoctorId: {DoctorId}, " +
                "SpecialtyId: {SpecialtyId}, Status: {Status}",
                date,
                doctorId,
                specialtyId,
                status
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<List<ReceptionAppointmentDTO>>
            {
                StatusCode = 500,
                Message = "Không thể lấy danh sách lịch hẹn.",
                Content = new List<ReceptionAppointmentDTO>()
            };
        }
    }


    // =====================================================
    // RESCHEDULE APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<AppointmentDTO>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // GET APPOINTMENT
            //
            // Chỉ lấy Appointment thuộc Patient hiện tại.
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId &&
                            a.PatientId == patient.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .AppointmentNotFound
                );
            }


            // =================================================
            // CHECK RESCHEDULABLE STATUS
            //
            // Patient chỉ được đổi lịch khi:
            //
            // Pending
            // Confirmed
            //
            // Không cho đổi:
            //
            // CheckedIn
            // Completed
            // Cancelled
            // NoShow
            // =================================================

            var canReschedule =
                appointment.Status ==
                    (byte)AppointmentStatus.Pending
                ||
                appointment.Status ==
                    (byte)AppointmentStatus.Confirmed;


            if (!canReschedule)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }


            // =================================================
            // CHECK CURRENT APPOINTMENT TIME
            //
            // Appointment hiện tại đã tới hoặc qua giờ khám
            // thì không cho Patient tự đổi nữa.
            // =================================================

            var now =
                DateTime.Now;

            if (appointment.StartTime <= now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }


            // =================================================
            // CHECK NEW START TIME
            //
            // Giờ mới phải nằm trong tương lai.
            // =================================================

            if (request.NewStartTime <= now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // CHECK SAME TIME
            //
            // Không cần reschedule nếu giờ mới
            // giống hệt giờ hiện tại.
            // =================================================

            if (
                request.NewStartTime ==
                appointment.StartTime
            )
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }

            // =================================================
            // GET NEW APPOINTMENT DATE
            // =================================================

            var appointmentDate =
                DateOnly.FromDateTime(
                    request.NewStartTime
                );

            // =================================================
            // RE-CHECK AVAILABLE SLOTS
            // =================================================

            var availableSlotsResponse =
                await _doctorService
                    .GetAvailableSlotsAsync(
                        appointment.DoctorId,
                        appointmentDate
                    );

            // =================================================
            // FIND SELECTED SLOT
            // =================================================

            var selectedSlot =
                availableSlotsResponse.Content?
                    .FirstOrDefault(
                        slot =>
                            slot.StartTime ==
                            request.NewStartTime
                    );
            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (selectedSlot == null)
            {
                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // UPDATE APPOINTMENT TIME
            // =================================================

            appointment.StartTime =
                selectedSlot.StartTime;

            appointment.EndTime =
                selectedSlot.EndTime;

            appointment.UpdatedAt =
                DateTime.Now;


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // RE-CHECK APPOINTMENT CONFLICT
            // =================================================

            var appointmentExists =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id != appointment.Id &&

                            a.DoctorId ==
                                appointment.DoctorId &&

                            a.Status <
                                (byte)AppointmentStatus.Cancelled &&

                            a.StartTime <
                                appointment.EndTime &&

                            a.EndTime >
                                appointment.StartTime
                    )
                    .AnyAsync();


            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (appointmentExists)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;

                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                200,
                AppointmentResponseMessageDTO
                    .RescheduleSuccess
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment reschedule."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to reschedule appointment. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .RescheduleFailed
            );
        }
    }

    // =====================================================
    // CANCEL APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<AppointmentDTO>> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // GET APPOINTMENT
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId &&
                            a.PatientId == patient.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .AppointmentNotFound
                );
            }


            // =================================================
            // CHECK CANCELLABLE STATUS
            //
            // Patient chỉ được tự hủy khi:
            //
            // Pending
            // Confirmed
            //
            // Không cho hủy:
            //
            // CheckedIn
            // Completed
            // Cancelled
            // NoShow
            // =================================================

            var canCancel =
                appointment.Status ==
                    (byte)AppointmentStatus.Pending
                ||
                appointment.Status ==
                    (byte)AppointmentStatus.Confirmed;


            if (!canCancel)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotCancelAppointment
                );
            }


            // =================================================
            // CHECK APPOINTMENT TIME
            //
            // Lịch đã tới hoặc đã qua
            // thì Patient không được tự hủy nữa.
            // =================================================

            if (
                appointment.StartTime <=
                DateTime.Now
            )
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotCancelAppointment
                );
            }


            // =================================================
            // CURRENT STATUS
            //
            // Phải lưu lại trước khi đổi sang Cancelled
            // để ghi AppointmentStatusHistory.
            // =================================================

            var fromStatus =
                appointment.Status;

            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            var now =
                DateTime.Now;

            appointment.Status =
                (byte)AppointmentStatus.Cancelled;

            appointment.CancelReason =
                request.CancelReason.Trim();

            appointment.UpdatedAt =
                now;

            // =================================================
            // CREATE STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    AppointmentId =
                        appointment.Id,

                    FromStatus =
                        fromStatus,

                    ToStatus =
                        (byte)AppointmentStatus.Cancelled,

                    ChangedBy =
                        currentUserId,

                    Reason =
                        request.CancelReason.Trim(),

                    ChangedAt =
                        now
                };

            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork.BeginTransactionAsync();

            transactionStarted = true;

            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork.AppointmentStatusHistoryRepository.AddAsync(statusHistory);

            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();

            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;

            // =================================================
            // SUCCESS
            // =================================================

            return Response(200, AppointmentResponseMessageDTO.CancelSuccess);

        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment cancellation."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to cancel appointment. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .CancelFailed
            );
        }
    }

    // =====================================================
    // GET MY APPOINTMENTS
    // =====================================================

    public async Task<HttpResponseData<List<MyAppointmentDTO>>> GetMyAppointmentsAsync(int currentUserId, MyAppointmentFilter filter)
    {
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                return MyAppointmentsResponse(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // CURRENT TIME
            // =================================================

            var now =
                DateTime.Now;


            // =================================================
            // BASE QUERY
            //
            // Chỉ lấy Appointment của Patient đang đăng nhập.
            // =================================================

            var query =
                _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.PatientId == patient.Id
                    );


            // =================================================
            // FILTER
            // =================================================

            switch (filter)
            {
                // =============================================
                // UPCOMING
                //
                // Pending   = 0
                // Confirmed = 1
                // CheckedIn = 2
                //
                // Và lịch chưa qua.
                // =============================================

                case MyAppointmentFilter.Upcoming:

                    query =
                        query.Where(
                            a =>
                                a.StartTime >= now &&
                                a.Status <
                                    (byte)
                                    AppointmentStatus.Completed
                        );

                    break;


                // =============================================
                // COMPLETED
                // =============================================

                case MyAppointmentFilter.Completed:

                    query =
                        query.Where(
                            a =>
                                a.Status ==
                                    (byte)
                                    AppointmentStatus.Completed
                        );

                    break;


                // =============================================
                // CANCELLED
                // =============================================

                case MyAppointmentFilter.Cancelled:

                    query =
                        query.Where(
                            a =>
                                a.Status ==
                                    (byte)
                                    AppointmentStatus.Cancelled
                        );

                    break;


                // =============================================
                // ALL
                //
                // Không thêm điều kiện.
                // =============================================

                case MyAppointmentFilter.All:

                default:
                    break;
            }


            // =================================================
            // QUERY + MAP DTO
            // =================================================

            var appointments =
                await query
                    .OrderByDescending(
                        a => a.StartTime
                    )
                    .Select(
                        a =>
                            new MyAppointmentDTO
                            {
                                Id =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                DoctorId = a.DoctorId,

                                DoctorName =
                                    a.Doctor.FullName,

                                SpecialtyName =
                                    a.Doctor
                                        .Specialty
                                        .Name,

                                StartTime =
                                    a.StartTime,

                                EndTime =
                                    a.EndTime,

                                Status =
                                    a.Status,

                                QueueNumber =
                                    a.QueueNumber
                            }
                    )
                    .ToListAsync();


            // =================================================
            // UPCOMING
            //
            // Với lịch sắp tới:
            // lịch gần nhất nên nằm trên đầu.
            // =================================================

            if (
                filter ==
                MyAppointmentFilter.Upcoming
            )
            {
                appointments =
                    appointments
                        .OrderBy(
                            a => a.StartTime
                        )
                        .ToList();
            }


            // =================================================
            // SUCCESS
            // =================================================

            return MyAppointmentsResponse(
                200,
                AppointmentResponseMessageDTO
                    .GetMyAppointmentsSuccess,
                appointments
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get appointments for UserId: {UserId}",
                currentUserId
            );


            return MyAppointmentsResponse(
                500,
                AppointmentResponseMessageDTO
                    .GetMyAppointmentsFailed
            );
        }
    }


    // =====================================================
    // CREATE APPOINTMENT
    // =====================================================

    public async Task<
        HttpResponseData<AppointmentDTO>>
        CreateAppointmentAsync(
            CreateAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;
        try
        {
            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
         await _unitOfWork
        .PatientRepository
        .WhereSql(p => p.UserId == currentUserId && p.IsActive)
        .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // CHECK DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == request.DoctorId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();

            if (doctor == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .DoctorNotFound
                );
            }

            // =================================================
            // GET SPECIALTY (lay ten chuyen khoa de bo vao AppointmentDTO, vi endpoint response la AppointmentDTO)
            // =================================================

            var specialty =
                await _unitOfWork
                    .SpecialtyRepository
                    .WhereSql(
                        s =>
                            s.Id == doctor.SpecialtyId
                    )
                    .FirstOrDefaultAsync();

            // =================================================
            // CHECK APPOINTMENT TIME
            // =================================================

            if (request.StartTime <= DateTime.Now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // RE-CHECK AVAILABLE SLOT
            // =================================================

            var appointmentDate =
                DateOnly.FromDateTime(
                    request.StartTime
                );

            var availableSlotsResponse =
                await _doctorService
                    .GetAvailableSlotsAsync(
                        request.DoctorId,
                        appointmentDate
                    );


            var selectedSlot =
                availableSlotsResponse.Content?
                    .FirstOrDefault(
                        slot =>
                            slot.StartTime ==
                            request.StartTime
                    );


            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (selectedSlot == null)
            {
                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // PREPARE APPOINTMENT
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // GENERATE APPOINTMENT CODE
            // =================================================

            var appointmentCode =
                "APT" +
                Guid.NewGuid()
                    .ToString("N")[..17]
                    .ToUpperInvariant();


            // =================================================
            // CREATE APPOINTMENT ENTITY
            // =================================================

            var appointment =
                new Appointment
                {
                    PatientId =
                        patient.Id,

                    DoctorId =
                        request.DoctorId,

                    StartTime =
                        selectedSlot.StartTime,

                    EndTime =
                        selectedSlot.EndTime,

                    Status =
                        (byte)AppointmentStatus.Pending,

                    Reason =
                        request.Reason,

                    CreatedAt =
                        now,

                    AppointmentCode =
                        appointmentCode,

                    Source =
                        (byte)AppointmentSource.Online,

                    FeeSnapshot =
                        doctor.ConsultationFee
                };

            // =================================================
            // CREATE APPOINTMENT STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    Appointment =
                        appointment,

                    FromStatus =
                        null,

                    ToStatus =
                        (byte)AppointmentStatus.Pending,

                    ChangedAt =
                        now
                };

            // =================================================
            // REMINDER TIME
            //
            // Nhắc trước lịch khám 24 giờ.
            // =================================================

            var reminderAt =
                appointment.StartTime.AddHours(-24);


            // =================================================
            // CREATE NOTIFICATION
            // =================================================

            var notification =
                new Notification
                {
                    Appointment =
                        appointment,

                    UserId =
                        patient.UserId,

                    Channel =
                        "InApp",

                    Title =
                        "Nhắc lịch khám",

                    Content =
                        $"Bạn có lịch khám vào " +
                        $"{appointment.StartTime:HH:mm dd/MM/yyyy}.",

                    Status =
                        0,

                    ScheduledAt =
                        reminderAt,

                    CreatedAt =
                        now
                };

            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;

            // =================================================
            // RE-CHECK APPOINTMENT CONFLICT
            // =================================================

            var appointmentExists =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId ==
                                request.DoctorId &&

                            a.Status <
                                (byte)AppointmentStatus.Cancelled &&

                            a.StartTime <
                                appointment.EndTime &&

                            a.EndTime >
                                appointment.StartTime
                    )
                    .AnyAsync();

            // check appoitnmentExists
            if (appointmentExists)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;

                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // ADD APPOINTMENT
            // =================================================

            await _unitOfWork
                .AppointmentRepository
                .AddAsync(
                    appointment
                );


            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(
                    statusHistory
                );


            // =================================================
            // ADD NOTIFICATION
            // =================================================

            await _unitOfWork
                .NotificationRepository
                .AddAsync(
                    notification
                );

            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;



            // =================================================
            // MAP RESPONSE DTO
            // =================================================

            var result =
                new AppointmentDTO
                {
                    Id =
                        appointment.Id,

                    AppointmentCode =
                        appointment.AppointmentCode,

                    DoctorName =
                        doctor.FullName,

                    SpecialtyName =
                        specialty?.Name
                        ?? string.Empty,

                    StartTime =
                        appointment.StartTime,

                    Status =
                        appointment.Status,

                    QueueNumber =
                        appointment.QueueNumber,

                    FeeSnapshot =
                        appointment.FeeSnapshot
                };


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                201,
                AppointmentResponseMessageDTO
                    .CreateSuccess,
                result
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment creation."
                    );
                }
            }

            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to create appointment."
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .CreateFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static HttpResponseData<AppointmentDTO> Response(int statusCode, string message, AppointmentDTO? content = null)
    {
        return new HttpResponseData<AppointmentDTO>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content
        };
    }

    // =====================================================
    // MY APPOINTMENTS RESPONSE
    // =====================================================

    private static HttpResponseData<List<MyAppointmentDTO>> MyAppointmentsResponse(int statusCode, string message, List<MyAppointmentDTO>? content = null)
    {
        return new HttpResponseData<List<MyAppointmentDTO>>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content ??
                new List<MyAppointmentDTO>()
        };
    }

    // =====================================================
    // CALCULATE AGE
    // =====================================================

    private static int CalculateAge(
        DateOnly? dateOfBirth,
        DateOnly referenceDate)
    {
        if (!dateOfBirth.HasValue)
        {
            return 0;
        }


        var birthDate =
            dateOfBirth.Value;


        var age =
            referenceDate.Year -
            birthDate.Year;


        if (
            referenceDate <
            birthDate.AddYears(age)
        )
        {
            age--;
        }


        return age < 0
            ? 0
            : age;
    }

}