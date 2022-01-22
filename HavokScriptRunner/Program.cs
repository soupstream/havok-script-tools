using System;
using System.IO;
using HavokScriptToolsCommon;

namespace HavokScriptRunner
{
    class Program
    {
        const string usage = "usage: hks <filename>";
        static bool ParseArgs(string[] args, out string filename, out bool interactive)
        {
            filename = null;
            interactive = false;

            if (args.Length == 0)
            {
                interactive = true;
            }
            else if (args.Length == 1)
            {
                filename = args[0];
            }
            else
            {
                Console.Error.WriteLine(usage);
                return false;
            }

            return true;
        }

        static int RunInteractive(Hks hks)
        {
            string input = null;
            do
            {
                Console.Write("> ");
                input = Console.ReadLine();
                hks.Dostring(input);
            } while (input != "exit");
            return 0;
        }

        static int Main(string[] args)
        {
            if (!ParseArgs(args, out string filename, out bool interactive))
            {
                return 1;
            }

            if (filename != null && !File.Exists(filename))
            {
                Console.Error.WriteLine("error: no such file: " + filename);
                return 1;
            }

            Hks hks = new Hks();

            int err = 0;
            if (filename != null)
            {
                err = hks.Dofile(filename);
            }

            if (interactive)
            {
                hks.Dostring("function dir(o) for k, v in pairs(o) do print(k) end end");
                hks.Dostring("print(\"Havok Script \" .. _VERSION)");
                err = RunInteractive(hks);
            }

            return err;
        }
    }
}
