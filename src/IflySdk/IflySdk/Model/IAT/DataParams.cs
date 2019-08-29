using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Enum;

namespace IflySdk.Model.IAT
{
    public class DataParams
    {
        /// <summary>
        /// 音频的状态
        /// 0 :第一帧音频
        /// 1 :中间的音频
        /// 2 :最后一帧音频，最后一帧必须要发送
        /// </summary>
        public FrameState status { get; set; }

        /// <summary>
        /// 音频的采样率支持16k和8k
        /// 16k音频：audio/L16;rate=16000
        /// 8k音频：audio/L16;rate=8000
        /// </summary>
        public string format { get; set; } = "audio/L16;rate=16000";

        /// <summary>
        /// 音频数据格式
        /// raw：原生音频（支持单声道的pcm和wav）
        /// speex：speex压缩后的音频（8k）
        /// speex-wb：speex压缩后的音频（16k）
        /// 其他请根据音频格式设置为匹配的值：amr、amr-wb、amr-wb-fx、ico、ict、opus、opus-wb、opus-ogg
        /// 请注意压缩前也必须是采样率16k或8k单声道的pcm或wav格式。
        /// 样例音频请参照音频样例
        /// </summary>
        public string encoding { get; set; } = "raw";

        /// <summary>
        /// 音频内容，采用base64编码
        /// </summary>
        public string audio { get; set; }
    }
}
