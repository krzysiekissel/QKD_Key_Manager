//KeyAllocation.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    [Serializable]
    class KeyAllocation
    {
        public Dictionary<string,QualityOfService> Allocation { get; set; }
    }
}
