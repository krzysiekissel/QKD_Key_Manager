//IMessageAuthenticator.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.Auth
{
    interface IMessageAuthenticator
    {
        MAC GetMAC(byte[] bytes);
    }
}
