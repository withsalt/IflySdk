using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Enum;

namespace IflySdk.Model.IAT
{
    class LastFrameData
    {
        public LastFrameData()
        {
            data = new DataParams();
            data.status = FrameState.Last;
        }
        public DataParams data { get; set; }
    }
}
