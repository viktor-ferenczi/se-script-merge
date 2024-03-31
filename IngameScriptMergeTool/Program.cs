using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScriptMerge;

namespace IngameScriptMergeTool;

public static class Program
{
    private const string DefaultScriptNamespace = "Script";
    private const string defaultWhitelistPath = "whitelist.txt";
    private const string defaultTerminalPath = "terminal.xml";

    private static string defaultSolutionPath;

    public static async Task Main(string[] args)
    {
        defaultSolutionPath = Directory.GetFiles(".", "*.sln").FirstOrDefault() ?? "*.sln";

        var rootCommand = new RootCommand
        {
            new Option<string>(
                aliases: ["--solution", "-s"],
                getDefaultValue: () => defaultSolutionPath,
                description: "Path to the solution file, it must include all projects your script depends on"
            ),
            new Option<string>(
                aliases: ["--namespace", "-n"],
                getDefaultValue: () => DefaultScriptNamespace,
                description: "Comma separated list of namespaces containing the code to be merged into the script"
            ),
            new Option<string>(
                aliases: ["--deploy", "-d"],
                description: "Deploy the script into SE's IngameScripts folder with this name (prints to stdout if not given)"
            ),
            new Option<bool>(
                aliases: ["--minify", "-m"],
                getDefaultValue: () => false,
                description: "Minify the source code by removing all comments and most unnecessary whitespace"
            ),
            new Option<bool>(
                aliases: ["--unicode", "-u"],
                getDefaultValue: () => false,
                description: "Shorten all names to single Unicode letters, exclusion: //! KeepThisName"
            ),
            new Option<bool>(
                aliases: ["--aggressive", "-a"],
                getDefaultValue: () => false,
                description: "Aggressively compress code (reduces repeated string literals) (requires --unicode)"
            ),
            new Option<bool>(
                aliases: ["--release", "-r"],
                getDefaultValue: () => false,
                description: "Enables release mode, it removes #ifdef DEBUG ... #endif blocks"
            ),
            new Option<string>(
                aliases: ["--whitelist", "-w"],
                getDefaultValue: () => defaultWhitelistPath,
                description: "Path of the API whitelist cache file"
            ),
            new Option<string>(
                aliases: ["--terminal", "-t"],
                getDefaultValue: () => defaultTerminalPath,
                description: "Path of the Terminal property cache file"
            ),
        };

        rootCommand.Description = "Command line tool to merge a Space Engineers script from multiple source code files.";
        rootCommand.Handler = CommandHandler.Create(HandleCommand);

        MergeExtensions.RecreateDebugDir();

        await rootCommand.InvokeAsync(args);
    }

    // The name of each handler function parameter must match the long name of their corresponding options
    private static async Task HandleCommand(string solution, string @namespace, string deploy, bool minify, bool unicode, bool aggressive, bool release, string whitelist, string terminal)
    {
        solution = Environment.ExpandEnvironmentVariables(solution);
        if (!File.Exists(solution))
        {
            await Console.Error.WriteLineAsync($"Solution file does not exist: {solution}");
            Environment.Exit(2);
        }

        var mergedScript = await new ScriptMerger(solution, @namespace, minify, unicode, aggressive, release, whitelist, terminal).Merge();

        if (mergedScript.Errors != null)
        {
            foreach (var error in mergedScript.Errors)
            {
                await Console.Error.WriteLineAsync(error);
            }
            Environment.Exit(-1);
        }

        DebugWriteScriptSource(solution, mergedScript.Text);

        if (string.IsNullOrEmpty(deploy))
        {
            Console.WriteLine(mergedScript);
            return;
        }

        SaveScript(deploy, mergedScript.Text);
    }

    private static void SaveScript(string deploy, string mergedCode)
    {
        var appDataDir = Environment.GetEnvironmentVariable("AppData") ?? ".";

        var pathElements = new List<string> { appDataDir, "SpaceEngineers", "IngameScripts", "local" };
        pathElements.AddRange(deploy.Split(['/', '\\']));

        var targetDir = Path.Combine(pathElements.ToArray());
        Directory.CreateDirectory(targetDir);

        var path = Path.Combine(targetDir, "Script.cs");
        File.WriteAllText(path, mergedCode, Encoding.UTF8);
    }

    private static void DebugWriteScriptSource(string solution, string mergedScript)
    {
#if DEBUG
        var solutionDir = Path.GetDirectoryName(solution) ?? ".";
        var scriptProjectDir = Path.Combine(solutionDir, "Script");
        if (!Directory.Exists(scriptProjectDir))
        {
            return;
        }

        var templatePath = Path.Combine(scriptProjectDir, "DebugMergedScript.cs.template.txt");
        var scriptPath = Path.Combine(scriptProjectDir, "DebugMergedScript.cs");
        if (!File.Exists(templatePath) || !File.Exists(scriptPath))
        {
            return;
        }

        var template = File.ReadAllText(templatePath);
        var substituted = template.Replace("//> DEBUG_MERGED_SCRIPT", mergedScript);
        File.WriteAllText(scriptPath, substituted);
#endif
    }
}