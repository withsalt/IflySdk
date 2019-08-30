using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.IAT.ResultNode
{
    class Result
    {
        /// <summary>
        /// 
        /// </summary>
        public int bg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ls { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pgs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<int> rg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sn { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<WsItem> ws { get; set; }
    }
}
