using System.Collections.Generic;
using CommandWizard.ViewModels;
using CommandWizard.Models;

namespace CommandWizard.Services
{
    public static class CommandBuilder
    {
        public static string BuildCommand(ToolSchemaViewModel? tool, SchemaAction? action)
        {
            if (tool == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            var toolName = tool.CommandName;
            if (!string.IsNullOrWhiteSpace(toolName))
            {
                parts.Add(toolName);
            }

            if (action != null && !string.IsNullOrWhiteSpace(action.Name))
            {
                parts.Add(action.Name);
            }

            foreach (var option in tool.Options)
            {
                if (option.IsBoolean)
                {
                    if (option.IsSelected)
                    {
                        var token = option.DisplayName;
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            parts.Add(token);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(option.Value))
                    {
                        var token = option.DisplayName;
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            parts.Add(token);
                            parts.Add(option.Value.Trim());
                        }
                    }
                }
            }

            foreach (var parameter in tool.Parameters)
            {
                if (!string.IsNullOrWhiteSpace(parameter.Value))
                {
                    parts.Add(parameter.Value.Trim());
                }
            }

            return string.Join(" ", parts);
        }
    }
}
