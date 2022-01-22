using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using HavokScriptToolsCommon;

namespace HavokScriptCompiler
{
    class Program
    {
        const string usage = "usage: hksc <filename>";
        static bool ParseArgs(string[] args, out string infilename, out string outfilename, out bool assembly)
        {
            infilename = null;
            outfilename = "hksc.out";
            assembly = false;

            List<string> positionalArgs = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-l")
                {
                    assembly = true;
                }
                else
                {
                    positionalArgs.Add(args[i]);
                }
            }

            if (positionalArgs.Count != 1 && positionalArgs.Count != 2)
            {
                Console.Error.WriteLine(usage);
                return false;
            }

            infilename = positionalArgs[0];
            if (positionalArgs.Count == 2)
            {
                outfilename = positionalArgs[1];
            }
            return true;
        }

        static int Main(string[] args)
        {
            if (!ParseArgs(args, out string infilename, out string outfilename, out bool assembly))
            {
                return 1;
            }

            if (!File.Exists(infilename))
            {
                Console.Error.WriteLine("error: no such file: " + infilename);
                return 1;
            }

            Hks hks = new Hks();
            int err = hks.Loadfile(infilename);
            if (err != 0)
            {
                Console.Error.WriteLine("error: LoadFile returned " + err);
                return 1;
            }

            if (assembly)
            {
                string script = string.Join("\n", new string[]{
                    "local env = {}",
                    "chunk = loadfile(\"" + infilename.Replace("\\", "\\\\") + "\")",
                    "setfenv(chunk, env)",
                    "pcall(chunk)",
                    "print(\"(main chunk)\")",
                    "print(debug.inspect(chunk).disassembly)",
                    "for k, v in pairs(env) do",
                        "if type(v) == \"function\" and debug.getinfo(v).what == \"Lua\" then",
                            "print(k)",
                            "print(debug.inspect(v).disassembly)",
                        "end",
                    "end"
                });
                err = hks.Dostring(script);
            }
            else
            {
                err = hks.Dump(outfilename);
            }
            if (err != 0)
            {
                Console.Error.WriteLine("error: Dump returned " + err);
                return 1;
            }
            return 0;
        }
    }
}
