using System;
using System.Collections.Generic;
using System.Text;
using IflySdk.Enum;

namespace IflySdk.Model.Common
{
    public class ResultModel<T> where T : class
    {
        public ResultCode Code { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }
    }
}
