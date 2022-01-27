using System;
using System.IO;
using HavokScriptToolsCommon;

namespace HavokScriptDisassembler
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes(args[0]);
            var disassembler = new HksDisassembler(data);
            string result = disassembler.Disassemble();
            Console.WriteLine(result);
        }
    }
}
