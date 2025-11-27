using Microsoft.EntityFrameworkCore;
using OfficeCalendar.Models;
using OfficeCalendar.Utils;

namespace OfficeCalendar.Services;

public class MessageService : IMessageService
{
    private readonly DatabaseContext _context;

    public MessageService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Message>> GetMessagesByUserId(int userId)
    {
        return await _context.Message
            .Where(m => m.FromUserId == userId || m.ToUserId == userId)
            .ToListAsync();
    }

    public async Task<bool> CreateMessage(Message message, int toId, int currentId)
    {
        if (!_context.User.Any(u => u.UserId == toId)) return false;

        DateTime now = DateTime.Now;
        DateTime formattedTime = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second
        );

        message.FromUserId = currentId;
        message.ToUserId = toId;
        message.Date = formattedTime;
        message.BeenRead = false;

        _context.Message.Add(message);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MessageRead(int userId, int messageId)
    {
        var message = await _context.Message.FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (message == null || message.BeenRead || message.ToUserId != userId) return false;

        message.BeenRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
