//IAddableKeyBuffer.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    interface IAddableKeyBuffer
    {
        void AddKey(Key key);
    }
}
