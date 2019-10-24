using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using IflySdk;
using IflySdk.Enum;
using IflySdk.Model.Common;
using IflySdk.Model.IAT;

namespace ASRDemo
{
    class Program
    {
        static void Main()
        {
            ASRAppend();
            Console.ReadKey(false);
        }

        static async void ASRAppend()
        {
            string path = @"02.pcm";  //测试文件路径,自己修改
            int frameSize = 10000;
            byte[] data = File.ReadAllBytes(path);

            try
            {
                ASRApi iat = new ApiBuilder()
                    .WithAppSettings(new AppSettings()
                    {
                        ApiKey = "7b845bf729c3eeb97be6de4d29e0b446",
                        ApiSecret = "50c591a9cde3b1ce14d201db9d793b01",
                        AppID = "5c56f257"
                    })
                    .UseError((sender, e) =>
                    {
                        Console.WriteLine("错误：" + e.Message);
                    })
                    .UseMessage((sender, e) =>
                    {
                        Console.WriteLine("实时结果：" + e);
                    })
                    .BuildASR();

                //计算识别时间
                Stopwatch sw = new Stopwatch();
                sw.Start();

                byte[] buffer = null;
                for (int i = 0; i < data.Length; i += frameSize)
                {
                    await Task.Delay(150);  //模拟说话暂停
                    buffer = SubArray(data, i, frameSize);
                    if (buffer == null || data.Length - i < frameSize)
                    {
                        //最后一个分片
                        iat.ConvertAppend(buffer, true);
                    }
                    else
                    {
                        iat.ConvertAppend(buffer);
                    }
                }

                Console.WriteLine("发送结束...等待退出...");
                while (iat.Status)
                {
                    await Task.Delay(10);
                }
                sw.Stop();
                Console.WriteLine($"总共花费{Math.Round(sw.Elapsed.TotalSeconds, 2)}秒。");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async void ASR()
        {
            string path = @"02.pcm";  //测试文件路径,自己修改
            byte[] data = File.ReadAllBytes(path);

            try
            {
                ASRApi iat = new ApiBuilder()
                    .WithAppSettings(new AppSettings()
                    {
                        ApiKey = "7b845bf729c3eeb97be6de4d29e0b446",
                        ApiSecret = "50c591a9cde3b1ce14d201db9d793b01",
                        AppID = "5c56f257"
                    })
                    .UseError((sender, e) =>
                    {
                        Console.WriteLine("错误：" + e.Message);
                    })
                    .UseMessage((sender, e) =>
                    {
                        Console.WriteLine("实时结果：" + e);
                    })
                    .BuildASR();

                //计算识别时间
                Stopwatch sw = new Stopwatch();
                sw.Start();

                ResultModel<string> result = await iat.Convert(data);
                if (result.Code == ResultCode.Success)
                {
                    Console.WriteLine("\n识别结果：" + result.Data);
                }
                else
                {
                    Console.WriteLine("\n识别错误：" + result.Message);
                }

                sw.Stop();
                Console.WriteLine($"总共花费{Math.Round(sw.Elapsed.TotalSeconds, 2)}秒。");
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
