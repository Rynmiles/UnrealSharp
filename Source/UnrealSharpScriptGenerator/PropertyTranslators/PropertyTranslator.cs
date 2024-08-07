﻿using System;
using System.Collections.Generic;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public abstract class PropertyTranslator
{
    EPropertyUsageFlags SupportedPropertyUsage;
    
    protected const EPropertyUsageFlags ContainerSupportedUsages = EPropertyUsageFlags.Property | EPropertyUsageFlags.Parameter 
        | EPropertyUsageFlags.ReturnValue 
        | EPropertyUsageFlags.OverridableFunctionParameter 
        | EPropertyUsageFlags.OverridableFunctionReturnValue 
        | EPropertyUsageFlags.StaticArrayProperty;
    
    public bool IsSupportedAsProperty() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Property);
    public bool IsSupportedAsParameter() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Parameter);
    public bool IsSupportedAsReturnValue() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.ReturnValue);
    public bool IsSupportedAsInner() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.Inner);
    public bool IsSupportedAsStructProperty() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.StructProperty);
    public bool IsSupportedAsOverridableFunctionParameter() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.OverridableFunctionParameter);
    public bool IsSupportedAsOverridableFunctionReturnValue() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.OverridableFunctionReturnValue);
    public bool IsSupportedInStaticArray() => SupportedPropertyUsage.HasFlag(EPropertyUsageFlags.StaticArrayProperty);
    
    // Is this property the same memory layout as the C++ type?
    public virtual bool IsBlittable => false;
    public virtual bool NeedSetter => true;
    public virtual bool ExportDefaultParameter => true;
    public virtual bool NeedProperty => true;
    
    public PropertyTranslator(EPropertyUsageFlags supportedPropertyUsage)
    {
        SupportedPropertyUsage = supportedPropertyUsage;
    }
    
    // Can we export this property?
    public abstract bool CanExport(UhtProperty property);
    
    // Get the managed type for this property
    // Example: "int" for a property of type "int32"
    public abstract string GetManagedType(UhtProperty property);
    
    // Get the marshaller for this property to marshal back and forth between C++ and C#
    public abstract string GetMarshaller(UhtProperty property);
    
    // Get the references this property need to work.
    public virtual void GetReferences(UhtProperty property, List<UhtType> references) { }
    
    // Get the marshaller delegates for this property
    public abstract string ExportMarshallerDelegates(UhtProperty property);
    
    // Get the null value for this property
    public abstract string GetNullValue(UhtProperty property);
    
    // Export the static constructor for this property
    public virtual void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
       builder.AppendLine($"{nativePropertyName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName(NativeClassPtr, \"{nativePropertyName}\");");
    }
    
    public virtual void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, UhtFunction function, string propertyEngineName, string functionName)
    {
        builder.AppendLine($"{functionName}_{propertyEngineName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
    }
    
    public virtual void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        builder.AppendLine($"static int {propertyEngineName}_Offset;");
    }
    
    public virtual void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function, string nativeMethodName, UhtProperty property, string propertyEngineName)
    {
        builder.AppendLine($"static int {nativeMethodName}_{propertyEngineName}_Offset;");
    }

    public virtual void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportFromNative(builder, property, propertyManagedName, "return", "NativeObject", $"{propertyManagedName}_Offset", false, false);
    }

    public virtual void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportToNative(builder, property, propertyManagedName, "NativeObject", $"{propertyManagedName}_Offset", "value");
    }

    public virtual void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        
    }

    public virtual void ExportFunctionReturnStatement(GeneratorStringBuilder builder,
        UhtProperty property,
        string nativePropertyName, 
        string functionName, 
        string paramsCallString)
    {
        throw new NotImplementedException();
    }
    
    // Cleanup the marshalling buffer
    public virtual void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property,
        string paramName)
    {

    }
    
    // Build the C# code to marshal this property from C++ to C#
    public abstract void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer,
        bool reuseRefMarshallers);
    
    // Build the C# code to marshal this property from C# to C++
    public abstract void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source);
    
    // Convert a C++ default value to a C# default value
    // Example: "0.0f" for a float property
    public abstract string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter);
    
    public virtual void OnPropertyExported(GeneratorStringBuilder builder, UhtProperty property) {}
    
    public void ExportProperty(GeneratorStringBuilder builder, UhtProperty property, List<string> reservedNames)
    {
        builder.AppendLine();
        builder.TryAddWithEditor(property);
        
        string propertyName = property.GetPropertyName(reservedNames);
        
        ExportPropertyVariables(builder, property, property.EngineName);
        builder.AppendLine();
        
        string protection = property.GetProtection();
        builder.AppendTooltip(property);
        
        string managedType = GetManagedType(property);
        builder.AppendLine($"{protection}{managedType} {propertyName}");
        builder.OpenBrace();

        builder.AppendLine("get");
        builder.OpenBrace();
        ExportPropertyGetter(builder, property, property.EngineName);
        builder.CloseBrace();

        if (NeedSetter && !property.HasAllFlags(EPropertyFlags.BlueprintReadOnly))
        {
            builder.AppendLine("set");
            builder.OpenBrace();
            ExportPropertySetter(builder, property, property.EngineName);
            builder.CloseBrace();
        }
        
        builder.CloseBrace();
        builder.TryEndWithEditor(property);
        builder.AppendLine();
        
        OnPropertyExported(builder, property);
    }

    public void ExportMirrorProperty(GeneratorStringBuilder builder, UhtProperty property, bool suppressOffsets, List<string> reservedNames)
    {
        string propertyScriptName = property.GetPropertyName(reservedNames);
        
        builder.AppendLine($"// {propertyScriptName}");
        builder.AppendLine();
        
        if (!suppressOffsets)
        {
            ExportPropertyVariables(builder, property, property.EngineName);
        }
        
        string protection = property.GetProtection();
        string managedType = GetManagedType(property);
        builder.AppendTooltip(property);
        builder.AppendLine($"{protection}{managedType} {propertyScriptName};");
        builder.AppendLine();
        OnPropertyExported(builder, property);
    }
    
    public string GetCppDefaultValue(UhtFunction function, UhtProperty parameter)
    {
        string metaDataKey = $"CPP_Default_{parameter.SourceName}";
        return function.GetMetadata(metaDataKey);
    }
    
    protected void AddNativePropertyField(GeneratorStringBuilder builder, string propertyName)
    {
        builder.AppendLine($"static IntPtr {GetNativePropertyField(propertyName)};");
    }

    protected string GetNativePropertyField(string propertyName)
    {
        return $"{propertyName}_NativeProperty";
    }
}