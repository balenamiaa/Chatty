using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Contacts;
using Chatty.Shared.Models.Enums;

using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Contacts;

public sealed class ContactService(
    ChattyDbContext context,
    ILogger<ContactService> logger,
    IEventBus eventBus)
    : IContactService
{
    public async Task<Result<ContactDto>> CreateAsync(
        Guid userId,
        CreateContactRequest request,
        CancellationToken ct = default)
    {
        // Check if contact exists
        var contactUser = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ContactUserId, ct);

        if (contactUser is null)
            return Result<ContactDto>.Failure(Error.NotFound("Contact user not found"));

        // Check if contact already exists
        var existingContact = await context.Contacts
            .FirstOrDefaultAsync(c =>
                (c.UserId == userId && c.ContactUserId == request.ContactUserId) ||
                (c.UserId == request.ContactUserId && c.ContactUserId == userId), ct);

        if (existingContact is not null)
            return Result<ContactDto>.Failure(Error.Conflict("Contact already exists"));

        try
        {
            // Create contact request
            var contact = new Contact
            {
                UserId = userId,
                ContactUserId = request.ContactUserId,
                Status = ContactStatus.Pending
            };

            // Create reciprocal contact
            var reciprocalContact = new Contact
            {
                UserId = request.ContactUserId,
                ContactUserId = userId,
                Status = ContactStatus.Pending
            };

            context.Contacts.Add(contact);
            context.Contacts.Add(reciprocalContact);
            await context.SaveChangesAsync(ct);

            // Load relationships for DTO
            await context.Entry(contact)
                .Reference(c => c.User)
                .LoadAsync(ct);

            await context.Entry(contact)
                .Reference(c => c.ContactUser)
                .LoadAsync(ct);

            var contactDto = contact.ToDto();

            // Publish contact request event
            await eventBus.PublishAsync(
                new ContactRequestEvent(userId, request.ContactUserId, contactDto),
                ct);

            return Result<ContactDto>.Success(contactDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create contact between {UserId} and {ContactUserId}",
                userId, request.ContactUserId);
            return Result<ContactDto>.Failure(Error.Internal("Failed to create contact"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct = default)
    {
        var contact = await context.Contacts
            .Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId, ct);

        if (contact is null)
            return Result<bool>.Success(true); // Already deleted

        try
        {
            // Delete reciprocal contact
            var reciprocalContact = await context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.UserId == contact.ContactUserId &&
                    c.ContactUserId == userId, ct);

            if (reciprocalContact is not null)
            {
                context.Contacts.Remove(reciprocalContact);
            }

            context.Contacts.Remove(contact);
            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete contact {ContactId} for user {UserId}", contactId, userId);
            return Result<bool>.Failure(Error.Internal("Failed to delete contact"));
        }
    }

    public async Task<Result<bool>> AcceptAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct = default)
    {
        var contact = await context.Contacts
            .Include(c => c.User)
            .Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.Id == contactId, ct);

        if (contact is null)
            return Result<bool>.Failure(Error.NotFound("Contact not found"));

        // Verify the accepting user is the contact user
        if (contact.ContactUserId != userId)
            return Result<bool>.Failure(Error.Unauthorized("Only the contact recipient can accept the request"));

        if (contact.Status != ContactStatus.Pending)
            return Result<bool>.Failure(Error.Validation("Contact is not pending"));

        try
        {
            // Update both contacts to accepted
            contact.Status = ContactStatus.Accepted;

            var reciprocalContact = await context.Contacts
                .Include(c => c.User)
                .Include(c => c.ContactUser)
                .FirstOrDefaultAsync(c =>
                    c.UserId == contact.ContactUserId &&
                    c.ContactUserId == contact.UserId, ct);

            if (reciprocalContact is not null)
            {
                reciprocalContact.Status = ContactStatus.Accepted;
            }

            await context.SaveChangesAsync(ct);

            // Publish contact accepted event
            await eventBus.PublishAsync(
                new ContactRequestEvent(userId, contact.UserId, reciprocalContact?.ToDto() ?? contact.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to accept contact {ContactId} for user {UserId}", contactId, userId);
            return Result<bool>.Failure(Error.Internal("Failed to accept contact"));
        }
    }

    public async Task<Result<bool>> BlockAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct = default)
    {
        var contact = await context.Contacts
            .Include(c => c.User)
            .Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId, ct);

        if (contact is null)
            return Result<bool>.Failure(Error.NotFound("Contact not found"));

        try
        {
            // Update both contacts to blocked
            contact.Status = ContactStatus.Blocked;

            var reciprocalContact = await context.Contacts
                .Include(c => c.User)
                .Include(c => c.ContactUser)
                .FirstOrDefaultAsync(c =>
                    c.UserId == contact.ContactUserId &&
                    c.ContactUserId == userId, ct);

            if (reciprocalContact is not null)
            {
                reciprocalContact.Status = ContactStatus.Blocked;
            }

            await context.SaveChangesAsync(ct);

            // Publish contact blocked event
            await eventBus.PublishAsync(
                new ContactRequestEvent(userId, contact.ContactUserId, contact.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to block contact {ContactId} for user {UserId}", contactId, userId);
            return Result<bool>.Failure(Error.Internal("Failed to block contact"));
        }
    }

    public async Task<Result<bool>> UnblockAsync(
        Guid userId,
        Guid contactId,
        CancellationToken ct = default)
    {
        var contact = await context.Contacts
            .Include(c => c.ContactUser)
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId, ct);

        if (contact is null)
            return Result<bool>.Failure(Error.NotFound("Contact not found"));

        if (contact.Status != ContactStatus.Blocked)
            return Result<bool>.Failure(Error.Validation("Contact is not blocked"));

        try
        {
            contact.Status = ContactStatus.Accepted;
            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unblock contact {ContactId} for user {UserId}", contactId, userId);
            return Result<bool>.Failure(Error.Internal("Failed to unblock contact"));
        }
    }

    public async Task<Result<IReadOnlyList<ContactDto>>> GetContactsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var contacts = await context.Contacts
            .Include(c => c.User)
            .Include(c => c.ContactUser)
            .Where(c => c.UserId == userId && c.Status == ContactStatus.Accepted)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ContactDto>>.Success(
            contacts.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<ContactDto>>> GetPendingAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var contacts = await context.Contacts
            .Include(c => c.User)
            .Include(c => c.ContactUser)
            .Where(c => c.UserId == userId && c.Status == ContactStatus.Pending)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ContactDto>>.Success(
            contacts.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<ContactDto>>> GetBlockedAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var contacts = await context.Contacts
            .Include(c => c.User)
            .Include(c => c.ContactUser)
            .Where(c => c.UserId == userId && c.Status == ContactStatus.Blocked)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ContactDto>>.Success(
            contacts.Select(c => c.ToDto()).ToList());
    }
}
