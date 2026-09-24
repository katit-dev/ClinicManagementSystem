using System.Text;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IUserService
{
    Task<HttpResponseData<object?>> RegisterUserAsync(UserRegisterDTO request);
    Task<HttpResponseData<AuthResponseDTO?>> LoginAsync(LoginRequestDTO request);
    Task<HttpResponseData<AuthResponseDTO?>> RefreshTokenAsync(RefreshTokenRequestDTO request);
    Task<HttpResponseData<object?>> ForgotPasswordAsync(ForgotPasswordRequestDTO request);
    Task<HttpResponseData<object?>> ResetPasswordAsync(ResetPasswordRequestDTO request);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IJwtAuthService _jwtAuthService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, IJwtAuthService jwtAuthService, IConfiguration configuration,
    IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jwtAuthService = jwtAuthService;
        _configuration = configuration;
        _emailService = emailService;

    }

    public async Task<HttpResponseData<object?>> ResetPasswordAsync(
    ResetPasswordRequestDTO request)
    {
        try
        {
            // 1. Chuẩn hóa dữ liệu
            string email =
                request.Email.Trim().ToLowerInvariant();

            string otp = request.Otp.Trim();

            // 2. BCrypt chỉ hỗ trợ tối đa 72 bytes
            if (Encoding.UTF8.GetByteCount(request.NewPassword) > 72)
            {
                return Response(
                    400,
                    UserResponseMessageDTO.PasswordTooLong);
            }

            // 3. Tìm account theo email
            var user = await _unitOfWork.UserRepository
                .SingleOrDefault(u => u.Email == email);

            if (user == null)
            {
                return Response(
                    404,
                    UserResponseMessageDTO.EmailNotFound);
            }

            // 4. Không cho reset account bị khóa/xóa
            if (!user.IsActive || user.IsDeleted)
            {
                return Response(
                    403,
                    UserResponseMessageDTO.AccountUnavailable);
            }

            // 5. Hash OTP user nhập
            string otpHash =
                _jwtAuthService.HashResetOtp(otp);

            var now = DateTime.UtcNow;

            // 6. Tìm OTP hợp lệ của đúng User
            var resetToken =
                await _unitOfWork.PasswordResetTokenRepository
                    .WhereSql(t =>
                        t.UserId == user.Id &&
                        t.TokenHash == otpHash &&
                        t.UsedAt == null &&
                        t.RevokedAt == null &&
                        t.ExpiresAt > now)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

            if (resetToken == null)
            {
                return Response(
                    400,
                    UserResponseMessageDTO.InvalidResetOtp);
            }

            // 7. Hash mật khẩu mới
            user.PasswordHash =
                HelperFunction.HashPassword(
                    request.NewPassword);

            user.UpdatedAt = now;

            // 8. Đánh dấu OTP đã sử dụng
            resetToken.UsedAt = now;

            await _unitOfWork.UserRepository
                .UpdateAsync(user);

            await _unitOfWork.PasswordResetTokenRepository
                .UpdateAsync(resetToken);

            // 9. Lưu User + ResetToken cùng lúc
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Password reset completed successfully for User {UserId}.", user.Id);

            return Response(200, UserResponseMessageDTO.ResetPasswordSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password failed.");

            return Response(500, UserResponseMessageDTO.ResetPasswordFailed);
        }
    }

    public async Task<HttpResponseData<object?>> ForgotPasswordAsync(
    ForgotPasswordRequestDTO request)
    {
        try
        {
            string email =
                request.Email.Trim().ToLowerInvariant();

            // 1. Tìm User theo email
            var user = await _unitOfWork.UserRepository
                .SingleOrDefault(u => u.Email == email);

            if (user == null)
            {
                return Response(
                    404,
                    UserResponseMessageDTO.EmailNotFound);
            }

            var now = DateTime.UtcNow;

            // 2. Lấy các reset token cũ còn hiệu lực
            var activeTokens =
                await _unitOfWork.PasswordResetTokenRepository
                    .WhereSql(t =>
                        t.UserId == user.Id &&
                        t.UsedAt == null &&
                        t.RevokedAt == null &&
                        t.ExpiresAt > now)
                    .ToListAsync();

            // 3. Revoke(vo hieu hoa) các token cũ
            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;

                await _unitOfWork.PasswordResetTokenRepository
                    .UpdateAsync(token);
            }

            // 4. Tạo Reset otp mới
            string otp = _jwtAuthService.GenerateResetOtp();

            // 5. Hash trước khi lưu DB
            string otpHash = _jwtAuthService.HashResetOtp(otp);

            int expirationMinutes = _configuration.GetValue<int>("Jwt:PasswordResetOtpExpirationMinutes");

            // 6. Tạo record mới
            var resetTokenModel = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = otpHash,
                ExpiresAt = now.AddMinutes(expirationMinutes),
                CreatedAt = now
            };

            await _unitOfWork.PasswordResetTokenRepository.AddAsync(resetTokenModel);

            await _unitOfWork.SaveChangesAsync();

            // Gửi OTP thật qua email
            await _emailService.SendPasswordResetOtpAsync(user.Email!, otp);

            _logger.LogInformation("Password reset OTP sent for User {UserId}.", user.Id);

            return Response(200, UserResponseMessageDTO.ForgotPasswordSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Forgot password request failed.");

            return Response(
                500,
                UserResponseMessageDTO.ForgotPasswordFailed);
        }
    }

    public async Task<HttpResponseData<AuthResponseDTO?>> RefreshTokenAsync(
    RefreshTokenRequestDTO request)
    {
        try
        {
            // 1. Hash Refresh Token client gửi lên
            string refreshTokenHash =
                _jwtAuthService.HashRefreshToken(
                    request.RefreshToken);

            // 2. Tìm hash trong database
            var storedRefreshToken =
                await _unitOfWork.RefreshTokenRepository
                    .SingleOrDefault(
                        rt => rt.TokenHash == refreshTokenHash);

            // 3. Token không tồn tại
            if (storedRefreshToken == null)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidRefreshToken);
            }

            // 4. Token đã bị revoke
            if (storedRefreshToken.RevokedAt.HasValue)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidRefreshToken);
            }

            // 5. Token đã hết hạn
            if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidRefreshToken);
            }

            // 6. Lấy User sở hữu Refresh Token
            var user =
                await _unitOfWork.UserRepository
                    .GetByIdAsync(
                        storedRefreshToken.UserId);

            if (user == null)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidRefreshToken);
            }

            // 7. Kiểm tra trạng thái account
            if (!user.IsActive || user.IsDeleted)
            {
                return LoginResponse(
                    403,
                    UserResponseMessageDTO.AccountUnavailable);
            }

            // 8. Lấy Roles
            var roles =
                await _unitOfWork.UserRoleRepository
                    .WhereSql(ur => ur.UserId == user.Id)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

            // 9. Lấy profile ID theo Role
            int? doctorId = null;
            int? patientId = null;

            if (roles.Contains(UserRoleConstant.Doctor))
            {
                doctorId =
                    await _unitOfWork.DoctorRepository
                        .WhereSql(d => d.UserId == user.Id)
                        .Select(d => (int?)d.Id)
                        .FirstOrDefaultAsync();
            }

            if (roles.Contains(UserRoleConstant.Patient))
            {
                patientId =
                    await _unitOfWork.PatientRepository
                        .WhereSql(p => p.UserId == user.Id)
                        .Select(p => (int?)p.Id)
                        .FirstOrDefaultAsync();
            }

            // 10. Tạo Access Token mới
            string newAccessToken =
                _jwtAuthService.GenerateAccessToken(
                    user,
                    roles);

            // 11. Tạo Refresh Token mới
            string newRefreshToken =
                _jwtAuthService.GenerateRefreshToken();

            string newRefreshTokenHash =
                _jwtAuthService.HashRefreshToken(
                    newRefreshToken);

            var now = DateTime.UtcNow;

            int refreshTokenExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            // 12. Revoke Refresh Token cũ
            storedRefreshToken.RevokedAt = now;

            await _unitOfWork.RefreshTokenRepository
                .UpdateAsync(storedRefreshToken);

            // 13. Lưu Refresh Token mới
            var refreshTokenModel = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = now.AddDays(
                    refreshTokenExpirationDays),
                CreatedAt = now
            };

            await _unitOfWork.RefreshTokenRepository
                .AddAsync(refreshTokenModel);

            // 14. Save DB
            await _unitOfWork.SaveChangesAsync();

            int accessTokenExpirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:AccessTokenExpirationMinutes");

            // 15. Response
            var authResponse = new AuthResponseDTO
            {
                AccessToken = newAccessToken,

                // Token mới thật được trả về client
                RefreshToken = newRefreshToken,

                ExpiresIn =
                    accessTokenExpirationMinutes * 60,

                User = new AuthUserDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Roles = roles,
                    DoctorId = doctorId,
                    PatientId = patientId
                }
            };

            _logger.LogInformation(
                "Refresh token successfully rotated for User {UserId}.",
                user.Id);

            return LoginResponse(
                200,
                UserResponseMessageDTO.RefreshTokenSuccess,
                authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Refresh token operation failed.");

            return LoginResponse(
                500,
                UserResponseMessageDTO.RefreshTokenFailed);
        }
    }

    public async Task<HttpResponseData<object?>> RegisterUserAsync(
    UserRegisterDTO request)
    {
        // Chuẩn hóa dữ liệu
        string fullName = request.FullName.Trim();
        string phone = request.Phone.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

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
            // 1. Kiểm tra số điện thoại đã có account chưa
            bool userExists = await _unitOfWork.UserRepository
                .WhereSql(u =>
                    u.Username == phone ||
                    u.Phone == phone)
                .AnyAsync();

            if (userExists)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // 2. Kiểm tra email đã được sử dụng chưa
            var existingEmailUser =
                await _unitOfWork.UserRepository
                    .SingleOrDefault(u => u.Email == email);

            if (existingEmailUser != null)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.EmailAlreadyExists);
            }

            // 3. Tìm hồ sơ Patient theo số điện thoại
            var existingPatient = await _unitOfWork.PatientRepository
                .WhereSql(p => p.Phone == phone)
                .FirstOrDefaultAsync();

            // 4. Nếu Patient đã liên kết với account
            if (existingPatient != null &&
                existingPatient.UserId.HasValue)
            {
                return Response(
                    409,
                    UserResponseMessageDTO.RegisterConflict);
            }

            // 5. Lấy role Patient
            var patientRole = await _unitOfWork.RoleRepository
                .WhereSql(r => r.Name == UserRoleConstant.Patient)
                .FirstOrDefaultAsync();

            if (patientRole == null)
            {
                _logger.LogError(
                    "Patient role was not found.");

                return Response(
                    500,
                    UserResponseMessageDTO.PatientRoleNotFound);
            }

            var now = DateTime.UtcNow;

            // 6. Tạo account
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

            // 7. Gán role Patient
            var userRole = new UserRole
            {
                User = user,
                RoleId = patientRole.Id,
                AssignedAt = now
            };

            // 8. Bắt đầu transaction
            await _unitOfWork.BeginTransactionAsync();

            transactionStarted = true;

            // 9. Thêm User
            await _unitOfWork.UserRepository
                .AddAsync(user);

            // 10. Thêm UserRole
            await _unitOfWork.UserRoleRepository
                .AddAsync(userRole);

            Patient patient;

            // 11. Xử lý Patient
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

                    PatientCode =
                        "BN" +
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

                // Liên kết hồ sơ Patient cũ với User vừa tạo
                patient.User = user;

                // Đồng bộ email đăng ký vào hồ sơ Patient
                patient.Email = email;

                patient.UpdatedAt = now;

                await _unitOfWork.PatientRepository
                    .UpdateAsync(patient);
            }

            // 12. Lưu tất cả thay đổi
            await _unitOfWork.SaveChangesAsync();

            // 13. Commit transaction
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

    public async Task<HttpResponseData<AuthResponseDTO?>> LoginAsync(
    LoginRequestDTO request)
    {
        try
        {
            string username = request.Username.Trim();

            // 1. Tìm User theo Username hoặc Email
            var user = await _unitOfWork.UserRepository
                .SingleOrDefault(
                    u => u.Username == username ||
                         u.Email == username);

            if (user == null)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidCredentials);
            }

            // 2. Kiểm tra password
            bool passwordValid =
                HelperFunction.VerifyPassword(
                    request.Password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                return LoginResponse(
                    401,
                    UserResponseMessageDTO.InvalidCredentials);
            }

            // 3. Kiểm tra trạng thái account
            if (!user.IsActive || user.IsDeleted)
            {
                return LoginResponse(
                    403,
                    UserResponseMessageDTO.AccountUnavailable);
            }

            // 4. Lấy danh sách Role
            var roles = await _unitOfWork.UserRoleRepository
                .WhereSql(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            // 5. Chỉ lấy DoctorId / PatientId theo Role
            int? doctorId = null;
            int? patientId = null;

            if (roles.Contains(UserRoleConstant.Doctor))
            {
                doctorId = await _unitOfWork.DoctorRepository
                    .WhereSql(d => d.UserId == user.Id)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync();
            }

            if (roles.Contains(UserRoleConstant.Patient))
            {
                patientId = await _unitOfWork.PatientRepository
                    .WhereSql(p => p.UserId == user.Id)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync();
            }

            // 6. Tạo Access Token
            string accessToken =
                _jwtAuthService.GenerateAccessToken(
                    user,
                    roles);

            // 7. Tạo Refresh Token
            string refreshToken =
                _jwtAuthService.GenerateRefreshToken();

            // 8. Hash Refresh Token
            string refreshTokenHash =
                _jwtAuthService.HashRefreshToken(
                    refreshToken);

            int refreshTokenExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            // 9. Lưu HASH của Refresh Token vào DB
            var refreshTokenModel = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RefreshTokenRepository
                .AddAsync(refreshTokenModel);

            // 10. Cập nhật thời gian login
            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.UserRepository
                .UpdateAsync(user);

            // 11. Lưu DB
            await _unitOfWork.SaveChangesAsync();

            int accessTokenExpirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:AccessTokenExpirationMinutes");

            // 12. Tạo response
            var authResponse = new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,

                ExpiresIn =
                    accessTokenExpirationMinutes * 60,

                User = new AuthUserDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Roles = roles,
                    DoctorId = doctorId,
                    PatientId = patientId
                }
            };

            _logger.LogInformation(
                "User {UserId} logged in successfully.",
                user.Id);

            return LoginResponse(
                200,
                UserResponseMessageDTO.LoginSuccess,
                authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "User login failed.");

            return LoginResponse(
                500,
                UserResponseMessageDTO.LoginFailed);
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

    private static HttpResponseData<AuthResponseDTO?> LoginResponse(int statusCode, string message, AuthResponseDTO? content = null)
    {
        return new HttpResponseData<AuthResponseDTO?>
        {
            StatusCode = statusCode,
            Message = message,
            Content = content
        };
    }


}