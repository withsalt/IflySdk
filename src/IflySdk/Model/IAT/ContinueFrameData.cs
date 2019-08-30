using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.IAT
{
    public class ContinueFrameData
    {
        public ContinueFrameData()
        {
            data = new DataParams();
        }
        public DataParams data { get; set; }
    }
}
