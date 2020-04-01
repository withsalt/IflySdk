using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IflySdk.Model.TTS
{
    public class BusinessParams
    {
        #region 官方参数
        /// <summary>
        /// 引擎类型
        /// aisound（普通效果）
        /// intp65（中文）
        /// intp65_en（英文）
        /// xtts（优化效果）
        /// 默认为intp65
        /// </summary>
        public string ent { get; set; } = "xtts";
        /// <summary>
        /// 音频编码
        /// speex：压缩格式
        /// speex-wb;7：压缩格式，压缩等级1 ~10，默认为7
        /// </summary>
        public string aue { get; set; } = "raw";
        /// <summary>
        /// 音频采样率
        /// audio/L16;rate=16000
        /// audio/L16;rate=8000
        /// (目前官网"x_"系列发音人中仅讯飞虫虫，讯飞春春，讯飞飞飞，讯飞刚刚，讯飞宋宝宝，讯飞小包，讯飞小东，讯飞小肥，讯飞小乔，讯飞小瑞，讯飞小师，讯飞小王，讯飞颖儿支持8k)
        /// </summary>
        public string auf { get; set; } = "audio/L16;rate=16000";
        /// <summary>
        /// 发音人，可选值详见控制台-我的应用-在线语音合成服务管理-发音人授权管理，使用方法参考官网
        /// </summary>
        public string vcn { get; set; } = "xiaoyan";
        /// <summary>
        /// 语速，可选值：[0-100]，默认为50
        /// </summary>
        public int speed { get; set; } = 50;
        /// <summary>
        /// 音量，可选值：[0-100]，默认为50
        /// </summary>
        public int volume { get; set; } = 50;
        //public int pitch { get; set; } = 50;

        //public int bgs { get; set; } = 0;

        /// <summary>
        /// 文本编码格式
        /// GB2312、GBK、BIG5、UNICODE、GB18030、UTF8
        /// </summary>
        public string tte { get; set; } = "UTF8";

        //public string reg { get; set; } = "2";

        //public string ram { get; set; } = "0";

        //public string rdn { get; set; } = "0";

        #endregion

        #region 非官方参数

        /// <summary>
        /// 保存文件路径
        /// </summary>
        [JsonIgnore]
        public string save_path { get; set; }

        #endregion
    }
}
