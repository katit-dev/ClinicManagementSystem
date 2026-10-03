using System.Linq.Expressions;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;

using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using MockQueryable;



namespace ClinicManagementSystem.UnitTests.Application.User;


// =====================================================
// ALIASES
// =====================================================
//
// Namespace test là:
//
// ClinicManagementSystem.UnitTests.Application.User
//
// nên model User có thể bị trùng tên namespace.
// Dùng alias để tránh CS0118.
// =====================================================

using UserEntity =
    ClinicManagementSystem.Infrastructure.Models.User;


public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly Mock<IJwtAuthService> _jwtAuthServiceMock;

    private readonly Mock<IEmailService> _emailServiceMock;

    private readonly IConfiguration _configuration;

    private readonly UserService _service;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public UserServiceTests()
    {
        _unitOfWorkMock =
            new Mock<IUnitOfWork>();

        _jwtAuthServiceMock =
            new Mock<IJwtAuthService>();

        _emailServiceMock =
            new Mock<IEmailService>();


        _configuration =
            new ConfigurationBuilder()
                .Build();


        _service =
            new UserService(
                _unitOfWorkMock.Object,
                NullLogger<UserService>.Instance,
                _jwtAuthServiceMock.Object,
                _configuration,
                _emailServiceMock.Object
            );
    }


    // =====================================================
    // HELPERS
    // =====================================================

    private void SetupUserRepository(
        UserEntity? user)
    {
        var repositoryMock =
            new Mock<IUserRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    UserEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                user
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupRefreshTokenRepository(
        RefreshToken? refreshToken)
    {
        var repositoryMock =
            new Mock<IRefreshTokenRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    RefreshToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                refreshToken
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private static UserRegisterDTO
        CreateValidRegisterRequest()
    {
        return new UserRegisterDTO
        {
            FullName =
                "Nguyen Van A",

            Phone =
                "0901234567",

            Email =
                "test@example.com",

            Password =
                "Password123!",

            DateOfBirth =
                DateTime.UtcNow.AddYears(-20)
        };
    }


    private static LoginRequestDTO
        CreateValidLoginRequest()
    {
        return new LoginRequestDTO
        {
            Username =
                "0901234567",

            Password =
                "Password123!"
        };
    }


    private static RefreshTokenRequestDTO
        CreateValidRefreshTokenRequest()
    {
        return new RefreshTokenRequestDTO
        {
            RefreshToken =
                "refresh-token-test"
        };
    }


    private static ForgotPasswordRequestDTO
        CreateValidForgotPasswordRequest()
    {
        return new ForgotPasswordRequestDTO
        {
            Email =
                "test@example.com"
        };
    }


    private static ResetPasswordRequestDTO
        CreateValidResetPasswordRequest()
    {
        return new ResetPasswordRequestDTO
        {
            Email =
                "test@example.com",

            Otp =
                "123456",

            NewPassword =
                "NewPassword123!"
        };
    }


    // =====================================================
    // TEST 1
    //
    // Register
    //
    // DateOfBirth ở tương lai.
    //
    // Expected:
    // 400
    // InvalidDateOfBirth
    //
    // Đây là validation xảy ra trước database.
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn400_WhenDateOfBirthIsInTheFuture()
    {
        // Arrange

        var request =
            CreateValidRegisterRequest();

        request.DateOfBirth =
            DateTime.UtcNow.AddDays(1);


        // Act

        var result =
            await _service
                .RegisterUserAsync(
                    request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidDateOfBirth,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 2
    //
    // Register
    //
    // Password > 72 UTF-8 bytes.
    //
    // Expected:
    // 400
    // PasswordTooLong
    //
    // BCrypt chỉ hỗ trợ tối đa 72 bytes.
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn400_WhenPasswordExceeds72Bytes()
    {
        // Arrange

        var request =
            CreateValidRegisterRequest();

        request.Password =
            new string(
                'a',
                73
            );


        // Act

        var result =
            await _service
                .RegisterUserAsync(
                    request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .PasswordTooLong,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.BeginTransactionAsync(),
            Times.Never
        );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 3
    //
    // Login
    //
    // User không tồn tại.
    //
    // Expected:
    // 401
    // InvalidCredentials
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn401_WhenUserDoesNotExist()
    {
        // Arrange

        SetupUserRepository(
            null
        );

        var request =
            CreateValidLoginRequest();


        // Act

        var result =
            await _service
                .LoginAsync(
                    request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidCredentials,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 4
    //
    // Login
    //
    // User tồn tại nhưng password sai.
    //
    // Expected:
    // 401
    // InvalidCredentials
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn401_WhenPasswordIsInvalid()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id =
                    1,

                Username =
                    "0901234567",

                Email =
                    "test@example.com",

                FullName =
                    "Nguyen Van A",

                Phone =
                    "0901234567",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "CorrectPassword123!"
                    ),

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        SetupUserRepository(
            user
        );


        var request =
            new LoginRequestDTO
            {
                Username =
                    "0901234567",

                Password =
                    "WrongPassword123!"
            };


        // Act

        var result =
            await _service
                .LoginAsync(
                    request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidCredentials,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 5
    //
    // Login
    //
    // Password đúng nhưng account inactive.
    //
    // Expected:
    // 403
    // AccountUnavailable
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn403_WhenAccountIsInactive()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id =
                    1,

                Username =
                    "0901234567",

                Email =
                    "test@example.com",

                FullName =
                    "Nguyen Van A",

                Phone =
                    "0901234567",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "Password123!"
                    ),

                IsActive =
                    false,

                IsDeleted =
                    false
            };


        SetupUserRepository(
            user
        );


        var request =
            new LoginRequestDTO
            {
                Username =
                    "0901234567",

                Password =
                    "Password123!"
            };


        // Act

        var result =
            await _service
                .LoginAsync(
                    request
                );


        // Assert

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .AccountUnavailable,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 6
    //
    // Refresh Token
    //
    // Token không tồn tại trong DB.
    //
    // Expected:
    // 401
    // InvalidRefreshToken
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn401_WhenRefreshTokenDoesNotExist()
    {
        // Arrange

        SetupRefreshTokenRepository(
            null
        );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "refresh-token-test"
                    )
            )
            .Returns(
                "hashed-refresh-token"
            );


        var request =
            CreateValidRefreshTokenRequest();


        // Act

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidRefreshToken,
            result.Message
        );

        Assert.Null(
            result.Content
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
        // Arrange

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "refresh-token-test"
                    )
            )
            .Returns(
                "hashed-refresh-token"
            );


        var storedRefreshToken =
            new RefreshToken
            {
                UserId =
                    1,

                TokenHash =
                    "hashed-refresh-token",

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt =
                    DateTime.UtcNow.AddMinutes(-5),

                CreatedAt =
                    DateTime.UtcNow.AddDays(-1)
            };


        SetupRefreshTokenRepository(
            storedRefreshToken
        );


        var request =
            CreateValidRefreshTokenRequest();


        // Act

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidRefreshToken,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.UserRepository,
            Times.Never
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
        // Arrange

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "refresh-token-test"
                    )
            )
            .Returns(
                "hashed-refresh-token"
            );


        var storedRefreshToken =
            new RefreshToken
            {
                UserId =
                    1,

                TokenHash =
                    "hashed-refresh-token",

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(-5),

                RevokedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-2)
            };


        SetupRefreshTokenRepository(
            storedRefreshToken
        );


        var request =
            CreateValidRefreshTokenRequest();


        // Act

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // Assert

        Assert.Equal(
            401,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .InvalidRefreshToken,
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
        // Arrange

        SetupUserRepository(
            null
        );


        var request =
            CreateValidForgotPasswordRequest();


        // Act

        var result =
            await _service
                .ForgotPasswordAsync(
                    request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .EmailNotFound,
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
        // Arrange

        SetupUserRepository(
            null
        );


        var request =
            CreateValidResetPasswordRequest();


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    request
                );


        // Assert

        Assert.Equal(
            404,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO
                .EmailNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }

    // =====================================================
    // HELPERS - TEST 11 -> 20
    // =====================================================

    private void SetupRegisterUserRepository(
        bool userExists,
        UserEntity? existingEmailUser)
    {
        var repositoryMock =
            new Mock<IUserRepository>();

        var existingUsers =
            userExists
                ? new List<UserEntity>
                {
                new UserEntity
                {
                    Id = 99,
                    Username = "0901234567",
                    Phone = "0901234567"
                }
                }
                : new List<UserEntity>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .Returns(
                existingUsers
                    .BuildMock()
            );

        repositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                existingEmailUser
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupPatientWhereSql(
        params Patient[] patients)
    {
        var repositoryMock =
            new Mock<IPatientRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<Patient, bool>
                            >
                        >()
                    )
            )
            .Returns(
                patients
                    .ToList()
                    .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x => x.PatientRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupRoleWhereSql(
        params Role[] roles)
    {
        var repositoryMock =
            new Mock<IRoleRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<Role, bool>
                            >
                        >()
                    )
            )
            .Returns(
                roles
                    .ToList()
                    .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x => x.RoleRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupTransaction()
    {
        _unitOfWorkMock
            .Setup(
                x => x.BeginTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.CommitTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.RollbackTransactionAsync()
            )
            .Returns(
                Task.CompletedTask
            );
    }


    // =====================================================
    // TEST 11
    //
    // Register - Phone đã tồn tại
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenPhoneAlreadyExists()
    {
        var request =
            CreateValidRegisterRequest();

        SetupRegisterUserRepository(
            userExists: true,
            existingEmailUser: null
        );

        var result =
            await _service
                .RegisterUserAsync(request);

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

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 12
    //
    // Register - Email đã tồn tại
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var request =
            CreateValidRegisterRequest();

        var existingUser =
            new UserEntity
            {
                Id = 10,
                Username = "another-user",
                Phone = "0911111111",
                Email = "test@example.com",
                FullName = "Existing User",
                IsActive = true,
                IsDeleted = false
            };

        SetupRegisterUserRepository(
            userExists: false,
            existingEmailUser: existingUser
        );

        var result =
            await _service
                .RegisterUserAsync(request);

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

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 13
    //
    // Register - Patient đã liên kết User
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn409_WhenPatientIsAlreadyLinkedToUser()
    {
        var request =
            CreateValidRegisterRequest();

        SetupRegisterUserRepository(
            userExists: false,
            existingEmailUser: null
        );

        var existingPatient =
            new Patient
            {
                Id = 20,
                Phone = request.Phone,
                UserId = 99,
                FullName = "Existing Patient"
            };

        SetupPatientWhereSql(
            existingPatient
        );

        var result =
            await _service
                .RegisterUserAsync(request);

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

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 14
    //
    // Register - Patient Role không tồn tại
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn500_WhenPatientRoleDoesNotExist()
    {
        var request =
            CreateValidRegisterRequest();

        SetupRegisterUserRepository(
            userExists: false,
            existingEmailUser: null
        );

        SetupPatientWhereSql();

        SetupRoleWhereSql();

        var result =
            await _service
                .RegisterUserAsync(request);

        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.PatientRoleNotFound,
            result.Message
        );

        Assert.Null(
            result.Content
        );

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 15
    //
    // Register thành công
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn201_WhenRegistrationIsSuccessful()
    {
        var request =
            CreateValidRegisterRequest();

        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .Returns(
                Array.Empty<UserEntity>()
                    .BuildMock()
            );

        userRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                (UserEntity?)null
            );

        UserEntity? addedUser = null;

        userRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<UserEntity>()
                    )
            )
            .Callback(
                (UserEntity user) =>
                {
                    addedUser = user;
                }
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        // Patient chưa tồn tại

        SetupPatientWhereSql();


        // Patient Role tồn tại

        var patientRole =
            new Role
            {
                Id = 5,
                Name = UserRoleConstant.Patient
            };

        SetupRoleWhereSql(
            patientRole
        );


        UserRole? addedUserRole = null;

        var userRoleRepositoryMock =
            new Mock<IUserRoleRepository>();

        userRoleRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<UserRole>()
                    )
            )
            .Callback(
                (UserRole userRole) =>
                {
                    addedUserRole = userRole;
                }
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRoleRepository
            )
            .Returns(
                userRoleRepositoryMock.Object
            );


        Patient? addedPatient = null;

        var patientRepositoryMock =
            new Mock<IPatientRepository>();

        patientRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<Patient, bool>
                            >
                        >()
                    )
            )
            .Returns(
                Array.Empty<Patient>()
                    .BuildMock()
            );

        patientRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<Patient>()
                    )
            )
            .Callback(
                (Patient patient) =>
                {
                    addedPatient = patient;
                }
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x => x.PatientRepository
            )
            .Returns(
                patientRepositoryMock.Object
            );


        SetupTransaction();


        // Act

        var result =
            await _service
                .RegisterUserAsync(request);


        // Assert

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

        Assert.NotNull(
            addedUser
        );

        Assert.Equal(
            request.Phone,
            addedUser!.Phone
        );

        Assert.Equal(
            request.Phone,
            addedUser.Username
        );

        Assert.Equal(
            request.Email.ToLowerInvariant(),
            addedUser.Email
        );

        Assert.True(
            addedUser.IsActive
        );

        Assert.False(
            addedUser.IsDeleted
        );

        Assert.NotNull(
            addedUserRole
        );

        Assert.Equal(
            patientRole.Id,
            addedUserRole!.RoleId
        );

        Assert.Same(
            addedUser,
            addedUserRole.User
        );

        Assert.NotNull(
            addedPatient
        );

        Assert.Same(
            addedUser,
            addedPatient!.User
        );

        Assert.Equal(
            request.Phone,
            addedPatient.Phone
        );

        Assert.Equal(
            request.Email.ToLowerInvariant(),
            addedPatient.Email
        );

        Assert.False(
            string.IsNullOrWhiteSpace(
                addedPatient.PatientCode
            )
        );

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x => x.RollbackTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 16
    //
    // Register - Exception sau khi BeginTransaction
    // => Rollback
    // =====================================================

    [Fact]
    public async Task RegisterUserAsync_ShouldReturn500AndRollback_WhenAddUserThrows()
    {
        var request =
            CreateValidRegisterRequest();

        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .Returns(
                Array.Empty<UserEntity>()
                    .BuildMock()
            );

        userRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                (UserEntity?)null
            );

        userRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<UserEntity>()
                    )
            )
            .ThrowsAsync(
                new InvalidOperationException(
                    "Test exception"
                )
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        SetupPatientWhereSql();

        SetupRoleWhereSql(
            new Role
            {
                Id = 5,
                Name = UserRoleConstant.Patient
            }
        );

        SetupTransaction();


        var result =
            await _service
                .RegisterUserAsync(request);


        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RegisterFailed,
            result.Message
        );

        Assert.Null(
            result.Content
        );

        _unitOfWorkMock.Verify(
            x => x.BeginTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x => x.RollbackTransactionAsync(),
            Times.Once
        );

        _unitOfWorkMock.Verify(
            x => x.CommitTransactionAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 17
    //
    // Login - Account đã bị deleted
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn403_WhenAccountIsDeleted()
    {
        var user =
            new UserEntity
            {
                Id = 17,
                Username = "0901234567",
                Email = "test@example.com",
                FullName = "Deleted User",
                Phone = "0901234567",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "Password123!"
                    ),

                IsActive = true,
                IsDeleted = true
            };

        SetupUserRepository(
            user
        );

        var request =
            new LoginRequestDTO
            {
                Username = "0901234567",
                Password = "Password123!"
            };

        var result =
            await _service
                .LoginAsync(request);

        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.AccountUnavailable,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 18
    //
    // Refresh Token - Account owner inactive
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn403_WhenTokenOwnerAccountIsUnavailable()
    {
        var request =
            CreateValidRefreshTokenRequest();

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        request.RefreshToken
                    )
            )
            .Returns(
                "hashed-refresh-token"
            );

        var storedRefreshToken =
            new RefreshToken
            {
                UserId = 18,
                TokenHash = "hashed-refresh-token",

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt = null,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-1)
            };

        SetupRefreshTokenRepository(
            storedRefreshToken
        );

        var user =
            new UserEntity
            {
                Id = 18,
                Username = "0901234568",
                Email = "inactive@example.com",
                FullName = "Inactive User",
                Phone = "0901234568",
                IsActive = false,
                IsDeleted = false
            };

        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.GetByIdAsync(18)
            )
            .ReturnsAsync(
                user
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        var result =
            await _service
                .RefreshTokenAsync(request);


        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.AccountUnavailable,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 19
    //
    // Forgot Password - Repository exception
    // =====================================================

    [Fact]
    public async Task ForgotPasswordAsync_ShouldReturn500_WhenRepositoryThrows()
    {
        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .ThrowsAsync(
                new InvalidOperationException(
                    "Test exception"
                )
            );

        _unitOfWorkMock
            .Setup(
                x => x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        var request =
            CreateValidForgotPasswordRequest();


        var result =
            await _service
                .ForgotPasswordAsync(request);


        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.ForgotPasswordFailed,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 20
    //
    // Reset Password - Account unavailable
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn403_WhenAccountIsUnavailable()
    {
        var user =
            new UserEntity
            {
                Id = 20,
                Email = "test@example.com",
                Username = "0901234570",
                Phone = "0901234570",
                FullName = "Inactive User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "OldPassword123!"
                    ),

                IsActive = false,
                IsDeleted = false
            };

        SetupUserRepository(
            user
        );


        var request =
            CreateValidResetPasswordRequest();


        var result =
            await _service
                .ResetPasswordAsync(request);


        Assert.Equal(
            403,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.AccountUnavailable,
            result.Message
        );

        Assert.Null(
            result.Content
        );

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 21
    //
    // Login thành công với Doctor role.
    // Expected:
    // - 200
    // - DoctorId được lấy
    // - PatientId = null
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturnDoctorId_WhenUserHasDoctorRole()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id = 21,
                Username = "0901234571",
                Email = "doctor@example.com",
                FullName = "Doctor Test",
                Phone = "0901234571",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "Password123!"
                    ),

                IsActive = true,
                IsDeleted = false
            };

        SetupLoginUserRepository(user);


        var doctorRole =
            new UserRole
            {
                UserId = user.Id,

                Role =
                    new Role
                    {
                        Name =
                            UserRoleConstant.Doctor
                    }
            };

        SetupUserRoleRepository(
            doctorRole
        );


        var doctorRepositoryMock =
            new Mock<IDoctorRepository>();

        doctorRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<Doctor, bool>
                            >
                        >()
                    )
            )
            .Returns(
                new List<Doctor>
                {
                new Doctor
                {
                    Id = 210,
                    UserId = user.Id
                }
                }
                .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x => x.DoctorRepository
            )
            .Returns(
                doctorRepositoryMock.Object
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateAccessToken(
                        user,
                        It.IsAny<List<string>>()
                    )
            )
            .Returns(
                "doctor-access-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateRefreshToken()
            )
            .Returns(
                "doctor-refresh-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "doctor-refresh-token"
                    )
            )
            .Returns(
                "doctor-refresh-hash"
            );


        var refreshTokenRepositoryMock =
            new Mock<IRefreshTokenRepository>();

        refreshTokenRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                refreshTokenRepositoryMock.Object
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // Act

        var result =
            await _service
                .LoginAsync(
                    new LoginRequestDTO
                    {
                        Username =
                            "0901234571",

                        Password =
                            "Password123!"
                    }
                );


        // Assert

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

        Assert.Equal(
            210,
            result.Content!.User.DoctorId
        );

        Assert.Null(
            result.Content.User.PatientId
        );

        Assert.Contains(
            UserRoleConstant.Doctor,
            result.Content.User.Roles
        );
    }


    // =====================================================
    // TEST 22
    //
    // Login thành công với Patient role.
    // Expected:
    // - 200
    // - PatientId được lấy
    // - DoctorId = null
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturnPatientId_WhenUserHasPatientRole()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id = 22,
                Username = "0901234572",
                Email = "patient@example.com",
                FullName = "Patient Test",
                Phone = "0901234572",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "Password123!"
                    ),

                IsActive = true,
                IsDeleted = false
            };

        SetupLoginUserRepository(user);


        var patientRole =
            new UserRole
            {
                UserId = user.Id,

                Role =
                    new Role
                    {
                        Name =
                            UserRoleConstant.Patient
                    }
            };

        SetupUserRoleRepository(
            patientRole
        );


        var patientRepositoryMock =
            new Mock<IPatientRepository>();

        patientRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<Patient, bool>
                            >
                        >()
                    )
            )
            .Returns(
                new List<Patient>
                {
                new Patient
                {
                    Id = 220,
                    UserId = user.Id
                }
                }
                .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x => x.PatientRepository
            )
            .Returns(
                patientRepositoryMock.Object
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateAccessToken(
                        user,
                        It.IsAny<List<string>>()
                    )
            )
            .Returns(
                "patient-access-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateRefreshToken()
            )
            .Returns(
                "patient-refresh-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "patient-refresh-token"
                    )
            )
            .Returns(
                "patient-refresh-hash"
            );


        var refreshTokenRepositoryMock =
            new Mock<IRefreshTokenRepository>();

        refreshTokenRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                refreshTokenRepositoryMock.Object
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // Act

        var result =
            await _service
                .LoginAsync(
                    new LoginRequestDTO
                    {
                        Username =
                            "0901234572",

                        Password =
                            "Password123!"
                    }
                );


        // Assert

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

        Assert.Equal(
            220,
            result.Content!.User.PatientId
        );

        Assert.Null(
            result.Content.User.DoctorId
        );

        Assert.Contains(
            UserRoleConstant.Patient,
            result.Content.User.Roles
        );
    }


    // =====================================================
    // TEST 23
    //
    // Login repository exception.
    //
    // Expected:
    // 500
    // LoginFailed
    // =====================================================

    [Fact]
    public async Task LoginAsync_ShouldReturn500_WhenRepositoryThrows()
    {
        // Arrange

        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<UserEntity, bool>
                            >
                        >()
                    )
            )
            .ThrowsAsync(
                new InvalidOperationException(
                    "Test exception"
                )
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .LoginAsync(
                    CreateValidLoginRequest()
                );


        // Assert

        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.LoginFailed,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 24
    //
    // Refresh Token thành công.
    //
    // Kiểm tra:
    // - Access token mới
    // - Refresh token mới
    // - Token cũ bị revoke
    // - Token mới được Add
    // - SaveChanges
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn200_WhenRefreshTokenIsValid()
    {
        // Arrange

        var request =
            CreateValidRefreshTokenRequest();


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        request.RefreshToken
                    )
            )
            .Returns(
                "old-refresh-hash"
            );


        var oldRefreshToken =
            new RefreshToken
            {
                UserId = 24,

                TokenHash =
                    "old-refresh-hash",

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt = null,

                CreatedAt =
                    DateTime.UtcNow.AddDays(-1)
            };

        SetupRefreshTokenRepository(
            oldRefreshToken
        );


        var user =
            new UserEntity
            {
                Id = 24,

                Username =
                    "0901234574",

                Email =
                    "refresh@example.com",

                FullName =
                    "Refresh User",

                Phone =
                    "0901234574",

                IsActive = true,

                IsDeleted = false
            };


        var userRepositoryMock =
            new Mock<IUserRepository>();

        userRepositoryMock
            .Setup(
                x =>
                    x.GetByIdAsync(24)
            )
            .ReturnsAsync(
                user
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        SetupUserRoleRepository();


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateAccessToken(
                        user,
                        It.IsAny<List<string>>()
                    )
            )
            .Returns(
                "new-access-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateRefreshToken()
            )
            .Returns(
                "new-refresh-token"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "new-refresh-token"
                    )
            )
            .Returns(
                "new-refresh-hash"
            );


        RefreshToken? addedToken =
            null;

        var refreshTokenRepositoryMock =
            new Mock<IRefreshTokenRepository>();

        refreshTokenRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    RefreshToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                oldRefreshToken
            );

        refreshTokenRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );

        refreshTokenRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Callback(
                (RefreshToken token) =>
                {
                    addedToken = token;
                }
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                refreshTokenRepositoryMock.Object
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // Act

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RefreshTokenSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );

        Assert.Equal(
            "new-access-token",
            result.Content!.AccessToken
        );

        Assert.Equal(
            "new-refresh-token",
            result.Content.RefreshToken
        );


        Assert.NotNull(
            oldRefreshToken.RevokedAt
        );


        Assert.NotNull(
            addedToken
        );

        Assert.Equal(
            24,
            addedToken!.UserId
        );

        Assert.Equal(
            "new-refresh-hash",
            addedToken.TokenHash
        );

        Assert.Null(
            addedToken.RevokedAt
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );
    }


    // =====================================================
    // TEST 25
    //
    // Refresh Token + Doctor role.
    //
    // Expected:
    // - 200
    // - DoctorId được trả về
    // - PatientId = null
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnDoctorId_WhenUserHasDoctorRole()
    {
        // Arrange

        var request =
            CreateValidRefreshTokenRequest();


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        request.RefreshToken
                    )
            )
            .Returns(
                "refresh-hash-25"
            );


        var oldToken =
            new RefreshToken
            {
                UserId =
                    25,

                TokenHash =
                    "refresh-hash-25",

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt =
                    null
            };


        // =================================================
        // REFRESH TOKEN REPOSITORY
        // =================================================

        var refreshRepositoryMock =
            new Mock<IRefreshTokenRepository>();


        refreshRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    RefreshToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                oldToken
            );


        refreshRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        refreshRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                refreshRepositoryMock.Object
            );


        // =================================================
        // USER
        // =================================================

        var user =
            new UserEntity
            {
                Id =
                    25,

                Username =
                    "0901234575",

                Email =
                    "doctor25@example.com",

                FullName =
                    "Doctor 25",

                Phone =
                    "0901234575",

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        var userRepositoryMock =
            new Mock<IUserRepository>();


        userRepositoryMock
            .Setup(
                x =>
                    x.GetByIdAsync(
                        25
                    )
            )
            .ReturnsAsync(
                user
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        // =================================================
        // USER ROLE
        // =================================================

        SetupUserRoleRepository(
            new UserRole
            {
                UserId =
                    25,

                Role =
                    new Role
                    {
                        Name =
                            UserRoleConstant.Doctor
                    }
            }
        );


        // =================================================
        // DOCTOR
        // =================================================

        var doctorRepositoryMock =
            new Mock<IDoctorRepository>();


        doctorRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Doctor,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                new List<Doctor>
                {
                new Doctor
                {
                    Id =
                        250,

                    UserId =
                        25
                }
                }
                .BuildMock()
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.DoctorRepository
            )
            .Returns(
                doctorRepositoryMock.Object
            );


        // =================================================
        // JWT
        // =================================================

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateAccessToken(
                        user,
                        It.IsAny<List<string>>()
                    )
            )
            .Returns(
                "access-25"
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateRefreshToken()
            )
            .Returns(
                "refresh-25"
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "refresh-25"
                    )
            )
            .Returns(
                "hash-25"
            );


        // =================================================
        // SAVE CHANGES
        // =================================================

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // =================================================
        // ACT
        // =================================================

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RefreshTokenSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );


        Assert.Equal(
            "access-25",
            result.Content!.AccessToken
        );

        Assert.Equal(
            "refresh-25",
            result.Content.RefreshToken
        );


        Assert.Equal(
            250,
            result.Content.User.DoctorId
        );

        Assert.Null(
            result.Content.User.PatientId
        );


        Assert.Contains(
            UserRoleConstant.Doctor,
            result.Content.User.Roles
        );


        Assert.NotNull(
            oldToken.RevokedAt
        );


        refreshRepositoryMock.Verify(
            x =>
                x.SingleOrDefault(
                    It.IsAny<
                        Expression<
                            Func<
                                RefreshToken,
                                bool
                            >
                        >
                    >()
                ),
            Times.Once
        );


        refreshRepositoryMock.Verify(
            x =>
                x.UpdateAsync(
                    oldToken
                ),
            Times.Once
        );


        refreshRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<RefreshToken>()
                ),
            Times.Once
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );
    }

    // =====================================================
    // TEST 26
    //
    // Refresh Token + Patient role.
    //
    // Expected:
    // - 200
    // - PatientId được trả về
    // - DoctorId = null
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnPatientId_WhenUserHasPatientRole()
    {
        // Arrange

        var request =
            CreateValidRefreshTokenRequest();


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        request.RefreshToken
                    )
            )
            .Returns(
                "refresh-hash-26"
            );


        var oldToken =
            new RefreshToken
            {
                UserId =
                    26,

                TokenHash =
                    "refresh-hash-26",

                ExpiresAt =
                    DateTime.UtcNow.AddDays(1),

                RevokedAt =
                    null
            };


        // =================================================
        // REFRESH TOKEN REPOSITORY
        // =================================================

        var refreshRepositoryMock =
            new Mock<IRefreshTokenRepository>();


        refreshRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    RefreshToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                oldToken
            );


        refreshRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        refreshRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<RefreshToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.RefreshTokenRepository
            )
            .Returns(
                refreshRepositoryMock.Object
            );


        // =================================================
        // USER
        // =================================================

        var user =
            new UserEntity
            {
                Id =
                    26,

                Username =
                    "0901234576",

                Email =
                    "patient26@example.com",

                FullName =
                    "Patient 26",

                Phone =
                    "0901234576",

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        var userRepositoryMock =
            new Mock<IUserRepository>();


        userRepositoryMock
            .Setup(
                x =>
                    x.GetByIdAsync(
                        26
                    )
            )
            .ReturnsAsync(
                user
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        // =================================================
        // USER ROLE
        // =================================================

        SetupUserRoleRepository(
            new UserRole
            {
                UserId =
                    26,

                Role =
                    new Role
                    {
                        Name =
                            UserRoleConstant.Patient
                    }
            }
        );


        // =================================================
        // PATIENT
        // =================================================

        var patientRepositoryMock =
            new Mock<IPatientRepository>();


        patientRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    Patient,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                new List<Patient>
                {
                new Patient
                {
                    Id =
                        260,

                    UserId =
                        26
                }
                }
                .BuildMock()
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PatientRepository
            )
            .Returns(
                patientRepositoryMock.Object
            );


        // =================================================
        // JWT
        // =================================================

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateAccessToken(
                        user,
                        It.IsAny<List<string>>()
                    )
            )
            .Returns(
                "access-26"
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateRefreshToken()
            )
            .Returns(
                "refresh-26"
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        "refresh-26"
                    )
            )
            .Returns(
                "hash-26"
            );


        // =================================================
        // SAVE CHANGES
        // =================================================

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // =================================================
        // ACT
        // =================================================

        var result =
            await _service
                .RefreshTokenAsync(
                    request
                );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RefreshTokenSuccess,
            result.Message
        );

        Assert.NotNull(
            result.Content
        );


        Assert.Equal(
            "access-26",
            result.Content!.AccessToken
        );

        Assert.Equal(
            "refresh-26",
            result.Content.RefreshToken
        );


        Assert.Equal(
            260,
            result.Content.User.PatientId
        );

        Assert.Null(
            result.Content.User.DoctorId
        );


        Assert.Contains(
            UserRoleConstant.Patient,
            result.Content.User.Roles
        );


        Assert.NotNull(
            oldToken.RevokedAt
        );


        refreshRepositoryMock.Verify(
            x =>
                x.SingleOrDefault(
                    It.IsAny<
                        Expression<
                            Func<
                                RefreshToken,
                                bool
                            >
                        >
                    >()
                ),
            Times.Once
        );


        refreshRepositoryMock.Verify(
            x =>
                x.UpdateAsync(
                    oldToken
                ),
            Times.Once
        );


        refreshRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<RefreshToken>()
                ),
            Times.Once
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );
    }
    // =====================================================
    // TEST 27
    //
    // Refresh Token exception.
    //
    // Expected:
    // 500
    // RefreshTokenFailed
    // =====================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturn500_WhenExceptionOccurs()
    {
        // Arrange

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashRefreshToken(
                        It.IsAny<string>()
                    )
            )
            .Throws(
                new InvalidOperationException(
                    "Test exception"
                )
            );


        // Act

        var result =
            await _service
                .RefreshTokenAsync(
                    CreateValidRefreshTokenRequest()
                );


        // Assert

        Assert.Equal(
            500,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.RefreshTokenFailed,
            result.Message
        );

        Assert.Null(
            result.Content
        );
    }


    // =====================================================
    // TEST 28
    //
    // Forgot Password thành công.
    // Không có OTP cũ.
    //
    // Expected:
    // 200
    // ForgotPasswordSuccess
    // =====================================================

    [Fact]
    public async Task ForgotPasswordAsync_ShouldReturn200_WhenRequestIsSuccessful()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id = 28,

                Email =
                    "forgot28@example.com",

                Username =
                    "0901234578",

                Phone =
                    "0901234578",

                FullName =
                    "Forgot User",

                IsActive = true,

                IsDeleted = false
            };

        SetupUserRepository(
            user
        );


        var tokenRepositoryMock =
            new Mock<IPasswordResetTokenRepository>();

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                Array.Empty<PasswordResetToken>()
                    .BuildMock()
            );


        PasswordResetToken? addedToken =
            null;

        tokenRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<PasswordResetToken>()
                    )
            )
            .Callback(
                (PasswordResetToken token) =>
                {
                    addedToken = token;
                }
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateResetOtp()
            )
            .Returns(
                "123456"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "otp-hash-28"
            );


        _emailServiceMock
        .Setup(
            x =>
                x.SendPasswordResetOtpAsync(
                    user.Email!,
                    "123456"
                )
        )
            .Returns(
                Task.CompletedTask
            );


        // Act

        var result =
            await _service
                .ForgotPasswordAsync(
                    CreateValidForgotPasswordRequest()
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.ForgotPasswordSuccess,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        Assert.NotNull(
            addedToken
        );

        Assert.Equal(
            28,
            addedToken!.UserId
        );

        Assert.Equal(
            "otp-hash-28",
            addedToken.TokenHash
        );

        Assert.Null(
            addedToken.UsedAt
        );

        Assert.Null(
            addedToken.RevokedAt
        );


        _emailServiceMock.Verify(
        x =>
            x.SendPasswordResetOtpAsync(
                user.Email!,
                "123456"
            ),
        Times.Once
    );

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );
    }


    // =====================================================
    // TEST 29
    //
    // Forgot Password thành công.
    // Có OTP cũ đang active.
    //
    // Expected:
    // - OTP cũ bị revoke
    // - OTP mới được tạo
    // - 200
    // =====================================================

    [Fact]
    public async Task ForgotPasswordAsync_ShouldRevokeActiveTokens_WhenRequestIsSuccessful()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id = 29,

                Email =
                    "forgot29@example.com",

                Username =
                    "0901234579",

                Phone =
                    "0901234579",

                FullName =
                    "Forgot User 29",

                IsActive = true,

                IsDeleted = false
            };

        SetupUserRepository(
            user
        );


        var oldToken =
            new PasswordResetToken
            {
                UserId = 29,

                TokenHash =
                    "old-otp-hash",

                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-5),

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),

                UsedAt = null,

                RevokedAt = null
            };


        PasswordResetToken? addedToken =
            null;

        var tokenRepositoryMock =
            new Mock<IPasswordResetTokenRepository>();

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                new List<PasswordResetToken>
                {
                oldToken
                }
                .BuildMock()
            );


        tokenRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<PasswordResetToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        tokenRepositoryMock
            .Setup(
                x =>
                    x.AddAsync(
                        It.IsAny<PasswordResetToken>()
                    )
            )
            .Callback(
                (PasswordResetToken token) =>
                {
                    addedToken = token;
                }
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.GenerateResetOtp()
            )
            .Returns(
                "654321"
            );

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "654321"
                    )
            )
            .Returns(
                "new-otp-hash-29"
            );


        _emailServiceMock
        .Setup(
            x =>
                x.SendPasswordResetOtpAsync(
                    user.Email!,
                    "654321"
                )
        );


        // Act

        var result =
            await _service
                .ForgotPasswordAsync(
                    new ForgotPasswordRequestDTO
                    {
                        Email =
                            "  FORGOT29@EXAMPLE.COM  "
                    }
                );


        // Assert

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.ForgotPasswordSuccess,
            result.Message
        );


        Assert.NotNull(
            oldToken.RevokedAt
        );


        Assert.NotNull(
            addedToken
        );

        Assert.Equal(
            29,
            addedToken!.UserId
        );

        Assert.Equal(
            "new-otp-hash-29",
            addedToken.TokenHash
        );


        tokenRepositoryMock.Verify(
            x =>
                x.UpdateAsync(
                    oldToken
                ),
            Times.Once
        );

        tokenRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<PasswordResetToken>()
                ),
            Times.Once
        );
    }


    // =====================================================
    // TEST 30
    //
    // Reset Password.
    // OTP không hợp lệ.
    //
    // Expected:
    // 400
    // InvalidResetOtp
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn400_WhenResetOtpIsInvalid()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id = 30,

                Email =
                    "reset30@example.com",

                Username =
                    "0901234580",

                Phone =
                    "0901234580",

                FullName =
                    "Reset User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "OldPassword123!"
                    ),

                IsActive = true,

                IsDeleted = false
            };

        SetupUserRepository(
            user
        );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "invalid-otp-hash"
            );


        var tokenRepositoryMock =
            new Mock<IPasswordResetTokenRepository>();

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                Array.Empty<PasswordResetToken>()
                    .BuildMock()
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    CreateValidResetPasswordRequest()
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidResetOtp,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }

    // =====================================================
    // TEST 31
    //
    // Reset Password
    //
    // New password > 72 UTF-8 bytes.
    //
    // Expected:
    // 400
    // PasswordTooLong
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn400_WhenNewPasswordExceeds72Bytes()
    {
        // Arrange

        var request =
            CreateValidResetPasswordRequest();

        request.NewPassword =
            new string(
                'a',
                73
            );


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    request
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.PasswordTooLong,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 32
    //
    // Reset Password
    //
    // OTP đã hết hạn.
    //
    // Expected:
    // 400
    // InvalidResetOtp
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn400_WhenOtpIsExpired()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id =
                    32,

                Email =
                    "test@example.com",

                Username =
                    "0901234532",

                Phone =
                    "0901234532",

                FullName =
                    "Expired OTP User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "OldPassword123!"
                    ),

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        SetupUserRepository(
            user
        );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "expired-otp-hash"
            );


        var expiredToken =
            new PasswordResetToken
            {
                UserId =
                    32,

                TokenHash =
                    "expired-otp-hash",

                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-10),

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(-1),

                UsedAt =
                    null,

                RevokedAt =
                    null
            };


        var tokenRepositoryMock =
    new Mock<IPasswordResetTokenRepository>();

        var tokens =
            new List<PasswordResetToken>
            {
        expiredToken
            };

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            PasswordResetToken,
                            bool
                        >
                    > predicate
                ) =>
                    tokens
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    CreateValidResetPasswordRequest()
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidResetOtp,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 33
    //
    // Reset Password
    //
    // OTP đã được sử dụng.
    //
    // Expected:
    // 400
    // InvalidResetOtp
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn400_WhenOtpWasAlreadyUsed()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id =
                    33,

                Email =
                    "test@example.com",

                Username =
                    "0901234533",

                Phone =
                    "0901234533",

                FullName =
                    "Used OTP User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "OldPassword123!"
                    ),

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        SetupUserRepository(
            user
        );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "used-otp-hash"
            );


        var usedToken =
            new PasswordResetToken
            {
                UserId =
                    33,

                TokenHash =
                    "used-otp-hash",

                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-5),

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),

                UsedAt =
                    DateTime.UtcNow.AddMinutes(-1),

                RevokedAt =
                    null
            };


        var tokenRepositoryMock =
    new Mock<IPasswordResetTokenRepository>();

        var tokens =
            new List<PasswordResetToken>
            {
        usedToken
            };

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            PasswordResetToken,
                            bool
                        >
                    > predicate
                ) =>
                    tokens
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    CreateValidResetPasswordRequest()
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidResetOtp,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 34
    //
    // Reset Password
    //
    // OTP đã bị revoke.
    //
    // Expected:
    // 400
    // InvalidResetOtp
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn400_WhenOtpWasRevoked()
    {
        // Arrange

        var user =
            new UserEntity
            {
                Id =
                    34,

                Email =
                    "test@example.com",

                Username =
                    "0901234534",

                Phone =
                    "0901234534",

                FullName =
                    "Revoked OTP User",

                PasswordHash =
                    HelperFunction.HashPassword(
                        "OldPassword123!"
                    ),

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        SetupUserRepository(
            user
        );


        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "revoked-otp-hash"
            );


        var revokedToken =
            new PasswordResetToken
            {
                UserId =
                    34,

                TokenHash =
                    "revoked-otp-hash",

                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-5),

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),

                UsedAt =
                    null,

                RevokedAt =
                    DateTime.UtcNow.AddMinutes(-1)
            };


        var tokenRepositoryMock =
    new Mock<IPasswordResetTokenRepository>();

        var tokens =
            new List<PasswordResetToken>
            {
        revokedToken
            };

        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                (
                    Expression<
                        Func<
                            PasswordResetToken,
                            bool
                        >
                    > predicate
                ) =>
                    tokens
                        .Where(
                            predicate.Compile()
                        )
                        .ToList()
                        .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        // Act

        var result =
            await _service
                .ResetPasswordAsync(
                    CreateValidResetPasswordRequest()
                );


        // Assert

        Assert.Equal(
            400,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.InvalidResetOtp,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never
        );
    }


    // =====================================================
    // TEST 35
    //
    // Reset Password thành công.
    //
    // Expected:
    // 200
    // ResetPasswordSuccess
    //
    // Kiểm tra:
    // - password được đổi
    // - OTP được mark UsedAt
    // - UserRepository.UpdateAsync
    // - PasswordResetTokenRepository.UpdateAsync
    // - SaveChangesAsync
    // =====================================================

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturn200_WhenOtpIsValid()
    {
        // Arrange

        var oldPasswordHash =
            HelperFunction.HashPassword(
                "OldPassword123!"
            );


        var user =
            new UserEntity
            {
                Id =
                    35,

                Email =
                    "test@example.com",

                Username =
                    "0901234535",

                Phone =
                    "0901234535",

                FullName =
                    "Reset Success User",

                PasswordHash =
                    oldPasswordHash,

                IsActive =
                    true,

                IsDeleted =
                    false
            };


        // =================================================
        // USER REPOSITORY
        // =================================================

        var userRepositoryMock =
            new Mock<IUserRepository>();


        userRepositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    UserEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                user
            );


        userRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<UserEntity>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                userRepositoryMock.Object
            );


        // =================================================
        // OTP
        // =================================================

        _jwtAuthServiceMock
            .Setup(
                x =>
                    x.HashResetOtp(
                        "123456"
                    )
            )
            .Returns(
                "valid-otp-hash"
            );


        var resetToken =
            new PasswordResetToken
            {
                UserId =
                    35,

                TokenHash =
                    "valid-otp-hash",

                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-2),

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),

                UsedAt =
                    null,

                RevokedAt =
                    null
            };


        // =================================================
        // PASSWORD RESET TOKEN REPOSITORY
        // =================================================

        var tokenRepositoryMock =
            new Mock<IPasswordResetTokenRepository>();


        tokenRepositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    PasswordResetToken,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                new List<PasswordResetToken>
                {
                resetToken
                }
                .BuildMock()
            );


        tokenRepositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<PasswordResetToken>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );


        _unitOfWorkMock
            .Setup(
                x =>
                    x.PasswordResetTokenRepository
            )
            .Returns(
                tokenRepositoryMock.Object
            );


        // =================================================
        // SAVE
        // =================================================

        _unitOfWorkMock
            .Setup(
                x =>
                    x.SaveChangesAsync()
            )
            .Returns(
                Task.CompletedTask
            );


        // =================================================
        // REQUEST
        // =================================================

        var request =
            new ResetPasswordRequestDTO
            {
                Email =
                    " TEST@EXAMPLE.COM ",

                Otp =
                    " 123456 ",

                NewPassword =
                    "NewPassword123!"
            };


        // =================================================
        // ACT
        // =================================================

        var result =
            await _service
                .ResetPasswordAsync(
                    request
                );


        // =================================================
        // ASSERT
        // =================================================

        Assert.Equal(
            200,
            result.StatusCode
        );

        Assert.Equal(
            UserResponseMessageDTO.ResetPasswordSuccess,
            result.Message
        );

        Assert.Null(
            result.Content
        );


        // Password phải được đổi

        Assert.NotEqual(
            oldPasswordHash,
            user.PasswordHash
        );


        Assert.True(
            HelperFunction.VerifyPassword(
                "NewPassword123!",
                user.PasswordHash
            )
        );


        // UpdatedAt phải được set

        Assert.NotNull(
            user.UpdatedAt
        );


        // OTP phải được mark Used

        Assert.NotNull(
            resetToken.UsedAt
        );


        // User update

        userRepositoryMock.Verify(
            x =>
                x.UpdateAsync(
                    user
                ),
            Times.Once
        );


        // OTP update

        tokenRepositoryMock.Verify(
            x =>
                x.UpdateAsync(
                    resetToken
                ),
            Times.Once
        );


        // Save

        _unitOfWorkMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once
        );
    }



    // --help
    private void SetupLoginUserRepository(
        UserEntity user)
    {
        var repositoryMock =
            new Mock<IUserRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.SingleOrDefault(
                        It.IsAny<
                            Expression<
                                Func<
                                    UserEntity,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .ReturnsAsync(
                user
            );

        repositoryMock
            .Setup(
                x =>
                    x.UpdateAsync(
                        It.IsAny<UserEntity>()
                    )
            )
            .Returns(
                Task.CompletedTask
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }


    private void SetupUserRoleRepository(
        params UserRole[] userRoles)
    {
        var repositoryMock =
            new Mock<IUserRoleRepository>();

        repositoryMock
            .Setup(
                x =>
                    x.WhereSql(
                        It.IsAny<
                            Expression<
                                Func<
                                    UserRole,
                                    bool
                                >
                            >
                        >()
                    )
            )
            .Returns(
                userRoles
                    .ToList()
                    .BuildMock()
            );

        _unitOfWorkMock
            .Setup(
                x =>
                    x.UserRoleRepository
            )
            .Returns(
                repositoryMock.Object
            );
    }

}