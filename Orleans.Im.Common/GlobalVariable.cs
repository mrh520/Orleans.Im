using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Orleans.Im.Common
{
    
    public class GlobalVariable
    {    
        public static IConfiguration Configuration { get; set; }
    }
}
