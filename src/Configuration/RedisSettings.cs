using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Configuration
{
    public class RedisSettings
    {
        public string InstanceName { get; set; }
        public string ConnectionString { get; set; }
    }
}
