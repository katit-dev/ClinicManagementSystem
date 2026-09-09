using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

namespace ClinicManagementSystem.Application.Services;

public interface IUserService
{
    Task<HttpResponseData<object?>> RegisterUserAsync(
        UserRegisterDTO request);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<HttpResponseData<object?>> RegisterUserAsync(
        UserRegisterDTO request)
    {
        // Nghiệp vụ Register sẽ được triển khai ở bước tiếp theo.
        throw new NotImplementedException();
    }
}