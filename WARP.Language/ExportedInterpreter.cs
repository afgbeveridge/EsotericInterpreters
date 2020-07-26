using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedObjects.Esoterica;
using System.Reflection;
using com.complexomnibus.esoteric.interpreter.abstractions;

namespace WARP {

    public class ExportedInterpreter : IEsotericInterpreter {

        public void Interpret(IOWrapper wrapper, string[] src) {
            new BasicInterpreter<SimpleSourceCode, PropertyBasedExecutionEnvironment>()
                .Execute(Assembly.GetExecutingAssembly(), src, 
                interp => {
                    var env = interp.State.GetExecutionEnvironment<PropertyBasedExecutionEnvironment>();
                    env.ScratchPad[Constants.RASName] =
                        new RandomAccessStack<WARPObject> { MaximumSize = Configuration.ConfigurationFor<int>("rasSize") };
                    env.ScratchPad[Constants.CurrentBase] = wrapper;
                    env.OnUnknownKey = e => new WARPObject();
                }
            );
        }

        public string Language {
            get {
                return "WARP";
            }
        }

        public string Summary {
            get {
                return "WARP is an object and stack based language, created by User:Aldous zodiac (talk) in May 2013. All numerics are signed, integral and expressed in hexatridecimal (base 36) notation, unless the radix system is changed within an executing program.";
            }
        }

        public string Url { get { return "https://esolangs.org/wiki/WARP"; } }
    }
}
