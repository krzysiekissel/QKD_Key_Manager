//MAC.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.Auth
{
    class MAC
    {
        public MAC(byte[] bytes)
        {
            _bytes = bytes;
        }
        private byte[] _bytes;
        public byte[] GetBytes()
        {
            return _bytes;
        }
        public bool IsEqual(MAC other)
        {

            return Enumerable.SequenceEqual(_bytes, other.GetBytes());
        }
        public override string ToString()
        {
            return Convert.ToBase64String(_bytes);
        }
    }
}
