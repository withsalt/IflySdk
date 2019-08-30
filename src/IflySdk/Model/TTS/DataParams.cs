using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.TTS
{
    public class DataParams
    {
        /// <summary>
        /// 音频采样率
        /// audio/L16;rate=16000
        /// audio/L16;rate=8000
        /// (目前官网"x_"系列发音人中仅讯飞虫虫，讯飞春春，讯飞飞飞，讯飞刚刚，讯飞宋宝宝，讯飞小包，讯飞小东，讯飞小肥，讯飞小乔，讯飞小瑞，讯飞小师，讯飞小王，讯飞颖儿支持8k)
        /// </summary>
        public string auf { get; set; } = "audio/L16;rate=16000";

        /// <summary>
        /// 音频编码
        /// raw（未压缩的wav格式）
        /// lame（mp3格式）
        /// </summary>
        public string aue { get; set; } = "raw";

        /// <summary>
        /// 发音人，可选值详见控制台-我的应用-在线语音合成服务管理-发音人授权管理，使用方法参考官网
        /// </summary>
        public string voice_name { get; set; } = "xiaoyan";

        /// <summary>
        /// 语速，可选值：[0-100]，默认为50
        /// </summary>
        public string speed { get; set; } = "50";

        /// <summary>
        /// 音量，可选值：[0-100]，默认为50
        /// </summary>
        public string volume { get; set; } = "50";

        /// <summary>
        /// 音高，可选值：[0-100]，默认为50
        /// </summary>
        public string pitch { get; set; } = "50";

        /// <summary>
        /// 引擎类型
        /// aisound（普通效果）
        /// intp65（中文）
        /// intp65_en（英文）
        /// mtts（小语种，需配合小语种发音人使用）
        /// x（优化效果）
        /// 默认为intp65
        /// </summary>
        public string engine_type { get; set; } = "intp65";

        /// <summary>
        /// 文本类型，可选值：text（普通格式文本），默认为text
        /// </summary>
        public string text_type { get; set; } = "text";

        /// <summary>
        /// 保存位置
        /// </summary>
        [JsonIgnoreAttribute]
        public string save_path { get; set; }
    }
}
