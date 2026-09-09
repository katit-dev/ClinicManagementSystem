using System.ComponentModel.DataAnnotations;
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
        // 1. Kiểm tra dữ liệu đầu vào
        if (request == null)
        {
            return Response(
                400,
                UserResponseMessageDTO.InvalidRegisterData);
        }

        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        string fullName = request.FullName?.Trim() ?? string.Empty;
        string phone = request.Phone?.Trim() ?? string.Empty;

        string? email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        if (!isValid ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(phone) ||
            (request.DateOfBirth.HasValue &&
             request.DateOfBirth.Value.Date > DateTime.UtcNow.Date))
        {
            return Response(
                400,
                UserResponseMessageDTO.InvalidRegisterData);
        }

        // BCrypt thông thường chỉ sử dụng tối đa 72 byte đầu vào.
        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            return Response(
                400,
                UserResponseMessageDTO.PasswordTooLong);
        }

        bool transactionStarted = false;

        try
        {
            // 2. Kiểm tra tài khoản đã tồn tại
            // Quy ước: Username = số điện thoại.
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

            // 3. Kiểm tra hồ sơ bệnh nhân cũ
            // Chưa tự liên kết hồ sơ chỉ vì trùng số điện thoại.
            bool patientExists = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Phone == phone)
                .AnyAsync();

            if (patientExists)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // 4. Lấy Role Patient từ database
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

            // 5. Tạo User
            var now = DateTime.UtcNow;

            var user = new User
            {
                Username = phone,
                PasswordHash = HelperFunction.HashPassword(request.Password),
                FullName = fullName,
                Phone = phone,
                Email = email,
                IsActive = true,
                IsDeleted = false,
                EmailConfirmed = false,
                FailedLoginCount = 0,
                CreatedAt = now
            };

            // 6. Gán Role Patient
            var userRole = new UserRole
            {
                User = user,
                RoleId = patientRole.Id,
                AssignedAt = now
            };

            // 7. Tạo hồ sơ bệnh nhân mới
            var patient = new Patient
            {
                User = user,
                FullName = fullName,
                Phone = phone,
                Email = email,
                DateOfBirth = request.DateOfBirth.HasValue
                    ? DateOnly.FromDateTime(request.DateOfBirth.Value)
                    : null,
                PatientCode = "BN" +
                    Guid.NewGuid().ToString("N")[..18].ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now
            };

            // 8. Bắt đầu transaction
            await _unitOfWork.BeginTransactionAsync();
            transactionStarted = true;

            // 9. Chuẩn bị thay đổi ở 3 Repository
            await _unitOfWork.UserRepository.AddAsync(user);
            await _unitOfWork.UserRoleRepository.AddAsync(userRole);
            await _unitOfWork.PatientRepository.AddAsync(patient);

            // 10. Lưu và commit
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;

            _logger.LogInformation(
                "Patient registration completed successfully.");

            // Không trả PasswordHash
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
            // Nếu lỗi xảy ra khi transaction còn mở thì rollback.
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

            // Lỗi trùng unique constraint
            if (HasSqlError(ex, 2601, 2627))
            {
                _logger.LogWarning(
                    "Patient registration rejected due to a uniqueness conflict.");

                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // SQL Server deadlock
            if (HasSqlError(ex, 1205))
            {
                _logger.LogWarning(
                    "Patient registration transaction was selected as a deadlock victim.");

                return Response(
                    503,
                    UserResponseMessageDTO.RegisterRetry);
            }

            _logger.LogError(
                ex,
                "Patient registration failed.");

            return Response(
                500,
                UserResponseMessageDTO.RegisterFailed);
        }
    }

    private static bool HasSqlError(
        Exception exception,
        params int[] errorNumbers)
    {
        for (Exception? current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                errorNumbers.Contains(sqlException.Number))
            {
                return true;
            }
        }

        return false;
    }

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