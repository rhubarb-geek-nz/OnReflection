// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

#if NETCOREAPP
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RhubarbGeekNz.OnReflection
{
    [TestClass]
    public class UnitTests
    {
        readonly InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        public UnitTests()
        {
            foreach (Type t in new Type[] {
                typeof(InvokeReflection)
            })
            {
                CmdletAttribute ca = t.GetCustomAttribute<CmdletAttribute>();

                if (ca == null) throw new NullReferenceException();

                initialSessionState.Commands.Add(new SessionStateCmdletEntry($"{ca.VerbName}-{ca.NounName}", t, ca.HelpUri));
            }

            initialSessionState.Variables.Add(new SessionStateVariableEntry("ErrorActionPreference", ActionPreference.Stop, "Stop action"));
        }

        [TestMethod]
        public void TestObjectMethod()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("$obj = New-Object -TypeName System.Collections.ArrayList ; Invoke-Reflection -Method 'Add' -Object $obj -ArgumentList 'foo'");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(1, outputPipeline.Count);

                var result = outputPipeline[0];

                Assert.AreEqual(0,(int)result.BaseObject);
            }
        }

        [TestMethod]
        public void TestClassMethod()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("$args = [byte[][]]@(,@(1,2,3)) ; Invoke-Reflection -Method 'ToBase64String' -Type ([System.Convert]) -ArgumentList $args");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(1, outputPipeline.Count);

                var result = outputPipeline[0];

                Assert.AreEqual("AQID", (string)result.BaseObject);
            }
        }

        [TestMethod]
        public void TestInvalidMethod()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool wasCaught = false;
                string exType = null;

                try
                {
                    powerShell.AddScript("$obj = New-Object -TypeName System.Collections.ArrayList ; Invoke-Reflection -Method 'Foo' -Object $obj -ArgumentList @(,'foo')");

                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exType = ex.ErrorRecord.GetType().FullName;
                    wasCaught = ex.ErrorRecord.Exception is InvalidOperationException;
                }

                Assert.IsTrue(wasCaught, exType);
            }
        }

        [TestMethod]
        public void TestPipeline()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddCommand("Invoke-Reflection");

                PSObject pobj = new PSObject();

                pobj.Members.Add(new PSNoteProperty("Method", "ToBase64String"));
                pobj.Members.Add(new PSNoteProperty("Type", typeof(System.Convert)));
                pobj.Members.Add(new PSNoteProperty("ArgumentList", new byte[][] { new byte[] { 1, 2, 3 } }));

                var outputPipeline = powerShell.Invoke(new object[] { pobj});

                Assert.AreEqual(1, outputPipeline.Count);

                var result = outputPipeline[0];

                Assert.AreEqual("AQID", (string)result.BaseObject);
            }
        }

        [TestMethod]
        public void TestBase64String()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript(
                    "$bytes = [byte[]]@(1,2,3)" + Environment.NewLine + 
                    "$base64 = Invoke-Reflection -Method ToBase64String -Type ([System.Convert]) -ArgumentList @(,$bytes)" + Environment.NewLine+
                    "Invoke-Reflection -Method FromBase64String -Type ([System.Convert]) -ArgumentList @(,[string]$base64)");

                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(1, outputPipeline.Count);

                byte [] result = (byte[])outputPipeline[0].BaseObject;

                Assert.AreEqual(3,result.Length);
            }
        }

        [TestMethod]
        public void TestInvalidArgument()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool wasCaught = false;
                string exType = null;

                try
                {
                    powerShell.AddScript("Invoke-Reflection -Method 'ToBase64String' -Type ([System.Convert]) -TypeList @(,([byte[]])) -ArgumentList @(,'foo')");

                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exType = ex.ErrorRecord.Exception.GetType().FullName;
                    wasCaught = ex.ErrorRecord.Exception is ArgumentException;
                }

                Assert.IsTrue(wasCaught, exType);
            }
        }

        [TestMethod]
        public void TestInvalidData()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool wasCaught = false;
                string exType = null;

                try
                {
                    powerShell.AddScript("Invoke-Reflection -Method 'FromBase64String' -Type ([System.Convert]) -ArgumentList @(,'?*&^$=~')");

                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exType = ex.ErrorRecord.Exception.GetType().FullName;
                    wasCaught = ex.ErrorRecord.Exception is FormatException;
                }

                Assert.IsTrue(wasCaught, exType);
            }
        }

        [TestMethod]
        public void TestInvalidArguments()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                bool wasCaught = false;
                string exType = null;

                try
                {
                    powerShell.AddScript("Invoke-Reflection -Method 'FromBase64String' -Type ([System.Convert]) -TypeList @(,([string])) -ArgumentList @('foo','bar')");

                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exType = ex.ErrorRecord.Exception.GetType().FullName;
                    wasCaught = ex.ErrorRecord.Exception is TargetParameterCountException;
                }

                Assert.IsTrue(wasCaught, exType);
            }
        }
    }
}
