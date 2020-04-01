using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using IflySdk;
using IflySdk.Common;
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
            string str = @"正在为您查询合肥的天气情况。今天是2020年2月24日，合肥市今天多云，最低温度9摄氏度，最高温度15摄氏度，微风。";
            try
            {
                TTSApi tts = new ApiBuilder()
                    .WithAppSettings(new AppSettings()
                    {
                        ApiKey = "7b845bf729c3eeb97be6de4d29e0b446",
                        ApiSecret = "50c591a9cde3b1ce14d201db9d793b01",
                        AppID = "5c56f257"
                    })
                    //设置发音人
                    .WithVcn("xiaoyan")
                    //设置音量
                    .WithVolume(50)
                    //设置语速
                    .WithSpeed(50)
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
                if (result.Code == IflySdk.Enum.ResultCode.Success)
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
                        //转pcm为wav格式
                        PcmToWav pcm = new PcmToWav();
                        pcm.ConverterToWav(path);

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
