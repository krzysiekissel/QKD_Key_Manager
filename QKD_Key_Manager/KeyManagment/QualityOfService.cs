//QualityOfService.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    [Serializable]
    class QualityOfService
    {
        public int BPS { get; set; }
        public int Priority { get; set; }
    }
}
