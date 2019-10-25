using System;
using System.Collections.Generic;
using System.Text;

namespace IflySdk.Enum
{
    public enum ResultCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 警告
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnect = 2,

        /// <summary>
        /// 系统错误
        /// </summary>
        Error = 100,
    }
}
