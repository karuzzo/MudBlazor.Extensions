﻿using System.Reflection;
using System.Text;
using BlazorJS;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper
{
    public static class JsImportHelper
    {
        private static bool useMinified => true; //!Debugger.IsAttached;
        
        private static string min => useMinified ? ".min" : string.Empty;
        private static bool initialized;
        internal static IJSRuntime _runtime;

        internal static async Task<IJSRuntime> GetInitializedJsRuntime()
        {
            return _runtime;
        }

        internal static async Task<IJSRuntime> GetInitializedJsRuntime(object field, IJSRuntime fallback)
        {
            var js = fallback ?? _runtime ?? field.ExposeField<IJSRuntime>("_jsRuntime") ?? field.ExposeField<IJSRuntime>("_jsInterop");
            return await InitializeMudBlazorExtensionsAsync(js);
        }

        internal static Task<IJSObjectReference> ImportModuleMudEx(this IJSRuntime runtime)
        {
            //runtime.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorJS/BlazorJS.lib.module.js").AsTask();
            return runtime.InvokeAsync<IJSObjectReference>("import", $"./_content/MudBlazor.Extensions/MudBlazor.Extensions.lib.module.js{CB()}").AsTask();
        }

        public static async Task<IJSRuntime> InitializeMudBlazorExtensionsAsync(this IJSRuntime runtime, bool force = false)
        {
            _runtime = runtime ?? _runtime;
            if (force || !initialized)
            {
                await runtime.ImportModuleBlazorJS(); // This is a workaround for using module in MAUI apps
                if (useMinified)
                    await runtime.ImportModuleMudEx(); // This is a workaround for using module in MAUI apps
                else
                    await runtime.LoadJsAsync(MainJs());
                await runtime.AddCss(await MudExResource.GetEmbeddedFileContentAsync($"wwwroot/mudBlazorExtensions{min}.css"));
                initialized = true;
            }
            return runtime;
        }

        internal static string ComponentJs<TComponent>()
        {
            var componentName = GetJsComponentName<TComponent>();
            return ComponentJs(componentName);
        }

        private static string GetJsComponentName<TComponent>()
        {
            return typeof(TComponent).Name.Split('`')[0];
        }

        internal static string MainJs()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //return $"/_content/{assemblyName}/js/mudBlazorExtensions.js";
            return $"/_content/{assemblyName}/js/mudBlazorExtensions.all{min}.js{CB()}";
        }

        internal static string ComponentJs(string componentName)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            return $"/_content/{assemblyName}/js/components/{componentName}{min}.js{CB()}";
        }

        private static string CB()
        {
            if (useMinified)
                return string.Empty;
            return $"?cb={DateTime.Now.Ticks}";
        }

        internal static Task<IJSObjectReference> ImportModuleAsync<TComponent>(this IJSRuntime js)
        {
            return js.ImportModuleAsync(ComponentJs<TComponent>());
        }

        internal static Task<(IJSObjectReference moduleReference, IJSObjectReference jsObjectReference)> ImportModuleAndCreateJsAsync<TComponent>(this IJSRuntime js, params object?[]? args)
        {
            return js.ImportModuleAndCreateJsAsync(ComponentJs<TComponent>(), $"initialize{GetJsComponentName<TComponent>()}", args);
        }

    }
}