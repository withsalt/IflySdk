using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Enum;

namespace IflySdk.Model.IAT
{
    class FirstFrameData
    {
        public FirstFrameData()
        {
            data = new DataParams();
            business = new BusinessParams();
            common = new CommonParams();

            data.status = FrameState.First;
        }

        public DataParams data { get; set; }

        public BusinessParams business { get; set; }

        public CommonParams common { get; set; }
    }
}
