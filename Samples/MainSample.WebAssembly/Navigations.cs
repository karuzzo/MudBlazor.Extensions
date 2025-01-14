﻿using MainSample.WebAssembly.Types;
using MudBlazor;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using System.Reflection;

namespace MainSample.WebAssembly;

public static class Navigations
{
    public static HashSet<NavigationEntry> Default()
    {
        var navigationEntries = new HashSet<NavigationEntry>
        {
            new("Home", Icons.Material.Outlined.Home, "/"),
            new("Readme", Icons.Material.Outlined.ReadMore, "/readme"),
        };

        navigationEntries.UnionWith(DemoAttribute.AllEntries());

        navigationEntries.Add(new NavigationEntry("Utilities")
        {
            Children = GetUtils()
        });

        navigationEntries.Add(new NavigationEntry("Dynamic component tests")
        {
            Children = new()
            {
                new NavigationEntry("MudEx Components", null) {Children = ReflectMudExComponents()},
                new NavigationEntry("MudBlazor Components", null) {Children = ReflectMudBlazorComponents()},
            }
        });

        return navigationEntries;
    }

    private static HashSet<NavigationEntry> GetUtils()
    {
        return typeof(MudExSvg).Assembly.GetTypes()
            .Select(t => new { Type = t, Documentation = t.GetCustomAttribute<HasDocumentationAttribute>() })
            .Where(d => d.Documentation != null)
            .Select(d => new NavigationEntry(d.Type.Name, "", $"/d/{Path.GetFileNameWithoutExtension(d.Documentation.MarkdownFile)}")).ToHashSet();
    }

    internal static HashSet<NavigationEntry> ReflectMudExComponents()
    {
        return ComponentTypes.AllMudExComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}") {Type = type}).ToHashSet();
    }

    private static HashSet<NavigationEntry> ReflectMudBlazorComponents()
    {
        return ComponentTypes.AllMudBlazorComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}") { Type = type }).ToHashSet();
    }

}