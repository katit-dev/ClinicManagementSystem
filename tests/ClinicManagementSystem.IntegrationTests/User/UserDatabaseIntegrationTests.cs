using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;

using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

// =====================================================
// USER DATABASE INTEGRATION TESTS
// =====================================================
//
// xUnit
//    ↓
// CustomWebApplicationFactory
//    ↓
// ASP.NET Core DI
//    ↓
// UserService
//    ↓
// UnitOfWork / Repository
//    ↓
// EF Core
//    ↓
// SQL Server Testcontainer
//
// =====================================================

namespace ClinicManagementSystem.IntegrationTests;


public class UserDatabaseIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public UserDatabaseIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    // =====================================================
    // TEST 1
    //
    // Register thành công
    //
    // Chưa có:
    // - User
    // - Patient
    //
    // Service phải:
    // - tạo User
    // - hash password
    // - tạo Patient
    // - tạo UserRole = Patient
    // - transaction
    // - SaveChanges
    // - Commit
    //
    // Expected:
    // 201
    // RegisterSuccess
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn201_WhenRegistrationIsSuccessfulWithoutExistingPatient()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();


        await db.Database.EnsureCreatedAsync();


        // =================================================
        // ENSURE PATIENT ROLE
        // =================================================

        var patientRole =
            await db.Roles
                .SingleOrDefaultAsync(
                    r => r.Name == UserRoleConstant.Patient
                );


        if (patientRole == null)
        {
            patientRole =
                new Role
                {
                    Name =
                        UserRoleConstant.Patient,

                    Description =
                        "Integration test Patient role",

                    IsSystem = true
                };

            db.Roles.Add(patientRole);

            await db.SaveChangesAsync();
        }


        // =================================================
        // UNIQUE TEST DATA
        // =================================================

        var token =
            Guid.NewGuid()
                .ToString("N")[..10];


        var phone =
            $"09{token[..8]}";


        var email =
            $"it-user-{token}@example.com";


        const string password =
            "IntegrationTest@123";


        var request =
            new UserRegisterDTO
            {
                FullName =
                    $"Integration User {token}",

                Phone =
                    $"  {phone} ",

                Email =
                    $"  {email.ToUpperInvariant()}  ",

                DateOfBirth =
                    new DateTime(
                        2000,
                        1,
                        2
                    ),

                Password =
                    password,

                ConfirmPassword =
                    password
            };


        // =================================================
        // VERIFY PRE-CONDITION
        //
        // Chưa có User
        // Chưa có Patient
        // =================================================

        Assert.False(
            await db.Users.AnyAsync(
                u => u.Phone == phone
            )
        );


        Assert.False(
            await db.Patients.AnyAsync(
                p => p.Phone == phone
            )
        );


        // =================================================
        // GET SERVICE
        // =================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RegisterUserAsync(
                request
            );


        // =================================================
        // RESPONSE
        // =================================================

        Assert.Equal(
            201,
            result.StatusCode
        );


        Assert.Equal(
            UserResponseMessageDTO.RegisterSuccess,
            result.Message
        );


        Assert.NotNull(
            result.Content
        );


        // =================================================
        // USE A NEW SCOPE
        //
        // Kiểm tra dữ liệu thật đã được commit xuống DB.
        // =================================================

        await using var verifyScope =
            _factory.Services.CreateAsyncScope();


        var verifyDb =
            verifyScope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();


        // =================================================
        // USER
        // =================================================

        var savedUser =
            await verifyDb.Users
                .SingleAsync(
                    u => u.Phone == phone
                );


        Assert.Equal(
            phone,
            savedUser.Username
        );


        Assert.Equal(
            phone,
            savedUser.Phone
        );


        Assert.Equal(
            email.ToLowerInvariant(),
            savedUser.Email
        );


        Assert.Equal(
            $"Integration User {token}",
            savedUser.FullName
        );


        Assert.True(
            savedUser.IsActive
        );


        Assert.False(
            savedUser.IsDeleted
        );


        Assert.False(
            savedUser.EmailConfirmed
        );


        Assert.Equal(
            0,
            savedUser.FailedLoginCount
        );


        // =================================================
        // PASSWORD HASH
        // =================================================

        Assert.NotEqual(
            password,
            savedUser.PasswordHash
        );


        Assert.True(
            HelperFunction.VerifyPassword(
                password,
                savedUser.PasswordHash
            )
        );


        // =================================================
        // PATIENT
        // =================================================

        var savedPatient =
            await verifyDb.Patients
                .SingleAsync(
                    p => p.UserId == savedUser.Id
                );


        Assert.Equal(
            savedUser.Id,
            savedPatient.UserId
        );


        Assert.Equal(
            $"Integration User {token}",
            savedPatient.FullName
        );


        Assert.Equal(
            phone,
            savedPatient.Phone
        );


        Assert.Equal(
            email.ToLowerInvariant(),
            savedPatient.Email
        );


        Assert.Equal(
            new DateOnly(
                2000,
                1,
                2
            ),
            savedPatient.DateOfBirth
        );


        Assert.True(
            savedPatient.IsActive
        );


        Assert.False(
            string.IsNullOrWhiteSpace(
                savedPatient.PatientCode
            )
        );


        // =================================================
        // USER ROLE
        // =================================================

        var savedUserRole =
            await verifyDb.UserRoles
                .SingleAsync(
                    ur =>
                        ur.UserId ==
                            savedUser.Id
                        &&
                        ur.RoleId ==
                            patientRole.Id
                );


        Assert.Equal(
            savedUser.Id,
            savedUserRole.UserId
        );


        Assert.Equal(
            patientRole.Id,
            savedUserRole.RoleId
        );
    }

    // =====================================================
    // TEST 2
    //
    // Register
    //
    // Phone đã tồn tại.
    //
    // Expected:
    // 409
    // RegisterConflict
    //
    // Không được tạo User mới.
    // Không được tạo Patient mới.
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenPhoneAlreadyExists()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();


        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();


        await db.Database.EnsureCreatedAsync();


        // =================================================
        // UNIQUE TEST DATA
        // =================================================

        var token =
            Guid.NewGuid()
                .ToString("N")[..10];


        var phone =
            $"09{token[..8]}";


        var email =
            $"duplicate-phone-{token}@example.com";


        // =================================================
        // EXISTING USER
        // =================================================

        var existingUser =
            new User
            {
                Username = phone,

                Phone = phone,

                Email = email,

                FullName = "Existing User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "ExistingPassword@123"
                    ),

                IsActive = true,

                IsDeleted = false,

                EmailConfirmed = false,

                FailedLoginCount = 0,

                CreatedAt = DateTime.UtcNow
            };


        db.Users.Add(existingUser);

        await db.SaveChangesAsync();


        // =================================================
        // VERIFY PRE-CONDITION
        // =================================================

        Assert.True(
            await db.Users.AnyAsync(
                u =>
                    u.Username == phone ||
                    u.Phone == phone
            )
        );


        // =================================================
        // REGISTER REQUEST
        // =================================================

        var request =
            new UserRegisterDTO
            {
                FullName =
                    "New User",

                Phone =
                    phone,

                Email =
                    $"new-{token}@example.com",

                DateOfBirth =
                    new DateTime(
                        2000,
                        1,
                        1
                    ),

                Password =
                    "NewPassword@123",

                ConfirmPassword =
                    "NewPassword@123"
            };


        // =================================================
        // GET SERVICE
        // =================================================

        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RegisterUserAsync(
                request
            );


        // =================================================
        // ASSERT RESPONSE
        // =================================================

        Assert.Equal(
            409,
            result.StatusCode
        );


        Assert.Equal(
            UserResponseMessageDTO.RegisterConflict,
            result.Message
        );


        Assert.Null(
            result.Content
        );


        // =================================================
        // VERIFY DATABASE
        //
        // Chỉ có User cũ.
        // Không được tạo User mới.
        // =================================================

        var usersWithPhone =
            await db.Users
                .Where(
                    u =>
                        u.Username == phone ||
                        u.Phone == phone
                )
                .ToListAsync();


        Assert.Single(
            usersWithPhone
        );


        Assert.Equal(
            existingUser.Id,
            usersWithPhone[0].Id
        );


        // =================================================
        // VERIFY NEW EMAIL WAS NOT CREATED
        // =================================================

        Assert.False(
            await db.Users.AnyAsync(
                u =>
                    u.Email ==
                    $"new-{token}@example.com"
            )
        );
    }

    // =====================================================
    // TEST 3
    //
    // Register
    //
    // Email đã tồn tại.
    //
    // Expected:
    // 409
    // EmailAlreadyExists
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenEmailAlreadyExists()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var request =
            CreateRegisterRequest();


        var existingUser =
            await SeedUserAsync(db);


        existingUser.Email =
            request.Email.Trim().ToLowerInvariant();

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RegisterUserAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.EmailAlreadyExists,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        // =================================================
        // VERIFY DATABASE
        //
        // Không được tạo User mới.
        // =================================================

        var users =
            await db.Users
                .Where(
                    u =>
                        u.Email ==
                        request.Email.Trim().ToLowerInvariant()
                )
                .ToListAsync();


        Assert.Single(
            users
        );
    }


    // =====================================================
    // TEST 4
    //
    // Register
    //
    // Patient đã tồn tại và đã liên kết User.
    //
    // Expected:
    // 409
    // RegisterConflict
    //
    // Không được tạo User mới.
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenPatientIsAlreadyLinkedToUser()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var request =
            CreateRegisterRequest();


        // =================================================
        // USER ĐANG SỞ HỮU PATIENT
        //
        // Phone của User khác phone của Patient.
        //
        // Nếu giống nhau thì Service sẽ dừng ở
        // bước kiểm tra User trước.
        // =================================================

        var existingUser =
            await SeedUserAsync(db);


        var existingPatient =
            new Patient
            {
                UserId =
                    existingUser.Id,

                FullName =
                    "Existing Linked Patient",

                Phone =
                    request.Phone,

                Email =
                    "linked-patient@example.com",

                PatientCode =
                    $"IT-P-{Guid.NewGuid():N}"[..20],

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };


        db.Patients.Add(
            existingPatient
        );

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        // =================================================
        // VERIFY PRE-CONDITION
        // =================================================

        Assert.True(
            await db.Patients.AnyAsync(
                p =>
                    p.Id == existingPatient.Id &&
                    p.UserId == existingUser.Id
            )
        );


        Assert.False(
            await db.Users.AnyAsync(
                u =>
                    u.Phone == request.Phone
            )
        );


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RegisterUserAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            409,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RegisterConflict,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        // =================================================
        // VERIFY DATABASE
        //
        // Không tạo User mới.
        // Patient vẫn thuộc User cũ.
        // =================================================

        Assert.False(
            await db.Users.AnyAsync(
                u =>
                    u.Phone == request.Phone
            )
        );


        var savedPatient =
            await db.Patients
                .SingleAsync(
                    p =>
                        p.Id ==
                        existingPatient.Id
                );


        Assert.Equal(
            existingUser.Id,
            savedPatient.UserId
        );
    }


    // =====================================================
    // TEST 5
    //
    // Register thành công
    //
    // Patient đã tồn tại nhưng CHƯA có User.
    //
    // Expected:
    // 201
    //
    // Service phải:
    // - tạo User
    // - tạo UserRole
    // - link User vào Patient cũ
    // - update Email Patient
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn201_WhenExistingPatientIsNotLinked()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var request =
            CreateRegisterRequest();


        // =================================================
        // PATIENT ROLE
        // =================================================

        var patientRole =
            await EnsurePatientRoleAsync(
                db
            );


        // =================================================
        // EXISTING PATIENT
        // =================================================

        var existingPatient =
            new Patient
            {
                UserId =
                    null,

                FullName =
                    "Old Patient",

                Phone =
                    request.Phone,

                Email =
                    "old-patient@example.com",

                PatientCode =
                    $"IT-P-{Guid.NewGuid():N}"[..20],

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };


        db.Patients.Add(
            existingPatient
        );

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RegisterUserAsync(
                request
            );


        // =================================================
        // ASSERT RESPONSE
        // =================================================

        Assert.Equal(
            201,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RegisterSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );


        // =================================================
        // USER
        // =================================================

        var savedUser =
            await db.Users
                .SingleAsync(
                    u =>
                        u.Phone ==
                        request.Phone
                );


        Assert.Equal(
            request.Phone,
            savedUser.Phone
        );

        Assert.Equal(
            request.Phone,
            savedUser.Username
        );

        Assert.Equal(
            request.Email.Trim().ToLowerInvariant(),
            savedUser.Email
        );


        Assert.True(
            HelperFunction.VerifyPassword(
                request.Password,
                savedUser.PasswordHash
            )
        );


        // =================================================
        // PATIENT
        //
        // Patient cũ phải được link vào User mới.
        // =================================================

        var savedPatient =
            await db.Patients
                .SingleAsync(
                    p =>
                        p.Id ==
                        existingPatient.Id
                );


        Assert.Equal(
            savedUser.Id,
            savedPatient.UserId
        );


        Assert.Equal(
            request.Email.Trim().ToLowerInvariant(),
            savedPatient.Email
        );


        // =================================================
        // USER ROLE
        // =================================================

        var savedUserRole =
            await db.UserRoles
                .SingleAsync(
                    ur =>
                        ur.UserId ==
                            savedUser.Id
                        &&
                        ur.RoleId ==
                            patientRole.Id
                );


        Assert.Equal(
            savedUser.Id,
            savedUserRole.UserId
        );


        Assert.Equal(
            patientRole.Id,
            savedUserRole.RoleId
        );
    }


    // =====================================================
    // TEST 6
    //
    // Login thành công.
    //
    // Không có Role.
    //
    // Expected:
    // 200
    // LoginSuccess
    //
    // Kiểm tra:
    // - AccessToken
    // - RefreshToken
    // - LastLoginAt
    // - RefreshToken được lưu DB
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn200_WhenCredentialsAreValid()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        const string password =
            "Password123!";


        var user =
            await SeedUserAsync(
                db,
                password
            );


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        var request =
            new LoginRequestDTO
            {
                Username =
                    user.Username,

                Password =
                    password
            };


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.LoginAsync(
                request
            );


        // =================================================
        // ASSERT RESPONSE
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.LoginSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );


        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Content!.AccessToken
            )
        );


        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Content.RefreshToken
            )
        );


        Assert.Equal(
            user.Id,
            result.Content.User.Id
        );


        Assert.Empty(
            result.Content.User.Roles
        );


        // =================================================
        // VERIFY USER
        // =================================================

        var savedUser =
            await db.Users
                .SingleAsync(
                    u =>
                        u.Id ==
                        user.Id
                );


        Assert.NotNull(
            savedUser.LastLoginAt
        );


        // =================================================
        // VERIFY REFRESH TOKEN
        // =================================================

        var savedRefreshToken =
            await db.RefreshTokens
                .SingleAsync(
                    rt =>
                        rt.UserId ==
                        user.Id
                );


        Assert.Equal(
            user.Id,
            savedRefreshToken.UserId
        );


        Assert.False(
            string.IsNullOrWhiteSpace(
                savedRefreshToken.TokenHash
            )
        );


        // Token lưu DB phải là hash,
        // không phải plaintext token trả về.
        Assert.NotEqual(
            result.Content.RefreshToken,
            savedRefreshToken.TokenHash
        );
    }


    // =====================================================
    // TEST 7
    //
    // Refresh Token
    //
    // Token đã bị revoke.
    //
    // Expected:
    // 401
    // InvalidRefreshToken
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn401_WhenRefreshTokenIsRevoked()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var jwtAuthService =
            scope.ServiceProvider
                .GetRequiredService<IJwtAuthService>();


        var refreshToken =
            $"it-revoked-{Guid.NewGuid():N}";


        var tokenHash =
            jwtAuthService.HashRefreshToken(
                refreshToken
            );


        var revokedAt =
            DateTime.UtcNow.AddMinutes(-5);


        var storedRefreshToken =
            new RefreshToken
            {
                UserId =
                    user.Id,

                TokenHash =
                    tokenHash,

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt =
                    revokedAt,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-1)
            };


        db.RefreshTokens.Add(
            storedRefreshToken
        );

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        var request =
            new RefreshTokenRequestDTO
            {
                RefreshToken =
                    refreshToken
            };


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RefreshTokenAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidRefreshToken,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        // =================================================
        // VERIFY DATABASE
        // =================================================

        var savedToken =
            await db.RefreshTokens
                .SingleAsync(
                    rt =>
                        rt.Id ==
                        storedRefreshToken.Id
                );


        Assert.NotNull(
            savedToken.RevokedAt
        );
    }


    // =====================================================
    // TEST 8
    //
    // Refresh Token
    //
    // Token đã hết hạn.
    //
    // Expected:
    // 401
    // InvalidRefreshToken
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn401_WhenRefreshTokenIsExpired()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var user =
            await SeedUserAsync(
                db
            );


        var jwtAuthService =
            scope.ServiceProvider
                .GetRequiredService<IJwtAuthService>();


        var refreshToken =
            $"it-expired-{Guid.NewGuid():N}";


        var tokenHash =
            jwtAuthService.HashRefreshToken(
                refreshToken
            );


        var storedRefreshToken =
            new RefreshToken
            {
                UserId =
                    user.Id,

                TokenHash =
                    tokenHash,

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(-5),

                RevokedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-2)
            };


        db.RefreshTokens.Add(
            storedRefreshToken
        );

        await db.SaveChangesAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        var request =
            new RefreshTokenRequestDTO
            {
                RefreshToken =
                    refreshToken
            };


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.RefreshTokenAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidRefreshToken,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 9
    //
    // Forgot Password
    //
    // Email không tồn tại.
    //
    // Expected:
    // 404
    // EmailNotFound
    // =====================================================

    [Fact]
    public async Task ForgotPasswordAsync_ShouldReturn404_WhenEmailDoesNotExist()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        var email =
            $"missing-{Guid.NewGuid():N}@example.com";


        var request =
            new ForgotPasswordRequestDTO
            {
                Email =
                    email
            };


        // =================================================
        // PRE-CONDITION
        // =================================================

        Assert.False(
            await db.Users.AnyAsync(
                u =>
                    u.Email ==
                    email
            )
        );


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.ForgotPasswordAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.EmailNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 10
    //
    // Reset Password
    //
    // Email không tồn tại.
    //
    // Expected:
    // 404
    // EmailNotFound
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn404_WhenEmailDoesNotExist()
    {
        // =================================================
        // ARRANGE
        // =================================================

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<ClinicManagementDbContext>();

        await db.Database.EnsureCreatedAsync();


        var service =
            scope.ServiceProvider
                .GetRequiredService<IUserService>();


        var email =
            $"missing-reset-{Guid.NewGuid():N}@example.com";


        var request =
            new ResetPasswordRequestDTO
            {
                Email =
                    email,

                Otp =
                    "123456",

                NewPassword =
                    "NewPassword123!",

                ConfirmPassword =
                    "NewPassword123!"
            };


        // =================================================
        // PRE-CONDITION
        // =================================================

        Assert.False(
            await db.Users.AnyAsync(
                u =>
                    u.Email ==
                    email
            )
        );


        // =================================================
        // ACT
        // =================================================

        var result =
            await service.ResetPasswordAsync(
                request
            );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.EmailNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // HELPERS
    // =====================================================

    // =====================================================
    // CREATE UNIQUE PHONE
    // =====================================================

    private static string CreateUniquePhone()
    {
        return
            $"09{Random.Shared.Next(
                10_000_000,
                100_000_000
            )}";
    }


    // =====================================================
    // CREATE REGISTER REQUEST
    // =====================================================

    private static UserRegisterDTO CreateRegisterRequest()
    {
        var token =
            Guid.NewGuid()
                .ToString("N");

        var phone =
            CreateUniquePhone();

        return new UserRegisterDTO
        {
            FullName =
                $"Integration User {token[..8]}",

            Phone =
                phone,

            Email =
                $"it-user-{token}@example.com",

            DateOfBirth =
                new DateTime(
                    2000,
                    1,
                    1
                ),

            Password =
                "Password123!",

            ConfirmPassword =
                "Password123!"
        };
    }


    // =====================================================
    // SEED USER
    // =====================================================

    private static async Task<User> SeedUserAsync(
        ClinicManagementDbContext db,
        string password = "Password123!")
    {
        var token =
            Guid.NewGuid()
                .ToString("N");

        var phone =
            CreateUniquePhone();


        var user =
            new User
            {
                Username =
                    phone,

                PasswordHash =
                    HelperFunction.HashPassword(
                        password
                    ),

                Email =
                    $"it-user-{token}@example.com",

                FullName =
                    $"Integration User {token[..8]}",

                Phone =
                    phone,

                IsActive =
                    true,

                IsDeleted =
                    false,

                EmailConfirmed =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                FailedLoginCount =
                    0
            };


        db.Users.Add(
            user
        );

        await db.SaveChangesAsync();


        return user;
    }


    // =====================================================
    // ENSURE PATIENT ROLE
    // =====================================================

    private static async Task<Role> EnsurePatientRoleAsync(
        ClinicManagementDbContext db)
    {
        var role =
            await db.Roles
                .SingleOrDefaultAsync(
                    r =>
                        r.Name ==
                        UserRoleConstant.Patient
                );


        if (role != null)
        {
            return role;
        }


        role =
            new Role
            {
                Name =
                    UserRoleConstant.Patient,

                Description =
                    "Integration Test Patient Role",

                IsSystem =
                    true
            };


        db.Roles.Add(
            role
        );

        await db.SaveChangesAsync();


        return role;
    }



}