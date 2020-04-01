using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IflySdk.Common
{

    //实现线性PCM加wav头
    public class PcmToWav
    {
        //音频头结构
        public struct Header
        {

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] fccID;
            public UInt32 dwSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] fccType;

        }
        //音频FMT块
        public struct FMT
        {

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] fccID;
            public UInt32 dwSize;
            public UInt16 wFormatTag;
            public UInt16 wChannels;
            public UInt32 dwSamplesPerSec;
            public UInt32 dwAvgBytesPerSec;
            public UInt16 wBlockAlign;
            public UInt16 uiBitsPerSample;
        }

        //音频DATA块
        public struct DATA
        {

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] fccID;
            public UInt32 dwSize;
        }

        //实例化
        Header pcmHEADER = new Header();
        FMT pcmFMT = new FMT();
        DATA pcmDATA = new DATA();
        int pcmHEADERSize,pcmFMTSize,pcmDATASize;
        
        //pcm文件的声道数,
        public UInt16 PCM_CHANNEL_NUM=1;
        //pcm文件的采样率
        public UInt32 PCM_SAMPLE_RATE= 16000;
        //每次采样占用的位数 8/16/32等
        public UInt16 PCM_SAMPLE_BITS= 16;

        //    binaryWriter.Write(channels); // Mono,声道数目，1-- 单声道；2-- 双声道  
        //            //    binaryWriter.Write(16000);// 16KHz 采样频率                     
        //            //    binaryWriter.Write(32000); //每秒所需字节数  

        /// <summary>
        /// 转换为WAV格式
        /// </summary>
        /// <param name="pcmPath">PCM文件路径</param>
        /// <param name="wavPath">WAV保存路径，如果为空则默认保存到PCM路径</param>
        /// <returns></returns>
        public string ConverterToWav(string pcmPath, string wavPath=null)
        {

            //判断文件是否存在 
            FileInfo fi = new FileInfo(pcmPath);
            if (fi.Exists == false)
                return null ;
            if (fi.Extension != ".pcm")
                return null;
            if (string.IsNullOrEmpty(wavPath))
            {
                wavPath = fi.FullName.Replace(".pcm", ".wav");
            }

            //如果输出文件存在，就删除原文件并创建
            if (File.Exists(wavPath))
            {
                FileInfo fiwav = new FileInfo(wavPath);
                fiwav.Delete();
            }
            //否则创建输出文件
            FileStream wavfs = new FileStream(wavPath, FileMode.Create);

            //开始创建Header
            //char[] fccIDHeader = new char[] { 'R', 'I', 'F', 'F'};
            //fccIDHeader.CopyTo(pcmHEADER.fccID, 0);
            //char[] fccType = new char[] { 'W', 'A', 'V', 'E' };
            //fccType.CopyTo(pcmHEADER.fccType, 0);
            pcmHEADER.fccID = new byte[]{ Convert.ToByte('R'), Convert.ToByte('I'), Convert.ToByte('F'), Convert.ToByte('F') };
            pcmHEADER.fccType = new byte[] { Convert.ToByte('W'), Convert.ToByte('A'), Convert.ToByte('V'), Convert.ToByte('E') };
            //越过wav的Header部分
            unsafe
            {
                pcmHEADERSize = Marshal.SizeOf(pcmHEADER);
                wavfs.Seek(pcmHEADERSize, SeekOrigin.Begin);
            }

            //开始创建FMT
            //char[] fccIDFMT = new char[] { 'F', 'M', 'T', '\0' };
            //fccIDFMT.CopyTo(pcmFMT.fccID, 0);
            
            pcmFMT.fccID = new byte[] { Convert.ToByte('f'), Convert.ToByte('m'), Convert.ToByte('t'), 32/* 空白*/ };
            pcmFMT.dwSize = 16;
            //PCM格式
            pcmFMT.wFormatTag = 1;
            //采样位数
            pcmFMT.uiBitsPerSample = PCM_SAMPLE_BITS;
            //采样率
            pcmFMT.dwSamplesPerSec = PCM_SAMPLE_RATE;
            //声道数
            pcmFMT.wChannels = PCM_CHANNEL_NUM;

            // 每秒所需字节数 (采样率*声道数*每个采样所需的BIT /8 )
            pcmFMT.dwAvgBytesPerSec = pcmFMT.dwSamplesPerSec * pcmFMT.wChannels * pcmFMT.uiBitsPerSample / 8;
            //数据块对齐单位(每个采样需要的字节数: 采样位*声道数/8)
            pcmFMT.wBlockAlign =(ushort) (pcmFMT.uiBitsPerSample * pcmFMT.wChannels / 8);
            
            //开始写FMT
            unsafe
            {
                pcmFMTSize= Marshal.SizeOf(pcmFMT);
                byte[] pcmFMTBytes = StructToBytes(pcmFMT, pcmFMTSize);
                wavfs.Write(pcmFMTBytes, 0, pcmFMTSize);
            }

            //开始建立DATA
            //char[] fccIDATA = new char[] { 'D', 'A', 'T', 'A' };
            //fccIDATA.CopyTo(pcmDATA.fccID, 0);
            pcmDATA.fccID = new byte[] { Convert.ToByte('d'), Convert.ToByte('a'), Convert.ToByte('t'), Convert.ToByte('a') };
            
            //越过pcmDATA长度
            unsafe
            {
                pcmDATASize = Marshal.SizeOf(pcmDATA);
                wavfs.Seek(pcmDATASize, SeekOrigin.Current);
            }

            //开始读pcm文件
            
            using (FileStream fs = new FileStream(pcmPath, FileMode.Open, FileAccess.Read))
            {
                byte[] dt = new byte[fs.Length];
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(dt, 0, Convert.ToInt32(fs.Length));
                wavfs.Write(dt, 0, Convert.ToInt32(fs.Length));
                pcmDATA.dwSize = Convert.ToUInt32(fs.Length);


                //while (fs.Position < fs.Length)
                //{
                //    wavfs.Write(dt, 0, 2);
                //    pcmDATA.dwSize += 2;
                //    if (fs.Position == fs.Length - 1)
                //        break;
                //    else
                //      fs.Read(dt, 0, 2);
                //}
         
            }
            pcmHEADER.dwSize =/*44*/ 36 + pcmDATA.dwSize;   //根据pcmDATA.dwsize得出pcmHEADER.dwsize的值 
            //指针回溯
            wavfs.Seek(0, SeekOrigin.Begin);

            //写入Header
            unsafe
            {
                pcmHEADERSize = Marshal.SizeOf(pcmHEADER);
                byte[] pcmHEADERBytes = StructToBytes(pcmHEADER, pcmHEADERSize);
                wavfs.Write(pcmHEADERBytes, 0, pcmHEADERSize);
            }

            //越过FMT
            wavfs.Seek(pcmFMTSize,SeekOrigin.Current);
            //写入DATA
            byte[] pcmDATABytes = StructToBytes(pcmDATA, pcmDATASize);
            wavfs.Write(pcmDATABytes,0,pcmDATASize);
            wavfs.Close();

            return wavPath;
        }

        //将结构体类型转换为Byte
        public static byte[] StructToBytes(object structObj, int size)
        {
            byte[] bytes = new byte[size];
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将结构体拷到分配好的内存空间
            Marshal.StructureToPtr(structObj, structPtr, false);
            //从内存空间拷贝到byte 数组
            Marshal.Copy(structPtr, bytes, 0, size);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            return bytes;

        }

        //将Byte转换为结构体类型
        public static object ByteToStruct(byte[] bytes, Type type)
        {
            int size = Marshal.SizeOf(type);
            if (size > bytes.Length)
            {
                return null;
            }
            //分配结构体内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组拷贝到分配好的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);
            //将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            return obj;
        }
    }


    


}
