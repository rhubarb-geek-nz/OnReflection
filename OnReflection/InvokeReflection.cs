// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections;
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

        [Parameter(ParameterSetName = "Instance-None", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Object instance to be invoked")]
        [Parameter(ParameterSetName = "Instance-List", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Object instance to be invoked")]
        [Parameter(ParameterSetName = "Instance-Dict", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Object instance to be invoked")]
        public PSObject Object;

        [Parameter(ParameterSetName = "Static-None", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Class for static method")]
        [Parameter(ParameterSetName = "Static-List", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Class for static method")]
        [Parameter(ParameterSetName = "Static-Dict", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Class for static method")]
        public Type Type;

        [Parameter(ParameterSetName = "Static-List", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Values to pass to invocation")]
        [Parameter(ParameterSetName = "Instance-List", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Values to pass to invocation")]
        public Object[] ArgumentList;

        [Parameter(ParameterSetName = "Static-List", Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Types of arguments")]
        [Parameter(ParameterSetName = "Instance-List", Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Types of arguments")]
        public Type[] TypeList;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public BindingFlags BindingFlags;

        [Parameter(ParameterSetName = "Static-List", Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = "Instance-List", Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public Binder Binder;

        [Parameter(ParameterSetName = "Static-List", Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = "Instance-List", Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public ParameterModifier[] ModifierList;

        [Parameter(ParameterSetName = "Static-Dict", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Dictionary of named arguments")]
        [Parameter(ParameterSetName = "Instance-Dict", Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Dictionary of named arguments")]
        public IDictionary ArgumentDictionary;

        static readonly Type[] EmptyTypes = new Type[0];
        static readonly object[] EmptyArguments = new object[0];

        protected override void ProcessRecord()
        {
            object baseObject = Object == null ? null : Object.BaseObject;
            Type type = baseObject == null ? Type : baseObject.GetType();
            BindingFlags bindingFlags = BindingFlags == 0 ? baseObject == null ? BindingFlags.Public | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance : BindingFlags;
            MethodInfo methodInfo = null;
            Object[] argumentList = null;
            if (ArgumentDictionary == null)
            {
                Type[] types = TypeList == null ? ArgumentList == null || ArgumentList.Length == 0 ? EmptyTypes : ArgumentList.Select(o => o.GetType()).ToArray() : TypeList;
                methodInfo = type.GetMethod(Method, bindingFlags, Binder, types, ModifierList);
                argumentList = ArgumentList == null ? EmptyArguments : ArgumentList;
            }
            else
            {
                foreach (var method in type.GetMethods(bindingFlags))
                {
                    if (Method.Equals(method.Name))
                    {
                        var parameters = method.GetParameters();
                        object[] args = new object[parameters.Length];
                        int i = 0, valuesUsed = 0;

                        foreach (var p in parameters)
                        {
                            if (ArgumentDictionary.Contains(p.Name))
                            {
                                object obj = ArgumentDictionary[p.Name];

                                if (obj != null && !p.ParameterType.IsAssignableFrom(obj.GetType()))
                                {
                                    break;
                                }

                                valuesUsed++;
                                args[i++] = obj;
                            }
                            else
                            {
                                if (p.HasDefaultValue)
                                {
                                    args[i++] = p.DefaultValue;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if (i == args.Length && valuesUsed == ArgumentDictionary.Count)
                        {
                            argumentList = args;
                            methodInfo = method;
                            break;
                        }
                    }
                }
            }

            if (methodInfo == null)
            {
                Exception ex = new InvalidOperationException($"No method found for {Method} in {type} with {bindingFlags}");
                WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidOperation, baseObject)); ;
            }
            else
            {
                try
                {
                    WriteObject(methodInfo.Invoke(baseObject, argumentList));
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
