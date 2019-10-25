using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.IAT
{
    class CacheBuffer
    {
        public CacheBuffer()
        {

        }

        public CacheBuffer(byte[] buffer)
        {
            this.Data = buffer;
            this.IsEnd = false;
        }

        public CacheBuffer(byte[] buffer, bool isEnd)
        {
            this.Data = buffer;
            this.IsEnd = IsEnd;
        }

        public byte[] Data { get; set; }

        public bool IsEnd { get; set; }
    }
}
