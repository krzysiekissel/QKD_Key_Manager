//CommonKeyStore.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class CommonKeyStore
    {
        public ServiceKeyBuffer ServiceKeyBuffer { get; set; }
        public KeyBuffer UndecidedKeyBuffer { get; set; }
        public Dictionary<string, ApplicationKeyBuffer> ApplicationKeyBuffers { get; set; } = new Dictionary<string, ApplicationKeyBuffer>();
        
        public KeyAllocation GetKeyAllocation()
        {
            var allocation = new Dictionary<string, QualityOfService>();
            foreach(var applicationBuffer in ApplicationKeyBuffers)
            {
                allocation.Add(applicationBuffer.Key, applicationBuffer.Value.QualityOfService);
            }
            return new KeyAllocation() { Allocation = allocation };
        }
        public void AddKeyBuffer(string handle,QualityOfService qos)
        {
            if (ApplicationKeyBuffers.ContainsKey(handle))
                return;

            var newApplicationKeyBuffer = new ApplicationKeyBuffer() { QualityOfService = qos };
            ApplicationKeyBuffers.Add(handle, newApplicationKeyBuffer);
        }
        public void RemoveKeyBuffer(string handle)
        {
            ApplicationKeyBuffers.Remove(handle);
        }
        public void AddNewKey(Key newKey,KeyAllocation keyAllocation)
        {
            Key rest=newKey;

            var split1 = rest.DivideKey(ServiceKeyBuffer.GetServiceKeyLength());

            ServiceKeyBuffer.AddKey(split1.Item1);
            rest = split1.Item2;

            var split2 = rest.DivideKey(ServiceKeyBuffer.GetServiceKeyLength());
            ServiceKeyBuffer.AddKey(split2.Item1);
            rest = split2.Item2;

            UndecidedKeyBuffer.AddKey(rest);
            AllocateUndecidedKeys(keyAllocation);
        }
        public virtual void AllocateUndecidedKeys(KeyAllocation keyAllocation) { }
    }
}
