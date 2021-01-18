//KeyBuffer.cs
using QKD_Key_Manager.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class KeyBuffer : IGetableKeyBuffer,IAddableKeyBuffer
    {
        private Key _queue = new Key(new byte[0]);
        public void AddKey(Key key)
        {
            _queue.AppendKey(key);
        }

        public Key GetKey(int length)
        {
            if (length > _queue.GetLength())
                throw new CannotGetKeyException();

            var split = _queue.DivideKey(length);
            _queue = split.Item2;
            return split.Item1;
        }

        public int GetLength()
        {
            return _queue.GetLength();
        }

        public bool IsEmpty()
        {
            return _queue.GetLength() == 0;
        }

        public Key PeekKey(int length)
        {
            if (length > _queue.GetLength())
                throw new CannotGetKeyException();
            var split = _queue.DivideKey(length);
            return split.Item1;
        }
    }
}
