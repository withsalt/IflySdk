using IflySdk;
using IflySdk.Enum;
using IflySdk.Model.Common;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TTSPlayDemo.Model;

namespace TTSPlayDemo
{
    /// <summary>
    /// TTS转换后播放
    /// </summary>
    class Program
    {
        private static readonly Queue<CacheBuffer> _cache = new Queue<CacheBuffer>();
        private static readonly object _cacheLocker = new object();
        private static bool _status = false;

        static void Main(string[] args)
        {
            TTS();
            Console.ReadKey(false);
        }

        static async void TTS()
        {
            string str = "量子论是现代物理学的两大基石之一。" +
                "量子论给我们提供了新的关于自然界的表述方法和思考方法。" +
                "量子论揭示了微观物质世界的基本规律，为原子物理学、固体物理学、核物理学和粒子物理学奠定了理论基础。" +
                "它能很好地解释原子结构、原子光谱的规律性、化学元素的性质、光的吸收与辐射等。";

            try
            {
                TTSApi tts = new ApiBuilder()
                    .WithAppSettings(new AppSettings()
                    {
                        ApiKey = "7b845bf729c3eeb97be6de4d29e0b446",
                        ApiSecret = "50c591a9cde3b1ce14d201db9d793b01",
                        AppID = "5c56f257"
                    })
                    .UseError((sender, e) =>
                    {
                        Console.WriteLine(e.Message);
                    })
                    .UseMessage((sender, e) =>
                    {
                        try
                        {
                            byte[] buffer = Convert.FromBase64String(e);
                            Play(new CacheBuffer(buffer));
                        }
                        catch { }
                    })
                    .BuildTTS();

                ResultModel<byte[]> result = await tts.Convert(str);
                if (result.Code == ResultCode.Success)
                {
                    //结束标识
                    Play(new CacheBuffer(null, true));
                    Console.WriteLine($"[{DateTime.Now.ToString()}]本次识别完成...");
                }
                else
                {
                    Console.WriteLine("\n错误：" + result.Message);
                }

                while (_status)
                {
                    await Task.Delay(10);
                }
                Console.WriteLine($"[{DateTime.Now.ToString()}]结束...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #region Play
        private static void Play(CacheBuffer cache)
        {
            if (!_status)
            {
                _status = true;
                Task.Run(() => AudioPlay());
                Console.WriteLine($"[{DateTime.Now.ToString()}]开始播放...");
            }
            lock (_cacheLocker)
            {
                _cache.Enqueue(cache);
            }
        }

        private static async void AudioPlay()
        {
            try
            {
                bool isEnd = false;
                using WaveOutEvent wo = new WaveOutEvent();
                BufferedWaveProvider rs = new BufferedWaveProvider(new WaveFormat(16000, 1))
                {
                    BufferLength = 10240000,  //针对长文本，请标准缓冲区足够大
                    DiscardOnBufferOverflow = true
                };
                wo.Init(rs);
                wo.Play();

                while (_status)
                {
                    CacheBuffer data = null;
                    lock (_cacheLocker)
                    {
                        if (_cache.Count == 0)
                            continue;
                        data = _cache.Dequeue();
                        if (data == null)
                            continue;
                    }
                    if (data.Data != null)
                        rs.AddSamples(data.Data, 0, data.Data.Length);
                    if (data.IsEnd)
                    {
                        isEnd = true;
                        break;
                    }
                }
                while (wo.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1);
                    if (rs.BufferedBytes == 0 && isEnd)
                    {
                        await Task.Delay(300);
                        wo.Stop();
                        wo.Dispose();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                lock (_cacheLocker)
                {
                    _cache.Clear();
                }
                _status = false;
            }
        }
        #endregion
    }
}
