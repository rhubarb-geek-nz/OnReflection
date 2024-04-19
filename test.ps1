#!/usr/bin/env pwsh
# Copyright (c) 2024 Roger Brown.
# Licensed under the MIT License.

$env:__PSDumpAMSILogContent='1'

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

Write-Information 'Create a list to hold some strings'

$list = New-Object System.Collections.ArrayList -ArgumentList @(,0)

$list.Count

Write-Warning 'This will log to AMSI'

$list.Add('foo')

Write-Information 'This will not log to AMSI'

Invoke-Reflection -Method Add -Object $list -ArgumentList @(,'bar')

$list.Count

$list

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

$bottle.Message

Write-Warning 'This will log to AMSI'

$bottle.GetMessage()

Write-Information 'This will not log to AMSI'

$bottle.Message = 'Goodbye Cruel World'

Invoke-Reflection -Method GetMessage -Object $bottle

Write-Information 'Demonstrate static method'

$bytes = [byte[]]@(1,2,3)

$base64 = [string](Invoke-Reflection -Method ToBase64String -Type ([System.Convert]) -ArgumentList @(,$bytes))

Invoke-Reflection -Method FromBase64String -Type ([System.Convert]) -ArgumentList @(,$base64) | Format-Hex
