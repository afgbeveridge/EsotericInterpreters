using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedObjects.Esoterica;

namespace Dummy
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class Dummy : IEsotericInterpreter
    {
        public Dummy()
        {
        }

        public void UseWrapper(IOWrapper wrapper) {  }

        public void Interpret(string[] src) {  }
    }
}
