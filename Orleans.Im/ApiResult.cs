using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Im
{
    public class ApiResult<T>
    {
        public int Code { get; set; }

        public string Message { get; set; } = "";

        public T Data { get; set; }
    }
}
