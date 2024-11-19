using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Contacts;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Contacts;
using Chatty.Shared.Models.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Services.Contacts;

public sealed class ContactServiceTests : IDisposable
{
    private readonly ChattyDbContext _context;
    private readonly Mock<IEventBus> _eventBus;
    private readonly ContactService _sut;

    public ContactServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _eventBus = new Mock<IEventBus>();
        var logger = Mock.Of<ILogger<ContactService>>();

        // Add test users
        _context.Users.AddRange(TestData.Users.User1, TestData.Users.User2);
        _context.SaveChanges();

        _sut = new ContactService(_context, logger, _eventBus.Object);
    }

    public void Dispose() => TestDbContextFactory.Destroy(_context);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesContact()
    {
        // Arrange
        var request = new CreateContactRequest(TestData.Users.User2.Id);

        // Act
        var result = await _sut.CreateAsync(TestData.Users.User1.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TestData.Users.User1.Id, result.Value.User.Id);
        Assert.Equal(TestData.Users.User2.Id, result.Value.ContactUser.Id);
        Assert.Equal(ContactStatus.Pending, result.Value.Status);

        // Verify reciprocal contact was created
        var reciprocalContact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == TestData.Users.User2.Id &&
                c.ContactUserId == TestData.Users.User1.Id);
        Assert.NotNull(reciprocalContact);
        Assert.Equal(ContactStatus.Pending, reciprocalContact.Status);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<ContactRequestEvent>(e =>
                e.RequesterId == TestData.Users.User1.Id &&
                e.ContactUserId == TestData.Users.User2.Id &&
                e.Contact.Status == ContactStatus.Pending),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateContactRequest(Guid.NewGuid());

        // Act
        var result = await _sut.CreateAsync(TestData.Users.User1.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contact user not found", result.Error.Message);
    }

    [Fact]
    public async Task CreateAsync_WithExistingContact_ReturnsConflict()
    {
        // Arrange
        var existingContact = new Contact
        {
            UserId = TestData.Users.User1.Id,
            ContactUserId = TestData.Users.User2.Id,
            Status = ContactStatus.Pending
        };
        _context.Contacts.Add(existingContact);
        await _context.SaveChangesAsync();

        var request = new CreateContactRequest(TestData.Users.User2.Id);

        // Act
        var result = await _sut.CreateAsync(TestData.Users.User1.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contact already exists", result.Error.Message);
    }

    [Fact]
    public async Task AcceptAsync_WithValidRequest_AcceptsContact()
    {
        // Arrange
        var contact = new Contact
        {
            UserId = TestData.Users.User1.Id,
            ContactUserId = TestData.Users.User2.Id,
            Status = ContactStatus.Pending
        };
        var reciprocalContact = new Contact
        {
            UserId = TestData.Users.User2.Id,
            ContactUserId = TestData.Users.User1.Id,
            Status = ContactStatus.Pending
        };
        _context.Contacts.AddRange(contact, reciprocalContact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.AcceptAsync(TestData.Users.User2.Id, contact.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify both contacts were updated
        var updatedContact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == TestData.Users.User1.Id &&
                c.ContactUserId == TestData.Users.User2.Id);
        Assert.NotNull(updatedContact);
        Assert.Equal(ContactStatus.Accepted, updatedContact.Status);

        var updatedReciprocalContact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == TestData.Users.User2.Id &&
                c.ContactUserId == TestData.Users.User1.Id);
        Assert.NotNull(updatedReciprocalContact);
        Assert.Equal(ContactStatus.Accepted, updatedReciprocalContact.Status);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<ContactRequestEvent>(e =>
                e.RequesterId == TestData.Users.User2.Id &&
                e.ContactUserId == TestData.Users.User1.Id &&
                e.Contact.Status == ContactStatus.Accepted),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AcceptAsync_WithNonexistentContact_ReturnsNotFound()
    {
        // Act
        var result = await _sut.AcceptAsync(TestData.Users.User1.Id, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contact not found", result.Error.Message);
    }

    [Fact]
    public async Task BlockAsync_WithValidRequest_BlocksContact()
    {
        // Arrange
        var contact = new Contact
        {
            UserId = TestData.Users.User1.Id,
            ContactUserId = TestData.Users.User2.Id,
            Status = ContactStatus.Accepted
        };
        var reciprocalContact = new Contact
        {
            UserId = TestData.Users.User2.Id,
            ContactUserId = TestData.Users.User1.Id,
            Status = ContactStatus.Accepted
        };
        _context.Contacts.AddRange(contact, reciprocalContact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.BlockAsync(TestData.Users.User1.Id, contact.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify both contacts were updated
        var updatedContact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == TestData.Users.User1.Id &&
                c.ContactUserId == TestData.Users.User2.Id);
        Assert.NotNull(updatedContact);
        Assert.Equal(ContactStatus.Blocked, updatedContact.Status);

        var updatedReciprocalContact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.UserId == TestData.Users.User2.Id &&
                c.ContactUserId == TestData.Users.User1.Id);
        Assert.NotNull(updatedReciprocalContact);
        Assert.Equal(ContactStatus.Blocked, updatedReciprocalContact.Status);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<ContactRequestEvent>(e =>
                e.RequesterId == TestData.Users.User1.Id &&
                e.ContactUserId == TestData.Users.User2.Id &&
                e.Contact.Status == ContactStatus.Blocked),
            It.IsAny<CancellationToken>()));
    }
}
