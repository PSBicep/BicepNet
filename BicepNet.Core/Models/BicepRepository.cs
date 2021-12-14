using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BicepNet.Core.Models
{
    public class BicepRepository
    {
        public string Name { get; set; }
        public string Endpoint { get; set; }
        public IList<BicepRepositoryModule> ModuleVersions { get; set; } = new List<BicepRepositoryModule>();

        public override string ToString()
        {
            return Name;
        }
    }
}
