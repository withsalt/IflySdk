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
            string str = @"白皮书说，党的十八大以来，中国的核安全事业进入安全高效发展的新时期。在核安全观引领下，中国逐步构建起法律规范、行政监管、行业自律、技术保障、人才支撑、文化引领、社会参与、国际合作等为主体的核安全治理体系，核安全防线更加牢固。";
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
