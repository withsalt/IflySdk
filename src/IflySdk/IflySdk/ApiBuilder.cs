using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Model.Common;
using IflySdk.Model.IAT;

namespace IflySdk
{
    public class ApiBuilder
    {
        private AppSettings _settings = null;

        #region ASR

        private string _uid = "";
        private string _language = "zh_cn";
        private string _domain = "iat";
        private string _accent = "mandarin";
        private string _format = "audio/L16;rate=16000";
        private string _encoding = "raw";

        private EventHandler<Model.Common.ErrorEventArgs> _onError = null;
        private EventHandler<string> _onMessage = null;

        #endregion

        #region TTS

        private string _aue = "raw";
        private string _auf = "audio/L16;rate=16000";
        private string _voiceName = "xiaoyan";
        private string _speed = "50";
        private string _volume = "50";
        private string _engineType = "intp65";
        private string _savePath = "result.wav";

        #endregion

        #region ASR


        /// <summary>
        /// 基本设置
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public ApiBuilder WithAppSettings(AppSettings settings)
        {
            _settings = settings;
            if (_settings == null
                || string.IsNullOrEmpty(_settings.AppID))
            {
                throw new Exception("App setting cannot null.");
            }
            return this;
        }

        /// <summary>
        /// 请求用户服务返回的uid，用户及设备级别个性化功能依赖此参数
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public ApiBuilder WithUid(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                _uid = uid;
            }
            else
            {
                _uid = "";
            }
            return this;
        }

        /// <summary>
        /// 语种
        /// zh_cn：中文（支持简单的英文识别）
        /// en_us：英文
        /// ja_jp：日语
        /// ko_kr：韩语
        /// ru-ru：俄语
        /// 注：日韩俄小语种若未授权无法使用会报错11200，可到控制台-语音听写（流式版）-方言/语种处添加试用或购买。
        /// 另外，小语种接口URL与中英文不同，详见接口要求。
        /// </summary>
        public ApiBuilder WithLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                _language = language;
            }
            else
            {
                _language = "zh_cn";
            }
            return this;
        }

        /// <summary>
        /// 应用领域
        /// iat：日常用语
        /// medical：医疗
        /// 注：医疗领域若未授权无法使用，可到控制台-语音听写（流式版）-高级功能处添加试用或购买；若未授权状态下设置该参数并不会报错，但不会生效。
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public ApiBuilder WithDomain(string domain)
        {
            if (!string.IsNullOrEmpty(domain))
            {
                _domain = domain;
            }
            else
            {
                _domain = "iat";
            }
            return this;
        }

        /// <summary>
        /// 方言选项，当前仅在language为中文时，支持方言选择。默认mandarin
        /// mandarin：中文普通话、其他语种
        /// 其他方言：可到控制台-语音听写（流式版）-方言/语种处添加试用或购买，添加后会显示该方言参数值；方言若未授权无法使用会报错11200。
        /// </summary>
        /// <param name="accent"></param>
        /// <returns></returns>
        public ApiBuilder WithAccent(string accent)
        {
            if (!string.IsNullOrEmpty(accent))
            {
                _accent = accent;
            }
            else
            {
                _accent = "mandarin";
            }
            return this;
        }

        /// <summary>
        /// 音频的采样率支持16k和8k，默认16k
        /// 16k音频：audio/L16;rate=16000
        /// 8k音频：audio/L16;rate=8000
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public ApiBuilder WithFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                _format = format;
            }
            else
            {
                _format = "audio/L16;rate=16000";
            }
            return this;
        }

        /// <summary>
        /// 音频数据格式
        /// raw：原生音频（支持单声道的pcm和wav）
        /// speex：speex压缩后的音频（8k）
        /// speex-wb：speex压缩后的音频（16k）
        /// 其他请根据音频格式设置为匹配的值：amr、amr-wb、amr-wb-fx、ico、ict、opus、opus-wb、opus-ogg
        /// 请注意压缩前也必须是采样率16k或8k单声道的pcm或wav格式。
        /// 样例音频请参照 音频样例（https://www.xfyun.cn/doc/asr/voicedictation/API.html#%E8%B0%83%E7%94%A8%E7%A4%BA%E4%BE%8B）
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public ApiBuilder WithEncoding(string encoding)
        {
            if (!string.IsNullOrEmpty(encoding))
            {
                _encoding = encoding;
            }
            else
            {
                _encoding = "raw";
            }
            return this;
        }

        public ApiBuilder UseError(EventHandler<ErrorEventArgs> onError)
        {
            _onError = onError;
            return this;
        }

        public ApiBuilder UseMessage(EventHandler<string> onMessage)
        {
            _onMessage = onMessage;
            return this;
        }


        public ASRApi BuildASR()
        {
            if (_settings == null)
            {
                throw new Exception("App setting can not null.");
            }
            if (string.IsNullOrEmpty(_language))
            {
                throw new Exception("Language set can not null.");
            }
            CommonParams common = new CommonParams()
            {
                app_id = _settings.AppID,
                uid = _uid,
            };
            DataParams data = new DataParams()
            {
                format = _format,
                encoding = _encoding
            };
            BusinessParams business = new BusinessParams()
            {
                language = _language,
                domain = _domain,
                accent = _accent,
            };
            ASRApi api = new ASRApi(_settings, common, data, business);
            api.OnError += _onError;
            api.OnMessage += _onMessage;
            return api;
        }

        #endregion

        #region TTS

        /// <summary>
        /// 音频编码
        /// raw（未压缩的wav格式）
        /// lame（mp3格式）
        /// </summary>
        /// <returns></returns>
        public ApiBuilder WithAue(string aue)
        {
            if (!string.IsNullOrEmpty(aue))
            {
                _aue = aue;
            }
            else
            {
                _aue = "raw";
            }
            return this;
        }

        /// <summary>
        /// 音频采样率
        /// audio/L16;rate=16000
        /// audio/L16;rate=8000
        /// (目前官网"x_"系列发音人中仅讯飞虫虫，讯飞春春，讯飞飞飞，讯飞刚刚，讯飞宋宝宝，讯飞小包，讯飞小东，讯飞小肥，讯飞小乔，讯飞小瑞，讯飞小师，讯飞小王，讯飞颖儿支持8k)
        /// </summary>
        /// <param name="auf"></param>
        /// <returns></returns>
        public ApiBuilder WithAuf(string auf)
        {
            if (!string.IsNullOrEmpty(auf))
            {
                _auf = auf;
            }
            else
            {
                _auf = "audio/L16;rate=16000";
            }
            return this;
        }

        /// <summary>
        /// 发音人，可选值详见控制台-我的应用-在线语音合成服务管理-发音人授权管理，使用方法参考官网
        /// </summary>
        /// <param name="voiceName"></param>
        /// <returns></returns>
        public ApiBuilder WithVoiceName(string voiceName)
        {
            if (!string.IsNullOrEmpty(voiceName))
            {
                _voiceName = voiceName;
            }
            else
            {
                _voiceName = "xiaoyan";
            }
            return this;
        }

        /// <summary>
        /// 语速，可选值：[0-100]，默认为50
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public ApiBuilder WithSpeed(int speed)
        {
            if (speed < 0 || speed > 100)
            {
                _speed = "50";
            }
            else
            {
                _speed = speed.ToString();
            }
            return this;
        }

        /// <summary>
        /// 音量，可选值：[0-100]，默认为50
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public ApiBuilder WithVolume(int volume)
        {
            if (volume < 0 || volume > 100)
            {
                _volume = "50";
            }
            else
            {
                _volume = volume.ToString();
            }
            return this;
        }

        /// <summary>
        /// 引擎类型
        /// aisound（普通效果）
        /// intp65（中文）
        /// intp65_en（英文）
        /// mtts（小语种，需配合小语种发音人使用）
        /// x（优化效果）
        /// 默认为intp65
        /// </summary>
        public ApiBuilder WithEngineType(string engineType)
        {
            if (!string.IsNullOrEmpty(engineType))
            {
                _engineType = engineType;
            }
            else
            {
                _engineType = "intp65";
            }
            return this;
        }

        /// <summary>
        /// 保存文件路径
        /// </summary>
        /// <param name="engineType"></param>
        /// <returns></returns>
        public ApiBuilder WithSavePath(string savePath)
        {
            if (!string.IsNullOrEmpty(savePath))
            {
                _savePath = savePath;
            }
            else
            {
                _savePath = "result.wav";
            }
            return this;
        }

        public TTSApi BuildTTS()
        {
            Model.TTS.DataParams data = new Model.TTS.DataParams()
            {
                aue = _aue,
                auf = _auf,
                voice_name = _voiceName,
                speed = _speed,
                volume = _volume,
                engine_type = _engineType,
                save_path = _savePath
            };
            TTSApi api = new TTSApi(_settings, data);
            api.OnError += _onError;
            return api;
        }

        #endregion


    }
}
