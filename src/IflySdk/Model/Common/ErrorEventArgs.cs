using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Enum;

namespace IflySdk.Model.Common
{
    public class ErrorEventArgs
    {
        public ResultCode Code { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }
    }
}
