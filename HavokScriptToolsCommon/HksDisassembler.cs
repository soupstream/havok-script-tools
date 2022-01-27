using System;
using System.Collections.Generic;
using System.Text;

namespace HavokScriptToolsCommon
{
    public class HksDisassembler
    {
        private readonly BinaryReader reader;
        HksHeader? globalHeader;

        public HksDisassembler(byte[] bytecode)
        {
            reader = new BinaryReader(bytecode);
        }

        public string Disassemble()
        {
            HksStructure structure = ReadStructure();
            StringBuilder sb = new();
            sb.AppendFormat(".endianness {0}\n", structure.Header.Endianness.ToString().ToLower());
            sb.AppendFormat(".int_size {0}\n", structure.Header.IntSize);
            sb.AppendFormat(".size_t_size {0}\n", structure.Header.Size_tSize);
            sb.AppendFormat(".instruction_size {0}\n", structure.Header.InstructionSize);
            sb.AppendFormat(".number_size {0}\n", structure.Header.NumberSize);
            sb.AppendFormat(".number_type {0}\n", structure.Header.NumberType.ToString().ToLower());
            sb.AppendFormat(".flags 0x{0:x}\n", structure.Header.Flags);
            sb.AppendFormat(".unk 0x{0:x}\n\n", structure.Header.Unk);
            DisassembleFunctions(structure.Functions, sb);
            DisassembleStructs(structure.Structs, sb);
            return sb.ToString();
        }

        private void DisassembleFunctions(List<HksFunctionBlock> functions, StringBuilder sb)
        {
            foreach (HksFunctionBlock function in functions)
            {
                //function header
                string functionName = "";
                if (function.DebugInfo is HksFunctionDebugInfo debugInfo)
                {
                    functionName = debugInfo.Name;
                    if (debugInfo.Path.Length != 0)
                    {
                        sb.AppendFormat(".path {0}\n", debugInfo.Path);
                    }
                }

                if (functionName == "")
                {
                    functionName = string.Format("FUNC_{0:X8}", function.Address);
                }
                sb.AppendFormat(".function {0}\n", functionName);
                sb.AppendFormat(".level {0}\n", function.Level);
                sb.AppendFormat(".param_count {0}\n", function.ParamCount);
                sb.AppendFormat(".unk1 {0}\n", function.Unk1);
                sb.AppendFormat(".slot_count {0}\n", function.SlotCount);
                sb.AppendFormat(".unk2 {0}\n", function.Unk2);
                sb.AppendFormat(".function_count {0}\n", function.FunctionCount);

                // constants
                List<string> constantStrs = new List<string>();
                for (int i = 0; i < function.Constants.Count; i++)
                {
                    object? value = function.Constants[i].Value;
                    if (value is null)
                    {
                        value = "nil";
                    }
                    else if (value is string str)
                    {
                        value = "\"" + str + "\"";
                    }
                    constantStrs.Add(value.ToString()!);
                    sb.AppendFormat(".constant {0} ; {1}\n", value, i);
                }

                if (function.DebugInfo is HksFunctionDebugInfo debugInfo2)
                {
                    foreach (HksDebugLocal local in debugInfo2.Locals)
                    {
                        sb.AppendFormat(".local {0}, {1}, {2}\n", local.Name, local.Start, local.End);
                    }

                    foreach (string upvalue in debugInfo2.Upvalues)
                    {
                        sb.AppendFormat(".upvalue {0}\n", upvalue);
                    }
                }

                // instructions
                for (int i = 0; i < function.Instructions.Count; i++)
                {
                    if (function.DebugInfo is HksFunctionDebugInfo debugInfo1)
                    {
                        sb.AppendFormat("[{0}] ", debugInfo1.Lines[i]);
                    }
                    HksInstruction instruction = function.Instructions[i];
                    sb.AppendFormat("{0} ", instruction.OpCode.ToString());
                    for (int j = 0; j < instruction.Args.Count; j++)
                    {
                        HksOpArg arg = instruction.Args[j];
                        if (arg.Mode == HksOpArgMode.CONST)
                        {
                            sb.AppendFormat("K({0})={1}", arg.Value, constantStrs[arg.Value]);
                        }
                        else if (arg.Mode == HksOpArgMode.REG)
                        {
                            sb.AppendFormat("R({0})", arg.Value);
                        }
                        else
                        {
                            sb.Append(arg.Value);
                        }
                        if (j < instruction.Args.Count - 1)
                        {
                            sb.Append(", ");
                        }
                    }
                    sb.Append('\n');
                }

                sb.Append('\n');
            }
        }

        private void DisassembleStructs(List<HksStructBlock> structs, StringBuilder sb)
        {
            foreach (HksStructBlock struct_ in structs)
            {
                sb.AppendFormat(".struct {0}\n", struct_.Header.Name);
                sb.AppendFormat(".struct_id {0}\n", struct_.Header.StructId);
                if (struct_.ExtendedStructs is List<string> extendedStructs)
                {
                    foreach (string extendedStruct in extendedStructs)
                    {
                        sb.AppendFormat(".extends {0}\n", extendedStruct);
                    }
                }
                sb.AppendLine();
                foreach (HksStructMember member in struct_.Members)
                {
                    sb.AppendFormat(".member {0}\n", member.Header.Name);
                    sb.AppendFormat(".member_id {0}\n", member.Header.StructId);
                    sb.AppendFormat(".member_type {0}\n", member.Header.Type);
                    sb.AppendFormat(".member_index {0}\n", member.Index);
                }
                sb.AppendLine();
            }
        }

        public HksStructure ReadStructure()
        {
            globalHeader = ReadHeader();
            reader.SetLittleEndian(globalHeader.Endianness == HksEndianness.LITTLE);
            var typeEnum = ReadTypeEnum();
            var functions = ReadFunctions();
            var unk = reader.ReadInt32();
            var structs = ReadStructs();
            return new HksStructure(globalHeader, typeEnum, functions, unk, structs);
        }

        private HksHeader ReadHeader()
        {
            var signature = reader.ReadBytes(4);
            byte[] expectedSignature = { 0x1b, 0x4c, 0x75, 0x61 }; // "\x1bLua"
            HksDisassemblyException.Assert(Util.ArraysEqual(signature, expectedSignature), "invalid signature");
            var version = reader.ReadUInt8();
            HksDisassemblyException.Assert(version == 0x51, "invalid Lua version");
            var format = reader.ReadUInt8();
            HksDisassemblyException.Assert(format == 14, "invalid Lua format version");
            var endianness = (HksEndianness)reader.ReadUInt8();
            var intSize = reader.ReadUInt8();
            var size_tSize = reader.ReadUInt8();
            var instructionSize = reader.ReadUInt8();
            var numberSize = reader.ReadUInt8();
            var numberType = (HksNumberType)reader.ReadUInt8();
            var flags = reader.ReadUInt8();
            var unk = reader.ReadUInt8();
            return new HksHeader(signature, version, format, endianness, intSize, size_tSize, instructionSize, numberSize, numberType, flags, unk);
        }

        private HksTypeEnum ReadTypeEnum()
        {
            var count = reader.ReadUInt32();
            var entries = new List<HksTypeEnumEntry>();
            for (var i = 0; i < count; i++)
            {
                var value = reader.ReadInt32();
                int length = reader.ReadInt32();
                string name = "";
                if (length != 0)
                {
                    name = reader.ReadString(length - 1);
                    reader.Skip(1);
                }
                entries.Add(new HksTypeEnumEntry(value, name));
            }
            return new HksTypeEnum(count, entries);
        }

        private List<HksFunctionBlock> ReadFunctions()
        {
            var functions = new List<HksFunctionBlock>();
            uint functionCount = 1;
            while (functionCount > 0)
            {
                var address = reader.GetPosition();

                var level = reader.ReadInt32();
                var paramCount = reader.ReadUInt32();
                var unk1 = reader.ReadInt8();
                var slotCount = reader.ReadUInt32();
                var unk2 = reader.ReadInt32();
                var instructionCount = reader.ReadUInt32();
                var instructions = new List<HksInstruction>();
                reader.Pad(4);
                for (var i = 0; i < instructionCount; i++)
                {
                    instructions.Add(ReadInstruction());
                }

                var constantCount = reader.ReadUInt32();
                var constants = new List<HksValue>();
                for (var i = 0; i < constantCount; i++)
                {
                    constants.Add(ReadValue());
                }

                var hasDebugInfo = reader.ReadInt32();
                HksFunctionDebugInfo? debugInfo = null;
                if (hasDebugInfo != 0)
                {
                    var lineCount = reader.ReadUInt32();
                    var localsCount = reader.ReadUInt32();
                    var upvalueCount = reader.ReadUInt32();
                    var lineBegin = reader.ReadUInt32();
                    var lineEnd = reader.ReadUInt32();
                    var path = ReadString();
                    var functionName = ReadString();
                    var lines = new List<int>();
                    for (var i = 0; i < lineCount; i++)
                    {
                        lines.Add(reader.ReadInt32());
                    }
                    var locals = new List<HksDebugLocal>();
                    for (var i = 0; i < localsCount; i++)
                    {
                        var localName = ReadString();
                        var start = reader.ReadInt32();
                        var end = reader.ReadInt32();
                        locals.Add(new HksDebugLocal(localName, start, end));
                    }
                    var upvalues = new List<string>();
                    for (var i = 0; i < upvalueCount; i++)
                    {
                        upvalues.Add(ReadString());
                    }
                    debugInfo = new HksFunctionDebugInfo(lineCount, localsCount, upvalueCount, lineBegin, lineEnd, path, functionName, lines, locals, upvalues);
                }

                var childFunctionCount = reader.ReadUInt32();

                var function = new HksFunctionBlock(level, paramCount, unk1, slotCount, unk2, instructionCount, instructions, constantCount, constants, hasDebugInfo, debugInfo, childFunctionCount)
                {
                    Address = address
                };
                functions.Add(function);
                functionCount += childFunctionCount;
                functionCount--;
            }

            return functions;
        }

        private string ReadString()
        {
            int size;
            if (globalHeader!.Size_tSize == 4)
            {
                size = reader.ReadInt32();
            }
            else
            {
                size = (int)reader.ReadInt64();
            }
            string str = "";
            if (size != 0)
            {
                str = reader.ReadString(size - 1);
                reader.Skip(1);
            }
            return str;
        }

        private HksValue ReadValue()
        {
            var type = (HksType)reader.ReadInt8();
            object? value;
            switch (type)
            {
                case HksType.TNIL:
                    value = null;
                    break;
                case HksType.TBOOLEAN:
                    value = reader.ReadInt8();
                    break;
                case HksType.TNUMBER:
                    if (globalHeader!.NumberSize == 4)
                    {
                        if (globalHeader.NumberType == HksNumberType.FLOAT)
                        {
                            value = reader.ReadFloat();
                        }
                        else
                        {
                            value = reader.ReadInt32();
                        }
                    }
                    else if (globalHeader.NumberSize == 8)
                    {
                        if (globalHeader.NumberType == HksNumberType.FLOAT)
                        {
                            value = reader.ReadDouble();
                        }
                        else
                        {
                            value = reader.ReadInt64();
                        }
                    }
                    else
                    {
                        throw new HksDisassemblyException("unknown number size: " + globalHeader.NumberSize);
                    }
                    break;
                case HksType.TSTRING:
                    value = ReadString();
                    break;
                case HksType.TLIGHTUSERDATA:
                case HksType.TTABLE:
                case HksType.TFUNCTION:
                case HksType.TUSERDATA:
                case HksType.TTHREAD:
                case HksType.TIFUNCTION:
                case HksType.TCFUNCTION:
                case HksType.TUI64:
                case HksType.TSTRUCT:
                default:
                    throw new HksDisassemblyException("type not implemented: " + type.ToString());
            }
            return new HksValue(type, value);
        }

        private HksInstruction ReadInstruction()
        {
            uint raw = reader.ReadUInt32();
            var opCode = (HksOpCode)(raw >> 25);
            var args = new List<HksOpArg>();

            var opModes = HksOpInfo.opModes[(int)opCode];

            if (opModes.opArgModeA == HksOpArgModeA.REG)
            {
                var mode = HksOpArgMode.REG;
                int value = (int)raw & 0xff;
                args.Add(new HksOpArg(mode, value));
            }

            if (opModes.opMode == HksOpMode.iABC)
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    HksOpArgMode mode;
                    uint value;
                    switch (opModes.opArgModeB)
                    {
                        case HksOpArgModeBC.NUMBER:
                            mode = HksOpArgMode.NUMBER;
                            value = (raw >> 17) & 0xff;
                            break;
                        case HksOpArgModeBC.OFFSET:
                            mode = HksOpArgMode.NUMBER;
                            value = (raw >> 17) & 0x1ff;
                            break;
                        case HksOpArgModeBC.REG:
                            mode = HksOpArgMode.REG;
                            value = (raw >> 17) & 0xff;
                            break;
                        case HksOpArgModeBC.REG_OR_CONST:
                            value = (raw >> 17) & 0x1ff;
                            if (value < 0x100)
                            {
                                mode = HksOpArgMode.REG;
                            }
                            else
                            {
                                mode = HksOpArgMode.CONST;
                                value &= 0xff;
                            }
                            break;
                        case HksOpArgModeBC.CONST:
                            mode = HksOpArgMode.CONST;
                            value = (raw >> 17) & 0xff;
                            break;
                        default:
                            throw new HksDisassemblyException("this shouldn't happen");
                    }
                    args.Add(new HksOpArg(mode, (int)value));
                }

                if (opModes.opArgModeC != HksOpArgModeBC.UNUSED)
                {
                    HksOpArgMode mode;
                    uint value;
                    switch (opModes.opArgModeC)
                    {
                        case HksOpArgModeBC.NUMBER:
                            mode = HksOpArgMode.NUMBER;
                            value = (raw >> 8) & 0xff;
                            break;
                        case HksOpArgModeBC.OFFSET:
                            mode = HksOpArgMode.NUMBER;
                            value = (raw >> 8) & 0x1ff;
                            break;
                        case HksOpArgModeBC.REG:
                            mode = HksOpArgMode.REG;
                            value = (raw >> 8) & 0xff;
                            break;
                        case HksOpArgModeBC.REG_OR_CONST:
                            value = (raw >> 8) & 0x1ff;
                            if (value < 0x100)
                            {
                                mode = HksOpArgMode.REG;
                            }
                            else
                            {
                                mode = HksOpArgMode.CONST;
                                value &= 0xff;
                            }
                            break;
                        case HksOpArgModeBC.CONST:
                            mode = HksOpArgMode.CONST;
                            value = (raw >> 8) & 0xff;
                            break;
                        default:
                            throw new HksDisassemblyException("this shouldn't happen");
                    }
                    args.Add(new HksOpArg(mode, (int)value));
                }
            }
            else
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    int value = (int)(raw >> 8) & 0x1ffff;
                    if (opModes.opMode == HksOpMode.iAsBx)
                    {
                        value -= 0xffff;
                    }

                    var mode = opModes.opArgModeB switch
                    {
                        HksOpArgModeBC.NUMBER or HksOpArgModeBC.OFFSET => HksOpArgMode.NUMBER,
                        HksOpArgModeBC.CONST => HksOpArgMode.CONST,
                        _ => throw new HksDisassemblyException("unexpected op arg mode: " + opModes.opArgModeB),
                    };
                    args.Add(new HksOpArg(mode, value));
                }
            }

            return new HksInstruction(opCode, args);
        }

        private List<HksStructBlock> ReadStructs()
        {
            var structs = new List<HksStructBlock>();
            while (reader.ReadInt64() != 0)
            {
                reader.Skip(-sizeof(long));
                var header = ReadStructHeader();
                var memberCount = reader.ReadInt32();
                int? extendCount = null;
                List<string>? extendedStructs = null;
                if (!globalHeader!.NoMemberExtensions)
                {
                    extendCount = reader.ReadInt32();
                    extendedStructs = new List<string>();
                    for (var i = 0; i < extendCount; i++)
                    {
                        extendedStructs.Add(ReadString());
                    }
                }
                var members = new List<HksStructMember>();
                for (var i = 0; i < memberCount; i++)
                {
                    members.Add(ReadStructMember());
                }
                structs.Add(new HksStructBlock(header, memberCount, extendCount, extendedStructs, members));
            }
            return structs;
        }

        private HksStructHeader ReadStructHeader()
        {
            var name = ReadString();
            var unk0 = reader.ReadInt32();
            var unk1 = reader.ReadInt16();
            var structId = reader.ReadInt16();
            var type = (HksType)reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            var unk3 = reader.ReadInt32();
            return new HksStructHeader(name, unk0, unk1, structId, type, unk2, unk3);
        }

        private HksStructMember ReadStructMember()
        {
            var header = ReadStructHeader();
            var index = reader.ReadInt32();
            return new HksStructMember(header, index);
        }
    }

    public class HksDisassemblyException : Exception
    {
        public HksDisassemblyException() { }
        public HksDisassemblyException(string message) : base(message) { }
        public HksDisassemblyException(string message, Exception innerException) : base(message, innerException) { }
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new HksDisassemblyException(message);
            }
        }
    }
}
