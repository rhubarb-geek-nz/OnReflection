// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace RhubarbGeekNz.OnReflection
{
    [Cmdlet(VerbsLifecycle.Invoke, "Reflection")]
    sealed public class InvokeReflection : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Method to be invoked")]
        public string Method;

        [Parameter(ParameterSetName = "Instance", Mandatory=true, ValueFromPipelineByPropertyName = true, HelpMessage = "Object instance to be invoked")]
        public PSObject Object;

        [Parameter(ParameterSetName = "Static", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Class for static method")]
        public Type Type;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Values to pass to invocation")]
        public Object[] ArgumentList;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Types of arguments")]
        public Type[] TypeList;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public BindingFlags BindingFlags;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public Binder Binder;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public ParameterModifier[] ModifierList; 

        static readonly Type[] EmptyTypes = new Type[0];
        static readonly object[] EmptyArguments = new object[0];

        protected override void ProcessRecord()
        {
            object baseObject = Object == null ? null : Object.BaseObject;
            Type type = baseObject == null ? Type : baseObject.GetType();
            Type[] types = TypeList == null ? ArgumentList == null ? EmptyTypes : ArgumentList.Select(o => o.GetType()).ToArray() : TypeList;
            BindingFlags bindingFlags = BindingFlags==0 ? baseObject == null ? BindingFlags.Public | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance : BindingFlags;
            var methodInfo = type.GetMethod(Method, bindingFlags, Binder, types, ModifierList);
            if (methodInfo == null)
            {
                Exception ex = new InvalidOperationException($"No method found for {Method} in {type} with {bindingFlags}");
                WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidOperation, baseObject)); ;
            }
            else
            {
                try
                {
                    WriteObject(methodInfo.Invoke(baseObject, ArgumentList == null ? EmptyArguments : ArgumentList));
                }
                catch (ArgumentException ex)
                {
                    WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidArgument, baseObject));
                }
                catch (TargetInvocationException ex)
                {
                    WriteError(new ErrorRecord(ex.GetBaseException(), ex.GetType().Name, ErrorCategory.NotSpecified, baseObject));
                }
                catch (TargetParameterCountException ex)
                {
                    WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidArgument, baseObject));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.NotSpecified, baseObject));
                }
            }
        }
    }
}
