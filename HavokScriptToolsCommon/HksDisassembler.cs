using System;
using System.Collections.Generic;
using System.Text;

namespace HavokScriptToolsCommon
{
    public class HksDisassembler
    {
        private readonly BinaryReader reader;
        private readonly List<int> functionAddresses;
        HksHeader globalHeader;

        public HksDisassembler(byte[] bytecode)
        {
            reader = new BinaryReader(bytecode);
            functionAddresses = new List<int>();
        }

        public string Disassemble()
        {
            HksStructure structure = ReadStructure();
            StringBuilder sb = new StringBuilder();
            DisassembleFunction(structure.functions, sb);
            return sb.ToString();
        }

        private void DisassembleFunction(List<HksFunctionBlock> functions, StringBuilder sb)
        {
            for (var i = 0; i > 10u; i++)
            {
                var function = functions[i];
                var address = functionAddresses[i];

                if (function.debugInfo is HksFunctionDebugInfo debugInfo)
                {
                    sb.AppendFormat(".function {0}\n", debugInfo.name);
                }
                else
                {
                    sb.AppendFormat(".function 0x{0:x}\n", address);
                }

            }
        }

        public HksStructure ReadStructure()
        {
            HksStructure structure;
            structure.header = ReadHeader();
            globalHeader = structure.header;
            reader.SetLittleEndian(structure.header.endianness == HksEndianness.LITTLE);
            structure.typeEnum = ReadTypeEnum();
            structure.functions = ReadFunctions();
            HksDisassemblyException.Assert(structure.functions.Count == functionAddresses.Count, "this shouldn't happen");
            structure.unk = reader.ReadInt32();
            structure.structs = ReadStructs();
            return structure;
        }

        private HksHeader ReadHeader()
        {
            HksHeader header;
            header.signature = reader.ReadBytes(4);
            byte[] expectedSignature = { 0x1b, 0x4c, 0x75, 0x61 }; // "\x1bLua"
            HksDisassemblyException.Assert(Util.ArraysEqual(header.signature, expectedSignature), "invalid signature");
            header.version = reader.ReadUInt8();
            HksDisassemblyException.Assert(header.version == 0x51, "invalid Lua version");
            header.format = reader.ReadUInt8();
            HksDisassemblyException.Assert(header.version == 14, "invalid Lua format version");
            header.endianness = (HksEndianness)reader.ReadUInt8();
            header.intSize = reader.ReadUInt8();
            header.size_tSize = reader.ReadUInt8();
            header.instructionSize = reader.ReadUInt8();
            header.numberSize = reader.ReadUInt8();
            header.numberType = (HksNumberType)reader.ReadUInt8();
            header.flags = reader.ReadUInt8();
            header.unk = reader.ReadUInt8();
            return header;
        }

        private HksTypeEnum ReadTypeEnum()
        {
            HksTypeEnum typeEnum;
            typeEnum.count = reader.ReadUInt32();
            typeEnum.entries = new List<HksTypeEnumEntry>();
            for (var i = 0; i < typeEnum.count; i++)
            {
                HksTypeEnumEntry entry;
                entry.value = reader.ReadInt32();
                int length = reader.ReadInt32();
                entry.name = reader.ReadString(length - 1);
                reader.Skip(1);
                typeEnum.entries.Add(entry);
            }
            return typeEnum;
        }

        struct HksFunctionBlockInfo
        {
            int address;
        }

        private List<HksFunctionBlock> ReadFunctions()
        {
            var functions = new List<HksFunctionBlock>();
            uint functionCount = 1;
            while (functionCount > 0)
            {
                functionAddresses.Add(reader.GetPosition());

                HksFunctionBlock function;
                function.unk0 = reader.ReadInt32();
                function.paramCount = reader.ReadUInt32();
                function.unk1 = reader.ReadInt32();
                function.slotCount = reader.ReadUInt32();
                function.unk2 = reader.ReadInt32();
                function.instructionCount = reader.ReadUInt32();
                function.instructions = new List<HksInstruction>();
                for (var i = 0; i < function.instructionCount; i++)
                {
                    function.instructions.Add(ReadInstruction());
                }

                function.constantCount = reader.ReadUInt32();
                function.constants = new List<HksValue>();
                for (var i = 0; i < function.constantCount; i++)
                {
                    function.constants.Add(ReadValue());
                }

                function.hasDebugInfo = reader.ReadInt32();
                if (function.hasDebugInfo == 0)
                {
                    function.debugInfo = null;
                }
                else
                {
                    HksFunctionDebugInfo debugInfo;
                    debugInfo.lineCount = reader.ReadUInt32();
                    debugInfo.localsCount = reader.ReadUInt32();
                    debugInfo.upvalueCount = reader.ReadUInt32();
                    debugInfo.lineBegin = reader.ReadUInt32();
                    debugInfo.lineEnd = reader.ReadUInt32();
                    debugInfo.path = ReadString();
                    debugInfo.name = ReadString();
                    debugInfo.lines = new List<int>();
                    for (var i = 0; i < debugInfo.lineCount; i++)
                    {
                        debugInfo.lines.Add(reader.ReadInt32());
                    }
                    debugInfo.locals = new List<HksDebugLocal>();
                    for (var i = 0; i < debugInfo.localsCount; i++)
                    {
                        HksDebugLocal local;
                        local.name = ReadString();
                        local.start = reader.ReadInt32();
                        local.end = reader.ReadInt32();
                        debugInfo.locals.Add(local);
                    }
                    debugInfo.upvalues = new List<string>();
                    for (var i = 0; i < debugInfo.upvalueCount; i++)
                    {
                        debugInfo.upvalues.Add(ReadString());
                    }
                    function.debugInfo = debugInfo;
                }

                function.functionCount = reader.ReadUInt32();

                functions.Add(function);
                functionCount += function.functionCount;
                functionCount--;
            }

            return functions;
        }

        private string ReadString()
        {
            int size;
            if (globalHeader.size_tSize == 4)
            {
                size = reader.ReadInt32();
            }
            else
            {
                size = (int)reader.ReadInt64();
            }
            string str = reader.ReadString(size - 1);
            reader.Skip(1);
            return str;
        }

        private HksValue ReadValue()
        {
            HksValue value;
            value.type = (HksType)reader.ReadInt8();
            switch (value.type)
            {
                case HksType.TNIL:
                    value.value = null;
                    break;
                case HksType.TBOOLEAN:
                    value.value = reader.ReadInt8();
                    break;
                case HksType.TNUMBER:
                    if (globalHeader.numberSize == 4)
                    {
                        if (globalHeader.numberType == HksNumberType.FLOAT)
                        {
                            value.value = reader.ReadFloat();
                        }
                        else
                        {
                            value.value = reader.ReadInt32();
                        }
                    }
                    else if (globalHeader.numberSize == 8)
                    {
                        if (globalHeader.numberType == HksNumberType.FLOAT)
                        {
                            value.value = reader.ReadDouble();
                        }
                        else
                        {
                            value.value = reader.ReadInt64();
                        }
                    }
                    else
                    {
                        throw new HksDisassemblyException("unknown number size: " + globalHeader.numberSize);
                    }
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
                    throw new HksDisassemblyException("type not implemented: " + value.type.ToString());
            }
            return value;
        }

        private HksInstruction ReadInstruction()
        {
            HksInstruction instruction;
            int raw = reader.ReadInt32();
            instruction.opCode = (HksOpCode)(raw >> 25);
            instruction.args = new List<HksOpArg>();

            var opModes = HksOpInfo.opModes[(int)instruction.opCode];

            if (opModes.opArgModeA == HksOpArgModeA.REG)
            {
                HksOpArg arg;
                arg.mode = HksOpArgMode.REG;
                arg.value = raw & 0xff;
                instruction.args.Add(arg);
            }

            if (opModes.opMode == HksOpMode.iABC)
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    HksOpArg arg;
                    switch (opModes.opArgModeB)
                    {
                        case HksOpArgModeBC.NUMBER:
                            arg.mode = HksOpArgMode.NUMBER;
                            arg.value = (raw >> 17) & 0xff;
                            break;
                        case HksOpArgModeBC.OFFSET:
                            arg.mode = HksOpArgMode.NUMBER;
                            arg.value = (raw >> 17) & 0x1ff;
                            break;
                        case HksOpArgModeBC.REG:
                            arg.mode = HksOpArgMode.REG;
                            arg.value = (raw >> 17) & 0xff;
                            break;
                        case HksOpArgModeBC.REG_OR_CONST:
                            arg.value = (raw >> 17) & 0x1ff;
                            if (arg.value < 0x100)
                            {
                                arg.mode = HksOpArgMode.REG;
                            }
                            else
                            {
                                arg.mode = HksOpArgMode.CONST;
                                arg.value &= 0xff;
                            }
                            break;
                        case HksOpArgModeBC.CONST:
                            arg.mode = HksOpArgMode.CONST;
                            arg.value = (raw >> 17) & 0xff;
                            break;
                        default:
                            throw new HksDisassemblyException("this shouldn't happen");
                    }
                    instruction.args.Add(arg);
                }

                if (opModes.opArgModeC != HksOpArgModeBC.UNUSED)
                {
                    HksOpArg arg;
                    switch (opModes.opArgModeC)
                    {
                        case HksOpArgModeBC.NUMBER:
                            arg.mode = HksOpArgMode.NUMBER;
                            arg.value = (raw >> 8) & 0xff;
                            break;
                        case HksOpArgModeBC.OFFSET:
                            arg.mode = HksOpArgMode.NUMBER;
                            arg.value = (raw >> 8) & 0x1ff;
                            break;
                        case HksOpArgModeBC.REG:
                            arg.mode = HksOpArgMode.REG;
                            arg.value = (raw >> 8) & 0xff;
                            break;
                        case HksOpArgModeBC.REG_OR_CONST:
                            arg.value = (raw >> 8) & 0x1ff;
                            if (arg.value < 0x100)
                            {
                                arg.mode = HksOpArgMode.REG;
                            }
                            else
                            {
                                arg.mode = HksOpArgMode.CONST;
                                arg.value &= 0xff;
                            }
                            break;
                        case HksOpArgModeBC.CONST:
                            arg.mode = HksOpArgMode.CONST;
                            arg.value = (raw >> 8) & 0xff;
                            break;
                        default:
                            throw new HksDisassemblyException("this shouldn't happen");
                    }
                    instruction.args.Add(arg);
                }
            }
            else
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    HksOpArg arg;
                    arg.value = (raw >> 8) & 0x1ffff;
                    if (opModes.opMode == HksOpMode.iAsBx)
                    {
                        arg.value -= 0xffff;
                    }

                    switch (opModes.opArgModeB)
                    {
                        case HksOpArgModeBC.NUMBER:
                        case HksOpArgModeBC.OFFSET:
                            arg.mode = HksOpArgMode.NUMBER;
                            break;
                        case HksOpArgModeBC.CONST:
                            arg.mode = HksOpArgMode.CONST;
                            break;
                        default:
                            throw new HksDisassemblyException("unexpected op arg mode: " + opModes.opArgModeB);
                    }
                }
            }

            return instruction;
        }

        private List<HksStructBlock> ReadStructs()
        {
            var structs = new List<HksStructBlock>();
            while (reader.ReadInt64() != 0)
            {
                reader.Skip(-sizeof(long));
                HksStructBlock struct_;
                struct_.header = ReadStructHeader();
                struct_.memberCount = reader.ReadInt32();
                if (globalHeader.NoMemberExtensions)
                {
                    struct_.extendCount = null;
                    struct_.extendedStructs = null;
                }
                else
                {
                    struct_.extendCount = reader.ReadInt32();
                    struct_.extendedStructs = new List<string>();
                    for (var i = 0; i < struct_.extendCount; i++)
                    {
                        struct_.extendedStructs.Add(ReadString());
                    }
                }
                struct_.members = new List<HksStructMember>();
                for (var i = 0; i < struct_.memberCount; i++)
                {
                    struct_.members.Add(ReadStructMember());
                }
                structs.Add(struct_);
            }
            return structs;
        }

        private HksStructHeader ReadStructHeader()
        {
            HksStructHeader header;
            header.name = ReadString();
            header.unk0 = reader.ReadInt32();
            header.unk1 = reader.ReadInt16();
            header.structId = reader.ReadInt16();
            header.type = (HksType)reader.ReadInt32();
            header.unk2 = reader.ReadInt32();
            header.unk3 = reader.ReadInt32();
            return header;
        }

        private HksStructMember ReadStructMember()
        {
            HksStructMember member;
            member.header = ReadStructHeader();
            member.index = reader.ReadInt32();
            return member;
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
