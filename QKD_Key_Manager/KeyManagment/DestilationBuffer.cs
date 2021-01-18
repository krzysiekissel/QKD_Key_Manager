//DestilationBuffer.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class DestilationBuffer 
    {
        public BitArray _bits { get; set; }
        private byte[] _bytes;

        private double _estimatedQBER;

        public DestilationBuffer()
        {
            _bits = null;
        }
        public void SetEstimatedQBER(double estimatedQBER)
        {
            _estimatedQBER = estimatedQBER;
        }
        public void AddBytes(byte[] bytes)
        {
            _bytes = bytes;
            _bits = new BitArray(bytes);
            
        }

        public bool[] GetBits(int index,int length)
        {
            if (_bits.Count - index < length)
            {
                length = _bits.Count - index;
            }
            var array = new bool[_bits.Count];
            _bits.CopyTo(array, 0);
            return array.ToList().Skip(index).Take(length).ToArray();
        }
        
        public double GetEstimatedQBER()
        {
            return _estimatedQBER;
        }

        public bool IsEmpty()
        {
            if (_bits == null)
                return true;
            if (_bits.Length == 0)
                return true;
            return false;
        }
        public int GetSize()
        {
            return _bits.Count;
        }
        public bool GetBit(int index)
        {
            return _bits[index];
        }
        public byte[] GetBytes(int index,int length)
        {
            if (4 * index + 4 * length > _bits.Length)
                throw new ArgumentException();
            var result = new byte[length];
            Array.Copy(_bytes, index, result, 0, length);
            return result;
        }
        public void UpdateBit(int index, bool value)
        {
            _bits[index] = value;
        }
        public void UpdateBuffer(int index,bool[] newBits)
        {
            for(int i = index; i < newBits.Length; i++)
            {
                _bits[i] = newBits[i];
            }
        }
        public void ShiftedXOr(byte[] preSharedKey)
        {
            BigInteger integer = new BigInteger(preSharedKey, true);
            BitArray shifted = new BitArray(LeftCyclicShift(new BitArray(_bits),(int)(integer%_bits.Count)));

            BitArray result = shifted.Xor(_bits);

            _bits = result;

        }
        private BitArray LeftCyclicShift(BitArray bitArray,int count)
        {
            BitArray temp = new BitArray(bitArray);
            BitArray result = bitArray.LeftShift(count);
            for(int i = 0; i < count; i++)
            {
                result[i] = temp[temp.Count-1 - i];
            }
            return result;
        }
        public void Slice(double ratio)
        {
            var resultLength = (int)(ratio * _bits.Length);
            _bits.Length = resultLength;
        }
        public void Permute(byte[] preSharedKey)
        {
            BitArray result = new BitArray(_bits);
            BigInteger integer = new BigInteger(preSharedKey,true);
            var random = new Random((int)integer);
            for (int i = _bits.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                bool temp = result[i];
                result[i] = result[swapIndex];
                result[swapIndex] = temp;
            }
            _bits = result;
        }

        public byte[] FlushBuffer()
        {
            byte[] bytes = new byte[_bits.Length / 4];
            new BitArray(_bits).CopyTo(bytes, 0);
            _bits = null;
            return bytes;
        }
    }
}
