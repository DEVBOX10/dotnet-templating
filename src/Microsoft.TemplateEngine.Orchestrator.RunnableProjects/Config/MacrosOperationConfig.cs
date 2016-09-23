using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    public class MacrosOperationConfig
    {
        private static IReadOnlyDictionary<string, IMacro> _macroObjects;

        public Guid Id => new Guid("B03E4760-455F-48B1-9FF2-79ADB1E91519");

        public string Key => "macros";

        public int Order => -10000;

        public IEnumerable<IOperationProvider> ProcessMacros(IComponentManager componentManager, IReadOnlyList<IMacroConfig> macroConfigs, IVariableCollection variables, IParameterSet parameters)
        {
            EnsureMacros(componentManager);

            ParameterSetter setter = (p, value) =>
            {
                ((RunnableProjectGenerator.ParameterSet)parameters).AddParameter(p);
                parameters.ResolvedValues[p] = value;
            };

            IList<IMacroConfig> allMacroConfigs = new List<IMacroConfig>(macroConfigs);

            foreach (IMacroConfig config in allMacroConfigs)
            {
                if (config is GeneratedSymbolDeferredMacroConfig)
                {
                    continue;
                }

                IMacro macroObject;
                if (_macroObjects.TryGetValue(config.Type, out macroObject))
                {
                    macroObject.EvaluateConfig(variables, config, parameters, setter);
                }
            }

            // run the deferred macros
            foreach (IMacroConfig config in macroConfigs)
            {
                GeneratedSymbolDeferredMacroConfig deferredConfig = config as GeneratedSymbolDeferredMacroConfig;
                if (deferredConfig == null)
                {
                    continue;
                }

                IMacro macroObject;
                if (_macroObjects.TryGetValue(deferredConfig.Type, out macroObject))
                {
                    macroObject.EvaluateDeferredConfig(variables, deferredConfig, parameters, setter);
                }
            }

            return Empty<IOperationProvider>.List.Value;
        }

        private static void EnsureMacros(IComponentManager componentManager)
        {
            if (_macroObjects == null)
            {
                Dictionary<string, IMacro> macroObjects = new Dictionary<string, IMacro>();

                foreach (IMacro macro in componentManager.OfType<IMacro>())
                {
                    macroObjects[macro.Type] = macro;
                }

                _macroObjects = macroObjects;
            }
        }
    }
}