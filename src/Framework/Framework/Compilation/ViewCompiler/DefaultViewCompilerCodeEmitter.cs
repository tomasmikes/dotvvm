using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Controls;
using DotVVM.Framework.Utils;
using FastExpressionCompiler;
using Microsoft.Extensions.DependencyInjection;

namespace DotVVM.Framework.Compilation.ViewCompiler
{
    public class DefaultViewCompilerCodeEmitter
    {
        private static Type[] emptyTypeArguments = new Type[] { };

        private int CurrentControlIndex;

        public const string ControlBuilderFactoryParameterName = "controlBuilderFactory";
        public const string ServiceProviderParameterName = "services";
        public const string BuildTemplateFunctionName = "BuildTemplate";

        private Dictionary<GroupedDotvvmProperty, string> cachedGroupedDotvvmProperties = new Dictionary<GroupedDotvvmProperty, string>();
        private ConcurrentDictionary<(Type obj, string argTypes), string> injectionFactoryCache = new ConcurrentDictionary<(Type obj, string argTypes), string>();
        private readonly Stack<BlockInfo> blockStack = new();
        public Type? ResultControlType { get; set; }

        public ParameterExpression EmitCreateVariable(Expression expression)
        {
            var name = ("c" + CurrentControlIndex).DotvvmInternString();
            CurrentControlIndex++;

            var variable = Expression.Variable(expression.Type, name);
            blockStack.Peek().Variables.Add(name, variable);

            EmitStatement(Expression.Assign(variable, expression));

            return variable;
        }

        public Expression EmitValue(object? value) => Expression.Constant(value);

        /// <summary>
        /// Emits the create object expression.
        /// </summary>
        public ParameterExpression EmitCreateObject(Type type, object?[]? constructorArguments = null)
        {
            constructorArguments ??= new object[0];
            var arguments = constructorArguments.Select(EmitValue);
            var argumentTypes = arguments.Select(a => a.Type).ToArray();
            var constructor = type.GetConstructor(argumentTypes).NotNull($"Could not find constructor of {type} with arguments ({string.Join(", ", argumentTypes.Select(a => a.Name))})");
            return EmitCreateObject(constructor, arguments);
        }
        /// <summary>
        /// Emits the create object expression.
        /// </summary>
        public ParameterExpression EmitCreateObject(ConstructorInfo ctor, object?[]? constructorArguments = null)
        {
            constructorArguments ??= new object[0];
            var parameters = ctor.GetParameters();
            if (parameters.Length != constructorArguments.Length)
                throw new ArgumentException($"Constructor {ctor.DeclaringType?.Name}.{ctor.Name} has {parameters.Length} parameters, but {constructorArguments.Length} arguments were provided.");
            return EmitCreateObject(ctor, constructorArguments.Zip(parameters, (a, p) => Expression.Constant(a, p.ParameterType)));
        }

        public ParameterExpression EmitCustomInjectionFactoryInvocation(Type factoryType, Type controlType)
        {
            //[controlType] c = ([controlType])(([factoryType])services.GetService(factoryType)(services,controlType))

            var servicesParameter = GetParameterOrVariable(ServiceProviderParameterName);

            var getServiceCall = Expression.Call(servicesParameter, nameof(IServiceProvider.GetService), emptyTypeArguments, Expression.Constant(factoryType));
            var factoryInstance = Expression.Convert(getServiceCall, factoryType);

            var factoryInvoke = Expression.Invoke(factoryInstance, servicesParameter, Expression.Constant(controlType));

            return EmitCreateVariable(Expression.Convert(factoryInvoke, controlType));
        }

        public ParameterExpression EmitInjectionFactoryInvocation(Type type, object[] arguments)
        {
            //[type] v = ([type])factory(services, object[] { ...arguments.Expression } )

            var objectFactory = ActivatorUtilities.CreateFactory(type, arguments.Select(a => a.GetType()).ToArray());

            var factoryInvoke = Expression.Invoke(Expression.Constant(objectFactory), GetParameterOrVariable(ServiceProviderParameterName), Expression.Constant(arguments, typeof(object[])));

            return EmitCreateVariable(Expression.Convert(factoryInvoke, type));
        }

        private ParameterExpression EmitCreateObject(ConstructorInfo ctor, IEnumerable<Expression> arguments)
        {
            return EmitCreateVariable(EmitCreateObjectExpression(ctor, arguments));
        }

        private Expression EmitCreateObjectExpression(ConstructorInfo ctor, IEnumerable<Expression> arguments)
        {
            return Expression.New(ctor, arguments.ToArray());
        }

        public static Expression EmitCreateArray(Type elementType, IEnumerable<Expression> values)
        {
            //new [elementType] [] = { ([elementType])v1, ([elementType])v2, ([elementType])v3, ... }
            //note: [elementType] is name of the type provided in 'elementType' parameter.

            var convertedValues = values.Select(v => Expression.Convert(v, elementType));
            return Expression.NewArrayInit(elementType, convertedValues);
        }

        public ParameterExpression EmitInvokeControlBuilder(Type controlType, string virtualPath)
        {
            var builderValueExpression = GetControlBuilderCreatingExpression(virtualPath);

            return EmitInvokeBuildControl(controlType, builderValueExpression);
        }

        private ParameterExpression EmitInvokeBuildControl(Type controlType, Expression builderValueExpression)
        {
            //var [untypedName] = [builderName].BuildControl(controlBuilderFactory, services)
            var controlBuilderFactoryParameter = GetParameterOrVariable(ControlBuilderFactoryParameterName);

            var servicesParameter = GetParameterOrVariable(ServiceProviderParameterName);

            var buildControlCall = Expression.Call(builderValueExpression, nameof(IControlBuilder.BuildControl), emptyTypeArguments, controlBuilderFactoryParameter, servicesParameter);

            return EmitCreateVariable(Expression.Convert(buildControlCall, controlType));
        }

        private Expression GetControlBuilderCreatingExpression(string virtualPath)
        {
            //var [builderName] = controlBuilderFactory.GetControlBuilder(virtualPath).Item2.Value

            var controlBuilderFactoryParameter = GetParameterOrVariable(ControlBuilderFactoryParameterName);

            var getBuilderCall = Expression.Call(controlBuilderFactoryParameter, nameof(IControlBuilderFactory.GetControlBuilder), emptyTypeArguments, EmitValue(virtualPath));

            var builderValueExpression = Expression.PropertyOrField(
                Expression.PropertyOrField(getBuilderCall, "Item2"),
                "Value");
            return builderValueExpression;
        }

        public void EmitSetProperty(string controlName, string propertyName, Expression valueExpression)
        {
            //[controlName].[propertyName] = [value]
            var controlParameter = GetParameterOrVariable(controlName);
            var assigment = Expression.Assign(Expression.PropertyOrField(controlParameter, propertyName), valueExpression);

            EmitStatement(assigment);
        }

        private readonly Dictionary<string, List<(DotvvmProperty prop, Expression value)>> controlProperties = new Dictionary<string, List<(DotvvmProperty, Expression)>>();

        public void EmitSetDotvvmProperty(string controlName, DotvvmProperty property, object? value) =>
            EmitSetDotvvmProperty(controlName, property, EmitValue(value));

        public void EmitSetDotvvmProperty(string controlName, DotvvmProperty property, Expression value)
        {
            if (!controlProperties.TryGetValue(controlName, out var propertyList))
                throw new Exception($"Can not set property, control {controlName} is not registered");

            propertyList.Add((property, value));
        }

        /// Instructs the emitter that this object can receive DotvvmProperties
        /// Note that the properties have to be committed using <see cref="CommitDotvvmProperties(string)" />
        public void RegisterDotvvmProperties(string controlName) =>
            controlProperties.Add(controlName, new List<(DotvvmProperty prop, Expression value)>());

        public void CommitDotvvmProperties(string name)
        {
            var properties = controlProperties[name];
            controlProperties.Remove(name);
            if (properties.Count == 0) return;

            properties.Sort((a, b) => string.Compare(a.prop.FullName, b.prop.FullName, StringComparison.Ordinal));

            var (hashSeed, keys, values) = PropertyImmutableHashtable.CreateTableWithValues(properties.Select(p => p.prop).ToArray(), properties.Select(p => p.value).ToArray());


            Expression valueExpr;
            if (TryCreateArrayOfConstants(values, out var invertedValues))
            {
                valueExpr = EmitValue(invertedValues);
            }
            else
            {
                valueExpr = EmitCreateArray(
                    typeof(object),
                    values.Select(v => v ?? EmitValue(null))
                );
            }

            var keyExpr = EmitValue(keys);

            // control.MagicSetValue(keys, values, hashSeed)
            var controlParameter = GetParameterOrVariable(name);

            var magicSetValueCall = Expression.Call(controlParameter, nameof(DotvvmBindableObject.MagicSetValue), emptyTypeArguments, keyExpr, valueExpr, EmitValue(hashSeed));

            EmitStatement(magicSetValueCall);
        }

        private bool TryCreateArrayOfConstants(Expression?[] values, out object?[] invertedValues)
        {
            invertedValues = new object?[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null) { continue; }

                if (values[i] is ConstantExpression constant)
                {
                    invertedValues[i] = constant.Value;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Emits the code that adds the specified value as a child item in the collection.
        /// </summary>
        public void EmitAddCollectionItem(string collectionName, string variableName)
        {
            var collectionParameter = GetParameterOrVariable(collectionName);
            var variablePartameter = GetParameterOrVariable(variableName);

            //[collectionParameter].Add([variablePartameter])
            var collectionAddCall = Expression.Call(collectionParameter, "Add", emptyTypeArguments, variablePartameter);

            EmitStatement(collectionAddCall);
        }

        /// <summary>
        /// Emits the code that adds the specified value as a child of the control
        /// </summary>
        public void EmitAddChildControl(string controlName, string variableName)
        {
            var controlParameter = GetParameterOrVariable(controlName);

            var collectionExpression = Expression.PropertyOrField(controlParameter, "Children");
            var variablePartameter = GetParameterOrVariable(variableName);

            //[collectionExpression].Children.AddUnchecked([variableParameter])
            var collectionAddCall = Expression.Call(collectionExpression, "AddUnchecked", emptyTypeArguments, variablePartameter);

            EmitStatement(collectionAddCall);
        }

        /// <summary>
        /// Emits the add HTML attribute.
        /// </summary>
        public void EmitAddToDictionary(string controlName, string propertyName, string key, Expression valueExpression)
        {
            //[controlName].[propertyName][key]= value;
            var controlParameter = GetParameterOrVariable(controlName);

            var dictionaryKeyExpression = Expression.Property(
                Expression.PropertyOrField(controlParameter, propertyName),
                "Item",
                EmitValue(key));

            var assigment = Expression.Assign(dictionaryKeyExpression, valueExpression);

            EmitStatement(assigment);
        }

        /// <summary>
        /// Emits the add directive.
        /// </summary>
        public void EmitAddDirective(string controlName, string name, string value)
        {
            EmitAddToDictionary(controlName, "Directives", name, EmitValue(value));
        }

        public ParameterExpression EmitEnsureCollectionInitialized(string parentName, DotvvmProperty property)
        {
            //if([parentName].GetValue(property) == null)
            //{
            //  [parentName].SetValue(property, new [property.PropertyType]());
            //}

            var parentParameter = GetParameterOrVariable(parentName);

            var getPropertyValue = Expression.Call(
                parentParameter,
                "GetValue",
                emptyTypeArguments,
                /*property:*/ EmitValue(property),
                /*inherit:*/ EmitValue(true /*default*/ ));

            var ifCondition = Expression.Equal(getPropertyValue, Expression.Constant(null));

            var statement = Expression.Call(
                parentParameter,
                "SetValue",
                emptyTypeArguments,
                /*property*/ EmitValue(property),
                /*value*/ EmitCreateObject(property.PropertyType));

            var ifStatement = Expression.IfThen(ifCondition, statement);

            EmitStatement(ifStatement);

            //var c = ([property.PropertyType])[parentName].GetValue(property);

            return EmitCreateVariable(Expression.Convert(getPropertyValue, property.PropertyType));
        }

        /// <summary>
        /// Emits the return clause.
        /// </summary>
        public void EmitReturnClause(string variableName)
        {
            EmitStatement(GetParameterOrVariable(variableName));
        }
        public void EmitStatement(Expression expr)
        {
            blockStack.Peek().Expressions.Add(expr);
        }

        public ParameterExpression GetParameterOrVariable(string identifierName)
            => GetCurrentBlock().GetParameterOrVariable(identifierName);

        private BlockInfo GetCurrentBlock() => blockStack.Peek();
        public ParameterExpression EmitParameter(string name, Type type) => Expression.Parameter(type, name);

        public ParameterExpression[] EmitControlBuilderParameters()
            => new[]
            {
                EmitParameter(ControlBuilderFactoryParameterName, typeof(IControlBuilderFactory)),
                EmitParameter(ServiceProviderParameterName, typeof(IServiceProvider))
            };

        /// <summary>
        /// Pushes the new method.
        /// </summary>
        public void PushNewMethod(params ParameterExpression[] parameters)
        {
            blockStack.Push(new BlockInfo(parameters));
        }

        /// <summary>
        /// Pops the method.
        /// </summary>
        public TDelegate PopMethod<TDelegate>()
            where TDelegate : Delegate
        {
            var blockInfo = blockStack.Pop();
            var block = Expression.Block(blockInfo.Variables.Values, blockInfo.Expressions);

            var lambda = Expression.Lambda<TDelegate>(block, blockInfo.Parameters.Values);
            return lambda.CompileFast(flags: CompilerFlags.ThrowOnNotSupportedExpression);
        }

        private record BlockInfo
        {
            public IReadOnlyDictionary<string, ParameterExpression> Parameters { get; }
            public Dictionary<string, ParameterExpression> Variables { get; set; } = new();
            public List<Expression> Expressions { get; set; } = new();

            public BlockInfo(ParameterExpression[] parameters)
            {
                Parameters = parameters.ToDictionary(k => k.Name.NotNull(), v => v);
            }

            public ParameterExpression GetParameterOrVariable(string identifierName)
               => Variables.ContainsKey(identifierName) ? Variables[identifierName]
               : Parameters.ContainsKey(identifierName) ? Parameters[identifierName]
               : throw new ArgumentException($"Parameter or variable '{identifierName}' was not found in the current block.");
        }
    }
}
