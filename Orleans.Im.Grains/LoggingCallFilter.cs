using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Im.Grains
{
    public class LoggingCallFilter : IIncomingGrainCallFilter
    {
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            await context.Invoke();

        }
    }

    public class LoggingCallFilter2 : IOutgoingGrainCallFilter
    {
        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            await context.Invoke();
        }
    }
}
