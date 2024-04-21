# rhubarb-geek-nz/OnReflection
Reflection tool for PowerShell

This tool accesses objects using reflection.

```
Invoke-Reflection -Method <string> -Object <psobject> [-BindingFlags <BindingFlags>]

Invoke-Reflection -Method <string> -Object <psobject> -ArgumentList <Object[]> [-TypeList <type[]>] [-BindingFlags <BindingFlags>] [-Binder <Binder>] [-ModifierList <ParameterModifier[]>]

Invoke-Reflection -Method <string> -Object <psobject> -ArgumentDictionary <IDictionary> [-BindingFlags <BindingFlags>]

Invoke-Reflection -Method <string> -Type <type> [-BindingFlags <BindingFlags>]

Invoke-Reflection -Method <string> -Type <type> -ArgumentList <Object[]> [-TypeList <type[]>] [-BindingFlags <BindingFlags>] [-Binder <Binder>] [-ModifierList <ParameterModifier[]>]

Invoke-Reflection -Method <string> -Type <type> -ArgumentDictionary <IDictionary> [-BindingFlags <BindingFlags>]
```

See [test.ps1](test.ps1)
