//ServiceKeyBuffer.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class ServiceKeyBuffer : KeyBuffer
    {
        private int _serviceKeyLength;
        public ServiceKeyBuffer(int serviceKeyLength)
        {
            _serviceKeyLength = serviceKeyLength;
        }
        public int GetServiceKeyLength()
        {
            return _serviceKeyLength;
        }
        public Key GetKey()
        {
            return base.GetKey(_serviceKeyLength);
        }
        public Key PeekKey()
        {
            return base.PeekKey(_serviceKeyLength);
        }
    }
}
