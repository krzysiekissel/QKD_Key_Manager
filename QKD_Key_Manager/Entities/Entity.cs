//Entity.cs
using QKD_Key_Manager.Auth;
using QKD_Key_Manager.Exceptions;
using QKD_Key_Manager.KeyManagment;
using QKD_Key_Manager.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.Entities
{
    abstract class Entity
    {
        public ServiceKeyBuffer LocalKeyStore { get; set; }
        public CommonKeyStore CommonKeyStore { get; set; }
        public DestilationBuffer DestilationBuffer { get; set; }
        public IMessageAuthenticator MessageAuthenticator { get; set; }

        public bool MessageAuth(AuthenticatedMessage authenticatedMessage)
        {
            return IsOk(authenticatedMessage);
        }

        public Message Receive(Message message)
        {
            if(message is AuthenticatedMessage)
            {
                if (IsOk(message as AuthenticatedMessage))
                {
                    return ReceiveAuthenticatedMessage(message as AuthenticatedMessage);
                }
                else
                {
                    throw new UnauthorizedMessageException();
                }
            }
            if(message is OfflineMessage)
            {
                return ReceiveOfflineMessage(message as OfflineMessage);
            }
            return RecieveOtherMessage(message);

        }
        private bool IsOk(AuthenticatedMessage authenticatedMessage)
        {
            var messageMAC = MessageAuthenticator.GetMAC(authenticatedMessage.Payload.GetBytes());
            var isOk = messageMAC.IsEqual(authenticatedMessage.MAC);
            return isOk;
        }
        protected abstract Message ReceiveAuthenticatedMessage(AuthenticatedMessage message);
        protected abstract Message ReceiveOfflineMessage(OfflineMessage message);
        protected Message RecieveOtherMessage(Message message) { return new NOP(); }
        public abstract void PrintStatus();
    }
}
