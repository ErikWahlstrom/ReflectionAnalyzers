# REFL015
## Use the containing type.

| Topic    | Value
| :--      | :--
| Id       | REFL015
| Severity | Warning
| Enabled  | True
| Category | ReflectionAnalyzers.SystemReflection
| Code     | [GetXAnalyzer]([GetXAnalyzer](https://github.com/DotNetAnalyzers/ReflectionAnalyzers/blob/master/ReflectionAnalyzers/NodeAnalzers/GetXAnalyzer.cs))

## Description

Use the containing type.

## Motivation

ADD MOTIVATION HERE

## How to fix violations

ADD HOW TO FIX VIOLATIONS HERE

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable REFL015 // Use the containing type.
Code violating the rule here
#pragma warning restore REFL015 // Use the containing type.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable REFL015 // Use the containing type.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReflectionAnalyzers.SystemReflection", 
    "REFL015:Use the containing type.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->