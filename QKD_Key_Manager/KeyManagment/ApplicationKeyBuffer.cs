//ApplicationKeyBuffer.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace QKD_Key_Manager.KeyManagment
{
    class ApplicationKeyBuffer : KeyBuffer
    {
        public QualityOfService QualityOfService { get; set; }
    }
}
