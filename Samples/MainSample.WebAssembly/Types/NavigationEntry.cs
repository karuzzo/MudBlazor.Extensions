﻿using Nextended.Core.Types;

namespace MainSample.WebAssembly.Types;

public class NavigationEntry : Hierarchical<NavigationEntry>
{
    public NavigationEntry(string text = "", string icon = "", string href = "", string target = "")
    {
        Icon = icon;
        Text = text;
        Href = href;
        Target = target;
    }
    public string Text { get; set; }
    public string Icon { get; set; }
    public string Href { get; set; }
    public string Target { get; set; }
    public bool? Bold { get; set; }
    internal DemoAttribute? Demo { get; set; }
}