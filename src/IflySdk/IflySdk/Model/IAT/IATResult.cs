using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Model.IAT.ResultNode;

namespace IflySdk.Model.IAT
{
    class IATResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string sid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Data data { get; set; }
    }
}
