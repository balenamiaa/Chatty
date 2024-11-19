using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Users;

namespace Chatty.Backend.Services.Users;

public interface IUserService : IService
{
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Result<UserDto>> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<Result<bool>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
        CancellationToken ct = default);

    Task<Result<bool>> RequestPasswordResetAsync(string email, CancellationToken ct = default);

    Task<Result<bool>> ResetPasswordAsync(string email, string token, string newPassword,
        CancellationToken ct = default);
}
