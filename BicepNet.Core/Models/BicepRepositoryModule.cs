using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BicepNet.Core.Models
{
    public class BicepRepositoryModule
    {
        public string Digest { get; set; }
        public string Repository { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public DateTimeOffset Created { get; set; }

        public override string ToString()
        {
            return string.Join(", ", Tags);
        }
    }
}
