using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace HavokScriptToolsCommon
{
    public record HksStructure
    (
        HksHeader Header,
        HksTypeEnum TypeEnum,
        List<HksFunctionBlock> Functions,
        int Unk,
        List<HksStructBlock> Structs
    );

    public record HksHeader
    (
        byte[] Signature,
        byte Version,
        byte Format,
        HksEndianness Endianness,
        byte IntSize,
        byte Size_tSize,
        byte InstructionSize,
        byte NumberSize,
        HksNumberType NumberType,
        byte Flags,
        byte Unk
    )
    {
        public bool UnkFlag0 => Util.GetBit(Flags, 0);
        public bool UnkFlag1 => Util.GetBit(Flags, 1);
        public bool UnkFlag2 => Util.GetBit(Flags, 2);
        public bool NoMemberExtensions => Util.GetBit(Flags, 3);
    }

    public record HksTypeEnum
    (
        uint Count,
        List<HksTypeEnumEntry> Entries
    );

    public record HksTypeEnumEntry
    (
        int Value,
        string Name
    );

    public record HksFunctionBlock
    (
        int Level,
        uint ParamCount,
        int Unk1,
        uint SlotCount,
        int Unk2,
        uint InstructionCount,
        List<HksInstruction> Instructions,
        uint ConstantCount,
        List<HksValue> Constants,
        int HasDebugInfo,
        HksFunctionDebugInfo? DebugInfo,
        uint FunctionCount
    )
    {
        public int Address { get; set; }
    }

    public record HksFunctionDebugInfo
    (
        uint LineCount,
        uint LocalsCount,
        uint UpvalueCount,
        uint LineBegin,
        uint LineEnd,
        string Path,
        string Name,
        List<int> Lines,
        List<HksDebugLocal> Locals,
        List<string> Upvalues
    );

    public record HksDebugLocal
    (
        string Name,
        int Start,
        int End
    );

    public record HksInstruction
    (
        HksOpCode OpCode,
        List<HksOpArg> Args
    );

    public record HksOpArg
    (
        HksOpArgMode Mode,
        int Value
    );

    public record HksStructBlock
    (
        HksStructHeader Header,
        int MemberCount,
        int? ExtendCount,
        List<string>? ExtendedStructs,
        List<HksStructMember> Members
    );

    public record HksStructMember
    (
        HksStructHeader Header,
        int Index
    );

    public record HksStructHeader
    (
        string Name,
        int Unk0,
        short Unk1,
        short StructId,
        HksType Type,
        int Unk2,
        int Unk3
    );

    public record HksValue
    (
        HksType Type,
        object? Value
    );

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
