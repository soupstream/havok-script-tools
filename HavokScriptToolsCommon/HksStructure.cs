using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace HavokScriptToolsCommon
{
    public struct HksStructure
    {
        public HksHeader header;
        public HksTypeEnum typeEnum;
        public List<HksFunctionBlock> functions;
        public int unk;
        public List<HksStructBlock> structs;
    }

    public struct HksHeader
    {
        public byte[] signature;
        public byte version;
        public byte format;
        public HksEndianness endianness;
        public byte intSize;
        public byte size_tSize;
        public byte instructionSize;
        public byte numberSize;
        public HksNumberType numberType;
        public byte flags;
        public byte unk;

        public bool UnkFlag0
        {
            get { return Util.GetBit(flags, 0); }
            set { Util.SetBit(ref flags, 0, value); }
        }
        public bool UnkFlag1
        {
            get { return Util.GetBit(flags, 1); }
            set { Util.SetBit(ref flags, 1, value); }
        }
        public bool UnkFlag2
        {
            get { return Util.GetBit(flags, 2); }
            set { Util.SetBit(ref flags, 2, value); }
        }
        public bool NoMemberExtensions
        {
            get { return Util.GetBit(flags, 3); }
            set { Util.SetBit(ref flags, 3, value); }
        }
    }

    public struct HksTypeEnum
    {
        public uint count;
        public List<HksTypeEnumEntry> entries;
    }
    public struct HksTypeEnumEntry
    {
        public int value;
        public string name;
    }

    public struct HksFunctionBlock
    {
        public int unk0;
        public uint paramCount;
        public int unk1;
        public uint slotCount;
        public int unk2;
        public uint instructionCount;
        public List<HksInstruction> instructions;
        public uint constantCount;
        public List<HksValue> constants;
        public int hasDebugInfo;
        public HksFunctionDebugInfo? debugInfo;
        public uint functionCount;
    }

    public struct HksFunctionDebugInfo
    {
        public uint lineCount;
        public uint localsCount;
        public uint upvalueCount;
        public uint lineBegin;
        public uint lineEnd;
        public string path;
        public string name;
        public List<int> lines;
        public List<HksDebugLocal> locals;
        public List<string> upvalues;
    }

    public struct HksDebugLocal
    {
        public string name;
        public int start;
        public int end;
    }

    public struct HksInstruction
    {
        public HksOpCode opCode;
        public List<HksOpArg> args;
    }

    public struct HksOpArg
    {
        public HksOpArgMode mode;
        public int value;
    }

    public struct HksStructBlock
    {
        public HksStructHeader header;
        public int memberCount;
        public int? extendCount;
        public List<string>? extendedStructs;
        public List<HksStructMember> members;
    }

    public struct HksStructMember
    {
        public HksStructHeader header;
        public int index;
    }

    public struct HksStructHeader
    {
        public string name;
        public int unk0;
        public short unk1;
        public short structId;
        public HksType type;
        public int unk2;
        public int unk3;
    }

    public struct HksValue
    {
        public HksType type;
        public object? value;
    }

    public enum HksType
    {
        TNIL,
        TBOOLEAN,
        TLIGHTUSERDATA,
        TNUMBER,
        TSTRING,
        TTABLE,
        TFUNCTION,
        TUSERDATA,
        TTHREAD,
        TIFUNCTION,
        TCFUNCTION,
        TUI64,
        TSTRUCT
    }

    public enum HksNumberType
    {
        FLOAT,
        INTEGER
    }

    public enum HksEndianness
    {
        BIG,
        LITTLE
    }
}
