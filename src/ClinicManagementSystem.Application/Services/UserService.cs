using System.Text;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IUserService
{
    Task<HttpResponseData<object?>> RegisterUserAsync(
        UserRegisterDTO request);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HttpResponseData<object?>> RegisterUserAsync(
        UserRegisterDTO request)
    {
        // Chuẩn hóa dữ liệu
        string fullName = request.FullName.Trim();
        string phone = request.Phone.Trim();

        string? email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        // Ngày sinh không được ở tương lai
        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
        {
            return Response(
                400,
                UserResponseMessageDTO.InvalidDateOfBirth);
        }

        // Giới hạn đầu vào cho BCrypt
        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            return Response(
                400,
                UserResponseMessageDTO.PasswordTooLong);
        }

        bool transactionStarted = false;

        try
        {
            // 1. Kiểm tra đã có account chưa
            bool userExists = await _unitOfWork.UserRepository
                .WhereSql(u =>
                    u.Username == phone ||
                    u.Phone == phone ||
                    (email != null && u.Email == email))
                .AnyAsync();

            if (userExists)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // 2. Tìm hồ sơ Patient theo số điện thoại
            var existingPatient = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Phone == phone)
                .FirstOrDefaultAsync();

            // 3. Nếu Patient đã liên kết account
            if (existingPatient != null &&
                existingPatient.UserId.HasValue)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // 4. Lấy role Patient
            var patientRole = await _unitOfWork.RoleRepository
                .WhereSql(r => r.Name == UserRoleConstant.Patient)
                .FirstOrDefaultAsync();

            if (patientRole == null)
            {
                _logger.LogError("Patient role was not found.");

                return Response(
                    500,
                    UserResponseMessageDTO.PatientRoleNotFound);
            }

            var now = DateTime.UtcNow;

            // 5. Tạo account
            // Quy ước hiện tại: Username = Phone
            var user = new User
            {
                Username = phone,
                PasswordHash =
                    HelperFunction.HashPassword(request.Password),
                FullName = fullName,
                Phone = phone,
                Email = email,

                IsActive = true,
                IsDeleted = false,
                EmailConfirmed = false,
                FailedLoginCount = 0,

                CreatedAt = now
            };

            // 6. Gán role Patient
            var userRole = new UserRole
            {
                User = user,
                RoleId = patientRole.Id,
                AssignedAt = now
            };

            await _unitOfWork.BeginTransactionAsync();
            transactionStarted = true;

            // 7. Thêm User
            await _unitOfWork.UserRepository.AddAsync(user);

            // 8. Thêm UserRole
            await _unitOfWork.UserRoleRepository.AddAsync(userRole);

            Patient patient;

            // 9. Xử lý Patient
            if (existingPatient == null)
            {
                // Trường hợp 1:
                // Chưa có account + chưa có hồ sơ Patient
                patient = new Patient
                {
                    User = user,
                    FullName = fullName,
                    Phone = phone,
                    Email = email,

                    DateOfBirth = request.DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(
                            request.DateOfBirth.Value)
                        : null,

                    PatientCode = "BN" +
                        Guid.NewGuid()
                            .ToString("N")[..18]
                            .ToUpperInvariant(),

                    IsActive = true,
                    CreatedAt = now
                };

                await _unitOfWork.PatientRepository
                    .AddAsync(patient);
            }
            else
            {
                // Trường hợp 2:
                // Đã có hồ sơ Patient nhưng chưa có account
                patient = existingPatient;

                // Liên kết hồ sơ cũ với User vừa tạo
                patient.User = user;
                patient.UpdatedAt = now;

                await _unitOfWork.PatientRepository
                    .UpdateAsync(patient);
            }

            // 10. Lưu tất cả thay đổi
            await _unitOfWork.SaveChangesAsync();

            // 11. Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;

            _logger.LogInformation(
                "Patient registration completed successfully.");

            return Response(
                201,
                UserResponseMessageDTO.RegisterSuccess,
                new
                {
                    UserId = user.Id,
                    PatientId = patient.Id,
                    PatientCode = patient.PatientCode
                });
        }
        catch (Exception ex)
        {
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
                        "Failed to rollback patient registration.");
                }
            }

            _logger.LogError(
                ex,
                "Patient registration failed.");

            return Response(
                500,
                UserResponseMessageDTO.RegisterFailed);
        }
    }

    // Helper function
    private static HttpResponseData<object?> Response(
        int statusCode,
        string message,
        object? content = null)
    {
        return new HttpResponseData<object?>
        {
            StatusCode = statusCode,
            Message = message,
            Content = content
        };
    }
}