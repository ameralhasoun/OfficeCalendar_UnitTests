using OfficeCalendar.Models;

namespace OfficeCalendar.Services;

public interface IMessageService
{
    public Task<List<Message>> GetMessagesByUserId(int userId);
    public Task<bool> CreateMessage(Message mes, int toId, int currentId);
    public Task<bool> MessageRead(int userId, int messageId);
}
