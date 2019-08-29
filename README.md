# IflySdk
科大讯飞SDK，目前支持流式语音识别、语音合成

#### 注意
其中的AppID和ApiKey为测试APP，只有500次调用量，用完即止。请更换为自己的APP。

#### 存在问题
1、语音识别速度偏慢

### 使用方法
#### ASR
```csharp
static async void ASR()
{
    string path = @"test.pcm";  //测试文件路径,自己修改
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

        ResultModel<string> result = await iat.Convert(data);
        if (result.Code == ResultCode.Success)
        {
            Console.WriteLine("\n识别结果：" + result.Data);
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
```

#### TTS
使用TTS之前先将目前IP添加到IP白名单。

```csharp
static async void TTS()
{
    string str = @"两只黄鹂鸣翠柳，一行白鹭上青天";
    try
    {
        TTSApi tts = new ApiBuilder()
            .WithAppSettings(new AppSettings()
            {
                //不同类型接口APIKey不一样，比如ASR和TTS的WebApi接口APIKey是不一样的
                ApiKey = "a8fae54d39911418e8501e97e783878f",
                AppID = "5c56f257"
            })
            .WithSavePath("test.wav")
            .BuildTTS();

        ResultModel<MemoryStream> result = await tts.Convert(str);
        if (result.Code == ResultCode.Success)
        {
            File.WriteAllBytes("test.wav", await StreamToByte(result.Data));
            string path = Environment.CurrentDirectory + "\\" + "test.wav";
            if (File.Exists(path))
            {
                Console.WriteLine("保存成功！");
            }
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
```
