# rhubarb-geek-nz/OnReflection
Reflection tool for PowerShell

This tool accesses objects using reflection.

```
Invoke-Reflection -Method <string> -Object <psobject> [-ArgumentList <Object[]>] [-TypeList <type[]>] [-BindingFlags <BindingFlags>] [-Binder <Binder>] [-ModifierList <ParameterModifier[]>] [<CommonParameters>]

Invoke-Reflection -Method <string> -Type <type> [-ArgumentList <Object[]>] [-TypeList <type[]>] [-BindingFlags <BindingFlags>] [-Binder <Binder>] [-ModifierList <ParameterModifier[]>] [<CommonParameters>]
```

See [test.ps1](test.ps1)
