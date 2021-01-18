//CommonKeyStoreSystem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class CommonKeyStoreSystem : CommonKeyStore
    {
        public CommonKeyStoreSystem(ServiceKeyBuffer serviceKeyBuffer)
        {
            ServiceKeyBuffer = serviceKeyBuffer;
            UndecidedKeyBuffer = new KeyBuffer();
        }

        public override void AllocateUndecidedKeys(KeyAllocation keyAllocation)
        {
            base.AllocateUndecidedKeys(keyAllocation);

            foreach(var allocation in keyAllocation.Allocation)
            {
                if (!ApplicationKeyBuffers.ContainsKey(allocation.Key))
                {
                    AddKeyBuffer(allocation.Key, allocation.Value);
                }
            }

            var sorted = keyAllocation.Allocation.ToList().OrderBy(s => s.Value.Priority);
            int undecidedLength = 0;
            do
            {
                undecidedLength = UndecidedKeyBuffer.GetLength();
                foreach(var allocation in sorted)
                {
                    if (allocation.Value.BPS <= UndecidedKeyBuffer.GetLength())
                    {
                        var key = UndecidedKeyBuffer.GetKey(allocation.Value.BPS);
                        ApplicationKeyBuffers[allocation.Key].AddKey(key);
                    }
                }
            } while (undecidedLength != UndecidedKeyBuffer.GetLength());
        }

    }
}
