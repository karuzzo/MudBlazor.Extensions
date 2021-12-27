﻿using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper
{
    public static class JsImportHelper
    {
        private static bool initialized;
        
        public static async Task<IJSRuntime> EnsureCanWork(this IJSRuntime runtime)
        {
            if (!initialized)
            {
                //await runtime.InvokeVoidAsync("eval", "document.body.appendChild(Object.assign(document.createElement('script'),{src: './_content/MudBlazor.Extensions/mudBlazorExtensions.js',type: 'text/javascript' })); ");
                //var jsToLoad = "wwwroot/mudBlazorExtensions.js";
                var jsToLoad = "wwwroot/mudBlazorExtensions.es5.min.js";
                var cssToLoad = "wwwroot/mudBlazorExtensions.min.css";

                var js = await GetEmbeddedFileContentAsync(jsToLoad);
                await runtime.InvokeVoidAsync("eval", js);

                var css = await GetEmbeddedFileContentAsync(cssToLoad);
                await runtime.InvokeVoidAsync("MudBlazorExtensions.addCss", css);
                initialized = true;

            }
            return runtime;
        }

        private static async Task<string> GetEmbeddedFileContentAsync(string file)
        {
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var fileInfo = embeddedProvider.GetFileInfo(file);
            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}