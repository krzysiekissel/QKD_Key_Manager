//MessageBus.cs
using QKD_Key_Manager.Entities;
using QKD_Key_Manager.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace QKD_Key_Manager.Messaging
{
    abstract class MessageBus
    {
        public int CascadeMessages { get; set; } = 0;
        public int PublicBits { get; set; } = 0;
        public int MessagesLength { get; set; } = 0;
        public abstract Entity MatchReceiver(Entity sender);

        public void ProcessResponse(Entity sender,Message response)
        {
            if (response is NOP)
                return;
            if (response is OfflineMessage)
                return;
            else
            {
                var receiver = MatchReceiver(sender);
                Send(receiver, response);
            }
        }
        public void Send(Entity entity,Message message)
        {
            var response = entity.Receive(message);
            PrintLog(entity, message, response);
            ProcessResponse(entity, response);
        }
        public Message SendRequest(Service service,Message message)
        {
            CascadeMessages += 2;
            MessagesLength += (message as AuthenticatedMessage).Payload.GetBytes().Length;
            var response = service.Receive(message);
            PrintLog(service, message,response);
            if (MatchReceiver(service).MessageAuth(response as AuthenticatedMessage))
            {
                return response;
            }
            throw new UnauthorizedMessageException();
            
        }

        private void PrintLog(Entity entity, Message message, Message response)
        {
            Console.WriteLine("==================");
            Console.WriteLine($"{entity.GetType()} received: {message.GetType()} and responded: {response.GetType()}");
            entity.PrintStatus();
        }
    }
    class ClientServerMessageBus : MessageBus
    {
        
        public Initiator Initiator { get; set; }
        public Service Service { get; set; }
        public override Entity MatchReceiver(Entity sender)
        {
            if (sender is Initiator)
                return Service;
            else
                return Initiator;
        }
    }
}
