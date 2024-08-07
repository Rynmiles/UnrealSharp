﻿using System;

namespace UnrealSharpScriptGenerator;

[Flags]
public enum EPropertyUsageFlags : byte
{
    None = 0x00,
    Property = 0x01,
    Parameter = 0x02,
    ReturnValue = 0x04,
    Inner = 0x08,
    StructProperty = 0x10,
    OverridableFunctionParameter = 0x20,
    OverridableFunctionReturnValue = 0x40,
    StaticArrayProperty = 0x80,
    Any = 0xFF,
};
