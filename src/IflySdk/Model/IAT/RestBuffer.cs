using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Model.IAT
{
    class RestBuffer
    {
        private readonly int frameSize;

        public RestBuffer(int frame)
        {
            if(frame <= 0)
            {
                throw new Exception("default frame size can not less than 1.");
            }
            frameSize = frame;

            this.Length = 0;
            this.Cache = new byte[frameSize];
        }

        public int Length { get; set; }

        public byte[] Cache { get; set; }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            this.Length = 0;
            Array.Clear(this.Cache, 0, frameSize);
        }
    }
}
