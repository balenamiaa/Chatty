using Chatty.Shared.Models.Contacts;

namespace Chatty.Backend.Realtime.Events;

public sealed record ContactRequestEvent(
    Guid RequesterId,
    Guid ContactUserId,
    ContactDto Contact);
