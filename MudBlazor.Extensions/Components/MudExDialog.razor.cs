﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDialog : IMudExComponent
{
    private DialogOptionsEx _options;
    private IDialogReference _reference;

    [Inject] private IJSRuntime js { get; set; }

    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    protected override void OnParametersSet()
    {
        EnsureInitialClass();
        base.OnParametersSet();
    }
    
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<DialogOptions>(nameof(Options), out var options) && options is DialogOptionsEx)
            _options = (DialogOptionsEx) options;
        
        bool oldVisible = IsVisible;
        await base.SetParametersAsync(parameters);
        if (oldVisible != IsVisible)
        {
            if (IsVisible)
                await Show();
            else
                Close();
        }
    }


    /// <summary>Show this inlined dialog</summary>
    /// <param name="title"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public new async Task<IDialogReference> Show(string title = null, DialogOptions options = null)
    {
        OptionsEx.JsRuntime = js;
        //await DialogServiceExt.InjectOptionsAsync(DotNetObjectReference.Create(this as ComponentBase), js, OptionsEx);
        return await base.Show(title, options).InjectOptionsAsync(OptionsEx);
    }

    
    [Parameter]
    public DialogOptionsEx OptionsEx
    {
        get => _options;
        set
        {
            _options = value;
            Options = value;
        }
    }

    private void EnsureInitialClass()
    {
        if (string.IsNullOrEmpty(Class))
            Class = "mud-ex-dialog-initial";
        if (!Class.Contains("mud-ex-dialog-initial"))
            Class += " mud-ex-dialog-initial";
    }
}