using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.TTS
{
    public class DataParams
    {
        public string text { get; set; }

        public string encoding { get; set; } = "";

        public int status
        {
            get
            {
                return 2;
            }
        }
    }
}
