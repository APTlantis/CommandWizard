using System;
using System.Linq;
using CommandWizard.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CommandWizard.Services
{
    public static class SchemaSerialization
    {
        public static ToolSchema Parse(string tomlText)
        {
            var result = Toml.Parse(tomlText);
            if (result.HasErrors)
            {
                var error = string.Join("; ", result.Diagnostics.Select(d => d.ToString()));
                throw new InvalidOperationException(error);
            }

            var model = Toml.ToModel(tomlText) as TomlTable
                ?? throw new InvalidOperationException("Schema root is not a table.");

            var schema = new ToolSchema();

            if (model.TryGetValue("tool", out var toolObj) && toolObj is TomlTable toolTable)
            {
                schema.Name = toolTable.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "" : "";
                schema.Description = toolTable.TryGetValue("description", out var descObj) ? descObj?.ToString() ?? "" : "";
                schema.ExecutablePath = toolTable.TryGetValue("executable_path", out var pathObj) ? pathObj?.ToString() ?? "" : "";
                schema.InstalledName = toolTable.TryGetValue("installed_name", out var installedObj) ? installedObj?.ToString() ?? "" : "";
            }

            if (model.TryGetValue("actions", out var actionsObj) && actionsObj is TomlTableArray actionsArray)
            {
                foreach (TomlTable actionTable in actionsArray)
                {
                    var action = new SchemaAction
                    {
                        Name = actionTable.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "" : "",
                        Description = actionTable.TryGetValue("description", out var descObj) ? descObj?.ToString() ?? "" : ""
                    };
                    if (!string.IsNullOrWhiteSpace(action.Name))
                    {
                        schema.Actions.Add(action);
                    }
                }
            }

            if (model.TryGetValue("arguments", out var argsObj) && argsObj is TomlTableArray argsArray)
            {
                foreach (TomlTable argTable in argsArray)
                {
                    var argument = new SchemaArgument
                    {
                        Flag = argTable.TryGetValue("flag", out var flagObj) ? flagObj?.ToString() ?? "" : "",
                        Long = argTable.TryGetValue("long", out var longObj) ? longObj?.ToString() ?? "" : "",
                        Description = argTable.TryGetValue("description", out var descObj) ? descObj?.ToString() ?? "" : "",
                        Type = argTable.TryGetValue("type", out var typeObj) ? typeObj?.ToString() ?? "boolean" : "boolean"
                    };
                    schema.Arguments.Add(argument);
                }
            }

            if (model.TryGetValue("parameters", out var paramsObj) && paramsObj is TomlTableArray paramsArray)
            {
                foreach (TomlTable paramTable in paramsArray)
                {
                    var required = true;
                    if (paramTable.TryGetValue("required", out var reqObj) && bool.TryParse(reqObj?.ToString(), out var req))
                    {
                        required = req;
                    }

                    var parameter = new SchemaParameter
                    {
                        Name = paramTable.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "" : "",
                        Type = paramTable.TryGetValue("type", out var typeObj) ? typeObj?.ToString() ?? "string" : "string",
                        Required = required
                    };

                    if (!string.IsNullOrWhiteSpace(parameter.Name))
                    {
                        schema.Parameters.Add(parameter);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                throw new InvalidOperationException("Schema is missing tool.name.");
            }

            return schema;
        }

        public static string Serialize(ToolSchema schema)
        {
            var root = new TomlTable();
            var toolTable = new TomlTable
            {
                ["name"] = schema.Name,
                ["description"] = schema.Description
            };
            if (!string.IsNullOrWhiteSpace(schema.ExecutablePath))
            {
                toolTable["executable_path"] = schema.ExecutablePath;
            }
            if (!string.IsNullOrWhiteSpace(schema.InstalledName))
            {
                toolTable["installed_name"] = schema.InstalledName;
            }
            root["tool"] = toolTable;

            if (schema.Actions.Count > 0)
            {
                var actions = new TomlTableArray();
                foreach (var action in schema.Actions)
                {
                    actions.Add(new TomlTable
                    {
                        ["name"] = action.Name,
                        ["description"] = action.Description
                    });
                }
                root["actions"] = actions;
            }

            if (schema.Arguments.Count > 0)
            {
                var args = new TomlTableArray();
                foreach (var arg in schema.Arguments)
                {
                    var entry = new TomlTable
                    {
                        ["flag"] = arg.Flag,
                        ["description"] = arg.Description,
                        ["type"] = arg.Type
                    };
                    if (!string.IsNullOrWhiteSpace(arg.Long))
                    {
                        entry["long"] = arg.Long;
                    }
                    args.Add(entry);
                }
                root["arguments"] = args;
            }

            if (schema.Parameters.Count > 0)
            {
                var parameters = new TomlTableArray();
                foreach (var param in schema.Parameters)
                {
                    parameters.Add(new TomlTable
                    {
                        ["name"] = param.Name,
                        ["type"] = param.Type,
                        ["required"] = param.Required
                    });
                }
                root["parameters"] = parameters;
            }

            return Toml.FromModel(root);
        }
    }
}
