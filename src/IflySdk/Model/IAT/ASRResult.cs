using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Model.IAT.ResultNode;

namespace IflySdk.Model.IAT
{
    class ASRResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Sid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Data Data { get; set; }
    }
}
