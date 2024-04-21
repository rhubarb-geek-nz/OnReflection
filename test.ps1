#!/usr/bin/env pwsh
# Copyright (c) 2024 Roger Brown.
# Licensed under the MIT License.

trap
{
	throw $PSItem
}

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

function Assert
{
	Param ([parameter(Mandatory=$true)][ScriptBlock]$assertion)

	if ( -not ( & $assertion ) )
	{
		throw $assertion
	}
}

$cmd = Get-Command -Verb 'Invoke' -Noun 'Reflection'

Assert { '1.0.1' -eq [string]$cmd.Version }

$list = New-Object System.Collections.ArrayList -ArgumentList @(,0)

$a = Invoke-Reflection -Method Add -Object $list -ArgumentList @(,'foo')

Assert { $a -eq 0 }

$b = Invoke-Reflection -Method Add -Object $list -ArgumentDictionary @{
	value = 'bar'
}

Assert { $b -eq 1 }

Assert { $list.Count -eq 2 }

Add-Type @"
namespace RhubarbGeekNz.OnReflection
{
	public class Bottle
	{
		public string Message { get; set; }
		public string GetMessage() { return Message; }
	}
}
"@

$bottle = New-Object RhubarbGeekNz.OnReflection.Bottle

$bottle.Message = 'Hello World'

$message = Invoke-Reflection -Method GetMessage -Object $bottle

Assert { $message -eq $bottle.Message }

$bytes = [byte[]]@(1,2,3)

$base64 = [string](Invoke-Reflection -Method ToBase64String -Type ([System.Convert]) -ArgumentList @(,$bytes))

Assert { $base64 -eq 'AQID' }

$result = Invoke-Reflection -Method FromBase64String -Type ([System.Convert]) -ArgumentDictionary @{
	s = $base64
}

Assert { $result.Length -eq 3 }

Write-Information 'Tests complete'
