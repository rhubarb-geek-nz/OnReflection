// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

#if NETCOREAPP
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

                Assert.AreEqual(0, (int)result.BaseObject);
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

                var outputPipeline = powerShell.Invoke(new object[] { pobj });

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
                    "$base64 = Invoke-Reflection -Method ToBase64String -Type ([System.Convert]) -ArgumentList @(,$bytes)" + Environment.NewLine +
                    "Invoke-Reflection -Method FromBase64String -Type ([System.Convert]) -ArgumentList @(,[string]$base64)");

                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(1, outputPipeline.Count);

                byte[] result = (byte[])outputPipeline[0].BaseObject;

                Assert.AreEqual(3, result.Length);
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

        [TestMethod]
        public void TestNamedArguments()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript(
                    "Param($addParams,$getRangeParams)" + Environment.NewLine +
                    "$list = New-Object System.Collections.ArrayList" + Environment.NewLine +
                    "Invoke-Reflection -Method 'Add' -Object $list -ArgumentDictionary $addParams" + Environment.NewLine +
                    "Invoke-Reflection -Method 'GetRange' -Object $list -ArgumentDictionary $getRangeParams"
                    )
                    .AddArgument(new Dictionary<string, object>() { { "value", "Hello World" } })
                    .AddArgument(new Dictionary<string, object>() { { "index", 0 }, { "count", 1 } });

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(2, outputPipeline.Count);
                Assert.AreEqual(0, (int)outputPipeline[0].BaseObject);
                Assert.AreEqual(1, ((ArrayList)outputPipeline[1].BaseObject).Count);
                Assert.AreEqual("Hello World", ((ArrayList)outputPipeline[1].BaseObject)[0]);
            }
        }

        [TestMethod]
        public void TestHashTable()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript(
                    "$addParams = @{ value = 'Hello World' }" + Environment.NewLine +
                    "$getRangeParams = @{ index = 0 ; count=1 }" + Environment.NewLine +
                    "$list = New-Object System.Collections.ArrayList" + Environment.NewLine +
                    "Invoke-Reflection -Method 'Add' -Object $list -ArgumentDictionary $addParams" + Environment.NewLine +
                    "Invoke-Reflection -Method 'GetRange' -Object $list -ArgumentDictionary $getRangeParams"
                    );

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(2, outputPipeline.Count);
                Assert.AreEqual(0, (int)outputPipeline[0].BaseObject);
                Assert.AreEqual(1, ((ArrayList)outputPipeline[1].BaseObject).Count);
                Assert.AreEqual("Hello World", ((ArrayList)outputPipeline[1].BaseObject)[0]);
            }
        }

        [TestMethod]
        public void TestNoArguments()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript(
                    "$list = New-Object System.Collections.ArrayList" + Environment.NewLine +
                    "Invoke-Reflection -Method 'ToArray' -Object $list"
                    )
                    .AddArgument(new Dictionary<string, object>() { { "value", "Hello World" } })
                    .AddArgument(new Dictionary<string, object>() { { "index", 0 }, { "count", 1 } });

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(1, outputPipeline.Count);
                Assert.AreEqual(0, ((object[])outputPipeline[0].BaseObject).Length);
            }
        }

        public class MyBaseClass
        {
            public void BaseMethod() { }
        }

        public class MyDisposableClass : MyBaseClass, IDisposable
        {
            public void Dispose() { }
        }

        public class MyChildClass : MyDisposableClass
        {
            public MyChildClass() { }
            public void ChildMethod() { }
            public void MethodWithDefault(int foo, int bar = 4) { }
        }

        [TestMethod]
        public void TestReflection()
        {
            MethodInfo baseMethod = typeof(MyBaseClass).GetMethod("BaseMethod");
            MethodInfo disposeMethod = typeof(IDisposable).GetMethod("Dispose");
            MethodInfo childMethod = typeof(MyChildClass).GetMethod("ChildMethod");
            MethodInfo defaultMethod = typeof(MyChildClass).GetMethod("MethodWithDefault");
            var defaultParams = defaultMethod.GetParameters();

            Assert.IsNotNull(baseMethod, "base method is not null");
            Assert.IsNotNull(disposeMethod, "dispose method is not null");
            Assert.IsNotNull(childMethod, "child method is not null");

            Assert.IsTrue(typeof(MyBaseClass).IsAssignableFrom(typeof(MyChildClass)), "Assign from test");
            Assert.IsTrue(typeof(MyBaseClass).GetMethods().Where(m => m.Name.Equals(baseMethod.Name)).Any(), "base has base method");
            Assert.IsFalse(typeof(MyBaseClass).GetMethods().Where(m => m.Name.Equals(childMethod.Name)).Any(), "base should not have child method");
            Assert.IsFalse(typeof(MyBaseClass).GetMethods().Where(m => m.Name.Equals(disposeMethod.Name)).Any(), "base should not have dispose method");

            Assert.IsTrue(typeof(MyChildClass).GetMethods().Where(m => m.Name.Equals(baseMethod.Name)).Any(), "child has base method");
            Assert.IsTrue(typeof(MyChildClass).GetMethods().Where(m => m.Name.Equals(disposeMethod.Name)).Any(), "child has dispose method");
            Assert.IsTrue(typeof(MyChildClass).GetMethods().Where(m => m.Name.Equals(childMethod.Name)).Any(), "base has child method");
        }
    }
}
