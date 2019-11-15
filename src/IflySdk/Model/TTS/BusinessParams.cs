using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IflySdk.Model.TTS
{
    public class BusinessParams
    {
        #region 官方参数

        public string ent { get; set; } = "intp65";

        public string aue { get; set; } = "raw";

        public string auf { get; set; } = "audio/L16;rate=16000";

        public string vcn { get; set; } = "xiaoyan";

        public int speed { get; set; } = 50;

        public int volume { get; set; } = 50;

        public int pitch { get; set; } = 50;

        public int bgs { get; set; } = 0;

        public string tte { get; set; } = "UTF8";

        public string reg { get; set; } = "2";

        public string ram { get; set; } = "0";

        public string rdn { get; set; } = "0";

        #endregion

        #region 非官方参数

        /// <summary>
        /// 保存位置
        /// </summary>
        [JsonIgnore]
        public string save_path { get; set; }

        #endregion
    }
}
