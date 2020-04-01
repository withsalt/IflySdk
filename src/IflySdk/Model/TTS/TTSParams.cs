using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Model.Common;

namespace IflySdk.Model.TTS
{
    /// <summary>
    /// TTS参数
    /// </summary>
    public class TTSParams
    {
        /// <summary>
        /// 讯飞接口配置
        /// </summary>
        public AppSettings AppSettings { get; set; }
        /// <summary>
        /// TTS参数设置
        /// </summary>
        public BusinessParams BusinessParams { get; set; }
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 禁用缓存
        /// </summary>
        public bool DisableCache { get; set; }
        /// <summary>
        /// True返回ReturnBase64数据，False返回wav下载链接。
        /// </summary>
        public bool ReturnBase64 { get; set; }
    }
}
