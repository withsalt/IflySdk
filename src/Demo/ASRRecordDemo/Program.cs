using IflySdk;
using IflySdk.Enum;
using IflySdk.Model.Common;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ASRRecordDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            ASRRecord();
            Console.ReadKey(false);
        }

        static void ASRRecord()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                #region ASRApi

                ASRApi iat = new ApiBuilder()
                    .WithAppSettings(new AppSettings()
                    {
                        ApiKey = "7b845bf729c3eeb97be6de4d29e0b446",
                        ApiSecret = "50c591a9cde3b1ce14d201db9d793b01",
                        AppID = "5c56f257"
                    })
                    .WithVadEos(5000)  //将静默检测超时设置为5s
                    .UseError((sender, e) =>
                    {
                        if (e.Code == ResultCode.Disconnect)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString()}]->开启新的识别回话。");
                        }
                        else
                        {
                            Console.WriteLine("错误：" + e.Message);
                        }
                    })
                    .UseMessage((sender, e) =>
                    {
                        Console.WriteLine("实时结果：" + e);
                    })
                    .BuildASR();

                #endregion

                #region Record

                using WaveInEvent wave = new WaveInEvent();
                wave.WaveFormat = new WaveFormat(16000, 1);
                wave.BufferMilliseconds = 50;
                wave.DataAvailable += (s, a) =>
                {
                    byte[] buffer = SubArray(a.Buffer, 0, a.BytesRecorded);
                    iat.Convert(buffer);

                    if (sw.ElapsedMilliseconds / 1000 > 60)
                    {
                        wave.StopRecording();
                    }
                };
                wave.RecordingStopped += (s, a) =>
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}]->结束录音...");
                };
                wave.StartRecording();
                Console.WriteLine($"[{DateTime.Now.ToString()}]->开始识别...");

                //注册退出事件
                Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
                {
                    bool state = iat.Stop();
                    if (state)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString()}]->语音识别已退出...");
                    }
                    wave.StopRecording();
                };

                //等待识别开始
                while (iat.Status != ServiceStatus.Running)
                {
                    Thread.Sleep(5);
                }

                #endregion

                //等待本次会话结束
                while (iat.Status != ServiceStatus.Stopped)
                {
                    Thread.Sleep(5);  //注意：此处不能使用Task.Delay();
                }
                sw.Stop();
                Console.WriteLine($"[{DateTime.Now.ToString()}]->总共花费{Math.Round(sw.Elapsed.TotalSeconds, 2)}秒。");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static byte[] SubArray(byte[] source, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > source.Length || length < 0)
            {
                return null;
            }
            byte[] Destination;
            if (startIndex + length <= source.Length)
            {
                Destination = new byte[length];
                Array.Copy(source, startIndex, Destination, 0, length);
            }
            else
            {
                Destination = new byte[(source.Length - startIndex)];
                Array.Copy(source, startIndex, Destination, 0, source.Length - startIndex);
            }
            return Destination;
        }
    }
}
