using HavokScriptToolsCommon;
using System;
using System.IO;

namespace HavokScriptAssembler
{
    class Program
    {
        const string usage = "usage: hksasm <filename>";
        static bool ParseArgs(string[] args, out string infilename, out string outfilename)
        {
            infilename = null;
            outfilename = "hksasm.out";
            if (args.Length != 1 && args.Length != 2)
            {
                Console.Error.WriteLine(usage);
                return false;
            }

            infilename = args[0];
            if (args.Length == 2)
            {
                outfilename = args[1];
            }
            return true;
        }
        static int Main(string[] args)
        {
            if (!ParseArgs(args, out string infilename, out string outfilename))
            {
                return 1;
            }
            var assembler = new HksAssembler(infilename);
            assembler.Assemble(outfilename);
            return 0;
        }
    }
}
