using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.IAT
{
    public class BusinessParams
    {
        /// <summary>
        /// 
        /// </summary>
        public string language { get; set; } = "zh_cn";

        /// <summary>
        /// 日常领域
        /// </summary>
        public string domain { get; set; } = "iat";

        /// <summary>
        /// 
        /// </summary>
        public string accent { get; set; } = "mandarin";

        /// <summary>
        /// 静默检测超时
        /// </summary>
        public int vad_eos { get; set; } = 3000;

        /// <summary>
        /// 
        /// </summary>
        public string dwa { get; set; } = "wpgs";

        /// <summary>
        /// 
        /// </summary>
        public string pd { get; set; } = "";

        /// <summary>
        /// （仅中文支持）是否开启标点符号添加 1：开启（默认值） 0：关闭
        /// </summary>
        public int ptt { get; set; } = 1;

        public string rlang
        {
            get
            {
                return "zh-cn";
            }
        }

        public int vinfo { get; set; } = 0;

        /// <summary>
        /// 中英日支持）将返回结果的数字格式规则为阿拉伯数字格式，默认开启
        /// </summary>
        public int nunum { get; set; } = 1;

        //public int speex_size { get; set; }

        /// <summary>
        /// 取值范围[1,5]，通过设置此参数，获取在发音相似时的句子多侯选结果。设置多候选会影响性能，响应时间延迟200ms左右。
        /// </summary>
        public int nbest { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public int wbest { get; set; } = 0;
    }
}
