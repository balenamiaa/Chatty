using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Contacts;

namespace Chatty.Backend.Services.Contacts;

public interface IContactService : IService
{
    Task<Result<ContactDto>> CreateAsync(Guid userId, CreateContactRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<Result<bool>> AcceptAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<Result<bool>> BlockAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<Result<bool>> UnblockAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContactDto>>> GetContactsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContactDto>>> GetPendingAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContactDto>>> GetBlockedAsync(Guid userId, CancellationToken ct = default);
}