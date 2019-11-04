using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using IflySdk;
using IflySdk.Enum;
using IflySdk.Model.Common;

namespace TTSDemo
{
    class Program
    {
        static void Main()
        {
            TTS();
            Console.ReadKey(false);
        }

        static async void TTS()
        {
            string str = @"两只黄鹂鸣翠柳，一行白鹭上青天";
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
                        Console.WriteLine("结果：" + e.Substring(0, 20) + "...");  //Base64的结果。没显示完
                    })
                    .BuildTTS();

                ResultModel<byte[]> result = await tts.Convert(str);
                if (result.Code == ResultCode.Success)
                {
                    //注意：转换后的结果为16K的单声道原始音频，可以使用ffmpeg来测试播放。
                    string path = Path.Combine(Environment.CurrentDirectory, "test.pcm");
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(result.Data, 0, result.Data.Length);
                        fs.Flush();
                    }
                    if (File.Exists(path))
                    {
                        Console.WriteLine("保存成功！");
                    }
                    else
                    {
                        Console.WriteLine("保存失败！");
                    }
                }
                else
                {
                    Console.WriteLine("\n错误：" + result.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
