using Microsoft.AspNetCore.SignalR;

namespace GoKidAPI.Hubs
{
    public class NotificationHub : Hub
    {
        // كل user بيتجمع في group باسم userId بتاعه
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
}
