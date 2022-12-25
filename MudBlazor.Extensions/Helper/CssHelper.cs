﻿using MudBlazor.Extensions.Extensions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Helper;

public static class CssHelper
{
    private static readonly string[] PropertiesToAddUnits = { "width", "height", "max-height", "max-width", "margin", "padding" };

    public static string GenerateCssString(object obj, string existingCss = "")
    {
        string unit = "px";
        var cssBuilder = new StringBuilder();

        // Get all of the properties of the object
        var properties = obj.GetType().GetProperties();

        // Iterate through the properties and append their names and values to the CSS string
        foreach (var property in properties)
        {
            // Split the property name on any uppercase character and concatenate the split parts with a hyphen
            var cssPropertyName = Regex.Replace(property.Name, "(?<!^)([A-Z])", "-$1").ToLower();

            // Surround the property value in quotes if it is a string
            object propertyValue = property.GetValue(obj, null);
            if (propertyValue != null)
            {
                if (propertyValue is Color color)
                    propertyValue = color.CssVarDeclaration();
                var formattedPropertyValue = propertyValue is string ? $"{propertyValue}" : propertyValue?.ToString();

                // If the property is an integer and its name is in the list of properties that should have units added, add the specified unit
                if (propertyValue is int && PropertiesToAddUnits.Contains(cssPropertyName))
                    formattedPropertyValue += unit;

                cssBuilder.Append(cssPropertyName + ": " + formattedPropertyValue + ";");
            }
        }

        return string.IsNullOrEmpty(existingCss) ? cssBuilder.ToString() : CombineCSSStrings(cssBuilder.ToString(), existingCss);
    }

    public static string CombineCSSStrings(string cssString, string leadingCssString)
    {
        // Create a dictionary to store the CSS properties and values
        var cssProperties = new Dictionary<string, string>();

        // Use a regular expression to extract the properties and values from the first CSS string
        var cssRegex = new Regex(@"([\w-]+)\s*:\s*([^;]+)");
        var cssProperties1 = cssRegex.Matches(cssString);
        foreach (Match property in cssProperties1)
        {
            // Trim any leading or trailing whitespace from the key and value
            var key = property.Groups[1].Value.Trim();
            var value = property.Groups[2].Value.Trim();

            // Add the property to the dictionary if it doesn't already exist
            if (!cssProperties.ContainsKey(key))
            {
                cssProperties.Add(key, value);
            }
        }

        // Use a regular expression to extract the properties and values from the second CSS string
        var cssProperties2 = cssRegex.Matches(leadingCssString);
        foreach (Match property in cssProperties2)
        {
            // Trim any leading or trailing whitespace from the key and value
            string key = property.Groups[1].Value.Trim();
            string value = property.Groups[2].Value.Trim();

            // Add the property to the dictionary or update its value if it already exists
            if (cssProperties.ContainsKey(key))
            {
                cssProperties[key] = value;
            }
            else
            {
                cssProperties.Add(key, value);
            }
        }

        return cssProperties.Aggregate("", (current, property) => current + (property.Key + ": " + property.Value + "; "));
    }


    public static T CssStringToObject<T>(string css) where T : new()
    {
        T obj = new T();

        // Split the CSS string into individual properties
        string[] properties = css.Split(';');

        // Iterate through the properties and set the corresponding property values on the object
        foreach (string property in properties)
        {
            // Split the property into its name and value
            string[] propertyParts = property.Split(':');
            if (propertyParts.Length != 2)
            {
                continue;
            }

            string propertyName = propertyParts[0].Trim();
            string propertyValue = propertyParts[1].Trim();

            // Convert the property name to camelCase
            propertyName = Regex.Replace(propertyName, "-([a-z])", m => m.Groups[1].Value.ToUpperInvariant());

            // Try to set the property value on the object
            try
            {
                PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(obj, propertyValue);
                }
            }
            catch (Exception)
            {
                // Ignore any errors that occur while setting the property value
            }
        }

        return obj;
    }


}