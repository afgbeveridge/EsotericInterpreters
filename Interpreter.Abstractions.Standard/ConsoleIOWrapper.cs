using System;
using System.Threading.Tasks;
using SharedObjects.Esoterica;

namespace com.complexomnibus.esoteric.interpreter.abstractions {

    public class ConsoleIOWrapper : IOWrapper {

        public Task<char> ReadCharacter() {
            return Task.FromResult(Convert.ToChar(Console.Read()));
        }

        public Task<string> ReadString(string defaultIfEmpty = "") {
            return Task.FromResult(Console.ReadLine() ?? defaultIfEmpty);
        }

        public Task Write(string src) {
            Console.Write(src);
            return Task.CompletedTask;
        }

        public Task Write(char c) {
            Console.Write(c);
            return Task.CompletedTask;
        }
    }
}
