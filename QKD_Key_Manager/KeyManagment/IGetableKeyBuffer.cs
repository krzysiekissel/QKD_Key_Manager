//IGetableKeyBuffer.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    interface IGetableKeyBuffer
    {
        Key GetKey(int length);
        Key PeekKey(int length);
        bool IsEmpty();
        int GetLength();
    }
}
