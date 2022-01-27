using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Globalization;

namespace HavokScriptToolsCommon
{
    public class HksAssembler
    {
        string filename;

        public HksAssembler(string filename)
        {
            this.filename = filename;
        }

        public byte[] Assemble(string outfile)
        {
            return null;
        }

        private List<List<string>> Lex()
        {
            // language=regex
            string instructionExp = "([RK]\\()|([,);])| "; // register/constant | separators | spaces
            // language=regex
            string directiveExp = "(\"[^\"]*\")|([,;])| "; // string | separators | spaces

            List<List<string>> lex = new();
            foreach (string fileLine in File.ReadLines(filename))
            {
                string line = fileLine.Trim();
                string[] split;
                if (line.StartsWith("."))
                {
                    split = Regex.Split(line, directiveExp);
                }
                else
                {
                    split = Regex.Split(line, instructionExp);
                }

                List<string> lineLex = new();
                foreach (string token in split)
                {
                    if (token  != "")
                    {
                        lineLex.Add(token);
                    }
                    else if (token == ";")
                    {
                        break;
                    }
                }
                if (lineLex.Count > 0)
                {
                    lex.Add(lineLex);
                }
            }
            return lex;
        }


        private HksStructure Parse(List<List<string>> lex)
        {
            int cursor = 0;
            HksHeader header = ParseHeader(lex, ref cursor)
            return new HksStructure();
        }

        private HksHeader ParseHeader(List<List<string>> lex, ref int cursor)
        {
            var foundDirectives = new Dictionary<string, string[]>();
            for (int i = 0; i < 8; i++, cursor++)
            {
                var line = lex[i];
                string directive = line[0];
                string[] args = line.Skip(1).ToArray();
                HksAssemblyException.Assert(!foundDirectives.ContainsKey(directive), "Duplicate directive: " + directive);
                foundDirectives[directive] = args;
            }

            // TODO: error handling
            HksEndianness endianness = (HksEndianness)Enum.Parse(typeof(HksEndianness), foundDirectives[".endianness"][0], true);
            byte intSize = byte.Parse(foundDirectives[".int_size"][0]);
            byte size_tSize = byte.Parse(foundDirectives[".size_t_size"][0]);
            byte instructionSize = byte.Parse(foundDirectives[".instruction_size"][0]);
            byte numberSize = byte.Parse(foundDirectives[".number_size"][0]);
            HksNumberType numberType = (HksNumberType)Enum.Parse(typeof(HksNumberType), foundDirectives[".number_type"][0]);
            byte flags = byte.Parse(foundDirectives[".flags"][0][2..], NumberStyles.HexNumber);
            byte unk = byte.Parse(foundDirectives[".unk"][0]);

            byte[] signature = { 0x1b, 0x4c, 0x75, 0x61 }; // "\x1bLua"
            return new HksHeader(signature, 0x51, 14, endianness, intSize, size_tSize, instructionSize, numberSize, numberType, flags, unk);
        }



        private HksFunctionBlock ParseFunction(List<List<string>> lex)
        {

        }

        private HksStructBlock ParseStruct(List<List<string>> lex)
        {

        }
    }

    public class HksAssemblyException : Exception
    {
        public HksAssemblyException() { }
        public HksAssemblyException(string message) : base(message) { }
        public HksAssemblyException(string message, Exception innerException) : base(message, innerException) { }
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new HksAssemblyException(message);
            }
        }
    }
}
