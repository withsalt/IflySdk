using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.TTS
{
    class TTSFrameData
    {
        public CommonParams common { get; set; }

        public BusinessParams business { get; set; }

        public DataParams data { get; set; }
    }
}
