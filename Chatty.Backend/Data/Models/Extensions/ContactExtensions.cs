using Chatty.Shared.Models.Contacts;



namespace Chatty.Backend.Data.Models.Extensions;



public static class ContactExtensions

{

    public static ContactDto ToDto(this Contact contact) => new(

        Id: contact.Id,

        User: contact.User.ToDto(),

        ContactUser: contact.ContactUser.ToDto(),

        Status: contact.Status,

        AddedAt: contact.AddedAt);

}
