using Microsoft.EntityFrameworkCore;
using OfficeCalendar.Models;
using OfficeCalendar.Services;


namespace Tests
{
    [TestClass]
    public class MessageServiceTests
    {
        private DatabaseContext _context = null!;
        private MessageService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique DB each test
                .Options;

            _context = new DatabaseContext(options);
            _context.Database.EnsureCreated();

            _service = new MessageService(_context);

            // Add new test users (IDs 5 and 6)
            _context.User.AddRange(
                new User
                {
                    UserId = 5,
                    FirstName = "Test",
                    LastName = "Sender",
                    Email = "sender@test.com",
                    Password = "pass",
                    RecuringDays = "mo,tu,we"
                },
                new User
                {
                    UserId = 6,
                    FirstName = "Test",
                    LastName = "Receiver",
                    Email = "receiver@test.com",
                    Password = "pass",
                    RecuringDays = "mo,tu,we"
                }
            );
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Test1()
        {
            // Arrange
            _context.Message.AddRange(
                new Message
                {
                    Content = "Hello Receiver",
                    FromUserId = 5,
                    ToUserId = 6,
                    Date = DateTime.Now,
                    BeenRead = false
                },
                new Message
                {
                    Content = "Reply to Sender",
                    FromUserId = 6,
                    ToUserId = 5,
                    Date = DateTime.Now,
                    BeenRead = false
                },
                new Message
                {
                    Content = "Other user message",
                    FromUserId = 7, // unrelated message
                    ToUserId = 8,
                    Date = DateTime.Now,
                    BeenRead = false
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var messagesForSender = await _service.GetMessagesByUserId(5);
            var messagesForReceiver = await _service.GetMessagesByUserId(6);

            // Assert
            Assert.AreEqual(2, messagesForSender.Count, "User 5 should have 2 messages (sent and received).");
            Assert.AreEqual(2, messagesForReceiver.Count, "User 6 should have 2 messages (sent and received).");
        }

        [TestMethod]
        public async Task GetMessagesByUserId_ShouldReturnEmptyList_WhenUserHasNoMessages()
        {
            // Arrange
            int userWithoutMessages = 9;

            // Act
            var messages = await _service.GetMessagesByUserId(userWithoutMessages);

            // Assert
            Assert.AreEqual(0, messages.Count, "User without messages should return an empty list.");
        }

        [TestMethod]
        public async Task CreateMessage_ShouldReturnTrue_WhenValid()
        {
            // Arrange
            var message = new Message { Content = "Test message" };

            // Act
            bool result = await _service.CreateMessage(message, toId: 6, currentId: 5);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(4, _context.Message.Count());
            Assert.AreEqual("Test message", _context.Message.Last().Content);
        }

        [TestMethod]
        public async Task CreateMessage_ShouldReturnFalse_WhenRecipientDoesNotExist()
        {
            // Arrange
            var message = new Message { Content = "Invalid recipient" };

            // Act
            bool result = await _service.CreateMessage(message, toId: 999, currentId: 5);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(3, _context.Message.Count());
        }

        [TestMethod]
        public async Task MessageRead_ShouldMarkMessageAsRead_WhenValid()
        {
            // Arrange
            var message = new Message
            {
                Content = "Please read",
                FromUserId = 5,
                ToUserId = 6,
                Date = DateTime.Now,
                BeenRead = false
            };
            _context.Message.Add(message);
            _context.SaveChanges();

            // Act
            bool result = await _service.MessageRead(6, message.MessageId);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(_context.Message.First().BeenRead);
        }

        [TestMethod]
        public async Task MessageRead_ShouldReturnFalse_WhenUserIsNotRecipient()
        {
            // Arrange
            var message = new Message
            {
                Content = "Not your message",
                FromUserId = 5,
                ToUserId = 6,
                Date = DateTime.Now,
                BeenRead = false
            };
            _context.Message.Add(message);
            _context.SaveChanges();

            // Act
            bool result = await _service.MessageRead(5, message.MessageId); // wrong user tries to read

            // Assert
            Assert.IsFalse(result);
        }
    }
}
