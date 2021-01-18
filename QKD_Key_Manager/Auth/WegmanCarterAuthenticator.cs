//WegmanCarterAuthenticator.cs
using QKD_Key_Manager.KeyManagment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text;

namespace QKD_Key_Manager.Auth
{
    class WegmanCarterAuthenticator : IMessageAuthenticator
    {
        private KeyBuffer _keyBuffer;
        private int _macLength;
        public WegmanCarterAuthenticator(KeyBuffer keyBuffer,int macLength)
        {
            _keyBuffer = keyBuffer;
            _macLength = macLength;
        }
        public MAC GetMAC(byte[] bytes)
        {
            var WCHash = new WCHash(bytes.Length*4, _macLength*4, _keyBuffer.GetKey((int)Math.Ceiling( (_macLength+ Math.Log2(Math.Log2((int)bytes.Length*4)) * Math.Log2((int)bytes.Length*4)))));

            var mac= new MAC(WCHash.F(bytes).ToByteArray());
            return mac;
        }
    }

    public class WCHash
    {
        private BigInteger[] _q;
        private BigInteger[] _r;
        private BigInteger _p;
        private int _a;
        private int _b;
        private int _s;

        public WCHash(int inputLength, int outputLength, Key key)
        {
            _a = inputLength;
            _b = outputLength;
            _s = outputLength + Convert.ToInt32(Math.Ceiling(Math.Log2(Math.Log2(inputLength))));
            _p = BigInteger.Parse("933667625601617143040075762732794923981773");
            var len_f = Convert.ToInt32(Math.Ceiling(Math.Log2(Math.Ceiling(inputLength / (double)_s))));
            if (len_f == 0)
                len_f = 1;
            var maxKey = BigInteger.Pow(((_p - 1) * _p), len_f);
            var intListKey = f(key.GetBytes(), maxKey, ((_p - 1) * _p));
            _q = new BigInteger[intListKey.Length];
            _r = new BigInteger[intListKey.Length];
            for (int i = 0; i < intListKey.Length; i++)
            {
                BigInteger r;
                BigInteger q = BigInteger.DivRem(intListKey[i], _p, out r);
                q += 1;
                _q[i] = q;
                _r[i] = r;
            }
        }
        public BigInteger F(byte[] message)
        {
            var m = f(message, BigInteger.Pow(2, _a), BigInteger.Pow(2, 2 * _s));
            var l = _q.Length;
            BigInteger[] a = m;
            for (int i = 0; i < l; i++)
            {
                if (a.Length == 1)
                    break;
                BigInteger[] temp = new BigInteger[a.Length / 2];
                for (int j = 0; j < a.Length / 2; j++)
                {
                    temp[j] = (((_q[i] * a[j] + _r[i]) % _p) % BigInteger.Pow(2, _s) + (((_q[i] * a[j + 1] + _r[i]) % _p) % BigInteger.Pow(2, _s)) * BigInteger.Pow(2, _s));
                }

                if (a.Length % 2 != 0)
                {

                    temp[temp.Length - 1] = ((_q[i] * a[a.Length - 1] + _r[i]) % _p) % BigInteger.Pow(2, _s);
                }
                a = temp;

            }

            return a[0] % BigInteger.Pow(2, _b);
        }
        private BigInteger[] f(byte[] payload, BigInteger xMax, BigInteger s)
        {
            BigInteger x = BytesToBigInteger(payload);
            if (xMax == 0)
                x += 1;
            var log = BigInteger.Log(xMax, (double)s);
            if (log < 1)
            {
                log = 1;
            }
            BigInteger[] array = new BigInteger[(int)Math.Ceiling(log)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = x % s;
                x = x / s;
            }
            return array;


        }
        private BigInteger BytesToBigInteger(byte[] payload)
        {
            return new BigInteger(payload,true);
        }

    }
}
