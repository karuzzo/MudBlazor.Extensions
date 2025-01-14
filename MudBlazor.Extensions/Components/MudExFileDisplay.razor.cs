﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Nextended.Blazor.Helper;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using PSC.Blazor.Components.BrowserDetect;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to display a file in a iframe or object tag or in a registered viewer that implements IMudExFileDisplay
/// </summary>
public partial class MudExFileDisplay : IMudExFileDisplayInfos
{

    #region Parameters and Properties

    [Parameter] public string Url { get; set; }
    [Parameter] public string ContentType { get; set; }
    [Parameter] public bool CanClose { get; set; } 
    [Parameter] public EventCallback OnCloseClick { get; set; }
    [Parameter] public string ElementId { get; set; } = Guid.NewGuid().ToFormattedId();
    /**
     * Should be true if file is not a binary one
     */
    [Parameter]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter]
    public bool ViewDependsOnContentType { get; set; } = true;

    [Parameter] public bool ImageAsBackgroundImage { get; set; } = false;
    [Parameter] public bool ShowContentError { get; set; } = true;
    [Parameter] public bool SandBoxIframes { get; set; } = true;
    [Parameter] public bool AllowDownload { get; set; } = true;
    [Parameter] public string FileName { get; set; }
    [Parameter] public Stream ContentStream { get; set; }

    /**
     * A function to handle content error. Return true if you have handled the error and false if you want to show the error message
     * For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
     * If you reset Url or Data here you need also to reset ContentType
     */
    [Parameter] public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandleContentErrorFunc { get; set; }
    [Parameter] public string CustomContentErrorMessage { get; set; }
    public string MediaType => ContentType?.Split("/")?.FirstOrDefault()?.ToLower();
    public BrowserContentTypePlugin PossiblePlugin { get; private set; }
    public BrowserInfo Info
    {
        get => _info;
        set
        {
            _info = value;
            PossiblePlugin = BrowserContentTypePlugin.Find(Info.BrowserName, ContentType);
            StateHasChanged();
        }
    }

    #endregion

    #region private fields

    private bool _isNativeRendered;
    private string _id = Guid.NewGuid().ToString();
    private (string tag, Dictionary<string, object> attributes) renderInfos;
    private BrowserInfo _info;
    private bool internalOverwrite;
    private List<IMudExFileDisplay> _possibleRenderControls;
    private (Type ControlType, bool ShouldAddDiv, IDictionary<string, object> Parameters) _componentForFile;


    #endregion

    public override object[] GetJsArguments()
    {
        return base.GetJsArguments().Concat(new object[] { _id }).ToArray();
    }

    protected override Task OnParametersSetAsync()
    {
        _possibleRenderControls = GetServices<IMudExFileDisplay>().Where(c => c.GetType() != GetType() && c.CanHandleFile(this)).ToList();
        if (ViewDependsOnContentType)
            _componentForFile = GetComponentForFile(_possibleRenderControls.FirstOrDefault());

        if (!internalOverwrite)
            renderInfos = GetRenderInfos();
        return base.OnParametersSetAsync();
    }

    private (Type ControlType, bool ShouldAddDiv, IDictionary<string, object> Parameters) GetComponentForFile(IMudExFileDisplay fileComponent)
    {
        var type = fileComponent?.GetType();
        if (type != null)
        {
            var parameters = ComponentRenderHelper.GetCompatibleParameters(this, type);
            parameters.Add(nameof(IMudExFileDisplay.FileDisplayInfos), this);
            
            return (type, fileComponent.WrapInMudExFileDisplayDiv, parameters);
        }
        return default;
    }

    private (string tag, Dictionary<string, object> attributes) GetRenderInfos()
    {
        if (ViewDependsOnContentType && !string.IsNullOrEmpty(MediaType))
        {
            switch (MediaType)
            {
                case "image":
                    return !ImageAsBackgroundImage
                        ? ("img", new()
                        {
                            {"data-id", _id},
                            {"src", Url},
                            {"loading", "lazy"},
                            {"alt", FileName},
                            {"data-mimetype", ContentType}
                        })
                        : ("div", new()
                        {
                            {"data-id", _id},
                            {"style", $"background-image:url('{Url}')"},
                            {"class", "mud-ex-file-display-img-box"},
                            {"src", Url},
                            {"loading", "lazy"},
                            {"alt", FileName},
                            {"data-mimetype", ContentType}
                        });
                case "video":
                    return ("video", new()
                    {
                        {"data-id", _id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", Url},
                        {"type", ContentType}
                    });
                case "audio":
                    return ("audio", new()
                    {
                        {"data-id", _id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", Url},
                        {"type", ContentType}
                    });
            }
        }

        if (!FallBackInIframe) // wenn binary
        {
            return ("object", new()
            {
                {"data-id", _id},
                {"data", Url},
                {"onerror", $"{GetJsOnError()}"},
                {"loading", "lazy"},
                {"type", ContentType}
            });
        }

        return ("iframe", new()
        {
            {"data-id", _id},
            {"src", Url},
            {"onerror", $"{GetJsOnError()}"},
            {"loading", "lazy"},
            {"sandbox", SandBoxIframes}
        });
    }
    
    private string GetJsOnError() =>
        @$"
            var displayMessage = !window.__mudExFileDisplay || !window.__mudExFileDisplay['{_id}'];
            var elementToShow = document.getElementById('content-type-display-error');
            if(displayMessage && elementToShow)
            {{
                elementToShow.classList.add('visible');
            }}
            else {{
                setTimeout(function(){{
                    window.__mudExFileDisplay['{_id}'].callBackReference.invokeMethodAsync('{nameof(HandleContentError)}').then(function(isHandled){{
                        if(!isHandled && elementToShow)
                        {{
                            document.getElementById('content-type-display-error').classList.add('visible');
                        }}                            
                    }});
                }}, 0)
            }}
        ";

    private bool internalCall;
    [JSInvokable]
    public async Task<bool> HandleContentError()
    {
        if (internalCall)
            return !(internalCall = false);
        var result = HandleContentErrorFunc != null ? await HandleContentErrorFunc(this) : MudExFileDisplayContentErrorResult.Unhandled;
        if (HandleContentErrorFunc != null && result != null)
        {
            internalCall = true;
            internalOverwrite = true;
            UpdateChangedFields(result);
            StateHasChanged();
        }

        return result?.IsHandled ?? false;
    }

    private void UpdateChangedFields(MudExFileDisplayContentErrorResult result)
    {
        bool urlChanged = false;
        if (result.ContentStream != null && result.ContentStream != ContentStream)
            ContentStream = result.ContentStream;
        if (result.FallBackInIframe != null && result.FallBackInIframe.Value != FallBackInIframe)
            FallBackInIframe = result.FallBackInIframe.Value;
        if (result.SandBoxIframes != null && result.SandBoxIframes.Value != SandBoxIframes)
            SandBoxIframes = result.SandBoxIframes.Value;
        if (!string.IsNullOrWhiteSpace(result.Message) && result.Message != CustomContentErrorMessage)
            CustomContentErrorMessage = result.Message;
        if (!string.IsNullOrWhiteSpace(result.ContentType) && result.ContentType != ContentType)
            ContentType = result.ContentType;
        if (!string.IsNullOrWhiteSpace(result.Url) && result.Url != Url)
        {
            urlChanged = true;
            Url = result.Url;
        }

        renderInfos = GetRenderInfos();
        if (renderInfos.tag == "object" && JsRuntime != null && urlChanged)
            JsRuntime.InvokeVoidAsync("eval", $"document.querySelector('object[data-id=\"{_id}\"]').data += ' ';");
    }

    private async Task Download(MouseEventArgs arg)
    {
        if (string.IsNullOrWhiteSpace(Url) && ContentStream != null)
        {
            ContentStream.Position = 0;
            Url = await DataUrl.GetDataUrlAsync(ContentStream.ToByteArray(), ContentType);
        }

        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url,
            FileName = $"{FileName}",
            MimeType = ContentType
        });
    }

    private void CloseContentError()
    {
        JsRuntime.InvokeVoidAsync("eval",
            "document.getElementById('content-type-display-error').classList.remove('visible')");
    }
    
    private void RenderWith(IMudExFileDisplay fileComponent)
    {
        CloseContentError();
        _componentForFile = GetComponentForFile(fileComponent); 
    }
}