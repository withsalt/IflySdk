using System;
using System.Diagnostics;
using System.IO;

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
            ASR();
            Console.ReadKey(false);
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
    }
}
