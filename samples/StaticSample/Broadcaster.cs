using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace StaticSample
{
    public class Broadcaster : Hub
    {
        public Task Broadcast(string sender, string message) =>
            Clients.Others.SendAsync("Message", sender, message);
    }
}
