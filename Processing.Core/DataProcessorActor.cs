using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Processing.Core.Messages;

namespace Processing.Core
{
    public interface IDataProcessor
    {
        public Task<ProcessResult> ProcessAsync(string data);
    }
}
