using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Interafaces.Log
{
    public interface ILogger
    {
        event Action<string> LogMessage;
    }
}
