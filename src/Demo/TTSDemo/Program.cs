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

        /// <summary>
        /// 使用TTS时必须添加IP白名单
        /// </summary>
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
                        Console.WriteLine("错误：" + e.Message);
                    })
                    .UseMessage((sender, e) =>
                    {
                        Console.WriteLine("结果：" + e);
                    })
                    //.WithSavePath("test.wav")
                    .BuildTTS();

                ResultModel<string> result = await tts.Convert(str);
                if(result.Code == ResultCode.Success)
                {
                    //File.WriteAllBytes("test.wav", await StreamToByte(result.Data));
                    //string path = Environment.CurrentDirectory + "\\" + "test.wav";
                    //if (File.Exists(path))
                    //{
                    //    Console.WriteLine("保存成功！");
                    //}
                    Console.WriteLine("识别结束！");
                }
                else
                {
                    Console.WriteLine("\n错误：" + result.Message);
                }

                //或者直接采用ConvertAndSave方法
                //ResultModel<string> result = await iat.ConvertAndSave(str);
                //if (result.Code == ResultCode.Success)
                //{
                //    Console.WriteLine("\n保存成功：" + result.Data);
                //}
                //else
                //{
                //    Console.WriteLine("\n错误：" + result.Message);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task<byte[]> StreamToByte(MemoryStream memoryStream)
        {
            byte[] buffer = new byte[memoryStream.Length];
            memoryStream.Seek(0, SeekOrigin.Begin);
            int count = await memoryStream.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
