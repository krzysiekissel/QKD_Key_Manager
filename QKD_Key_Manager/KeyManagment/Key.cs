//Key.cs
using QKD_Key_Manager.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    public class Key
    {
        private byte[] _bytes;
        public Key(byte[] bytes)
        {
            _bytes = bytes;
        }
        public void AppendKey(Key key)
        {
            _bytes =_bytes.Concat(key.GetBytes()).ToArray();
        }
        public byte[] GetBytes()
        {
            return _bytes;
        }
        public override string ToString()
        {
            return String.Join(" ", _bytes);
        }
        public int GetLength()
        {
            return _bytes.Length;
        }
        public Tuple<Key,Key> DivideKey(int lengthOfFirstKey)
        {
            if (lengthOfFirstKey > _bytes.Length)
                throw new CannotDivideKeyException();

            return new Tuple<Key, Key>(
                new Key(SliceKey(0, lengthOfFirstKey)),
                new Key(SliceKey(lengthOfFirstKey, _bytes.Length - lengthOfFirstKey))
                );
        }
        private byte[] SliceKey(int startIndex, int length)
        {
            byte[] newArray = new byte[length];
            Array.Copy(_bytes, startIndex, newArray, 0, length);
            return newArray;
        }
    }
}
