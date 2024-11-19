using Chatty.Shared.Models.Contacts;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ContactExtensions

{
    public static ContactDto ToDto(this Contact contact) => new(
        contact.Id,
        contact.User.ToDto(),
        contact.ContactUser.ToDto(),
        contact.Status,
        contact.AddedAt);
}
