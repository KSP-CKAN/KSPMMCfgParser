using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CommandLine;
using log4net;
using log4net.Config;
using log4net.Core;
using ParsecSharp;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;

namespace KSPMMCfgValidator
{
    /// <summary>
    /// Main class for validator
    /// </summary>
    public class KSPMMCfgValidator
    {
        /// <summary>
        /// Entry point for validator
        /// </summary>
        /// <param name="args">Command line arguments, interpreted as paths to scan</param>
        /// <returns>
        /// Standard Unix exit codes (0=success, 1=badopt, 2=fail)
        /// </returns>
        public static int Main(string[] args)
        {
            var logRepo = LogManager.GetRepository(Assembly.GetExecutingAssembly());
            BasicConfigurator.Configure(logRepo);
            logRepo.Threshold = Level.Warn;

            try
            {
                var options = new ValidatorOptions();
                CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
                var results = FindFromPaths(options.Paths).SelectMany(ParseCfgFile)
                                                          .ToArray();
                return results.Contains(null) ? ExitError : ExitOk;
            }
            catch (Exception exc)
            {
                log.Error(Properties.Resources.ErrorValidating, exc);
                return ExitError;
            }
        }

        private static IEnumerable<KSPConfigNode?> ParseCfgFile(string file)
            => ConfigFile.Parse(File.Open(file, FileMode.Open))
                         .CaseOf(failure =>
                                 {
                                     Console.WriteLine(errorFormat,
                                                       file,
                                                       failure.State.Position.Line,
                                                       failure.State.Position.Column,
                                                       failure.Message);
                                     // Inject null into sequence so caller can return failure
                                     return Enumerable.Repeat<KSPConfigNode?>(null, 1);
                                 },
                                 success => success.Value);

        private static IEnumerable<string> FindFromPaths(List<string> paths)
            => paths.Count < 1
                ? FindCfgFiles(".").Select(file =>
                    file.TrimStart('.', Path.DirectorySeparatorChar))
                : paths.SelectMany(path =>
                    File.Exists(path) ? Enumerable.Repeat(path, 1)
                                      : FindCfgFiles(path));

        private static IEnumerable<string> FindCfgFiles(string dir)
            => Directory.EnumerateFiles(dir, "*.cfg",
                                        SearchOption.AllDirectories);

        private static readonly string[] GitHubEnvVars = new string[]
        {
            "GITHUB_ACTIONS",
            "GITHUB_WORKFLOW"
        };

        private static readonly bool inGitHub =
            GitHubEnvVars.Select(Environment.GetEnvironmentVariable)
                         .All(v => v != null);

        private static readonly string errorFormat =
            inGitHub ? "::error file={0},line={1},col={2}::{3}"
                     : "{0}:{1}:{2}: {3}";

        private const int ExitOk     = 0;
        private const int ExitError  = 2;

        private static readonly ILog log = LogManager.GetLogger(typeof(KSPMMCfgValidator));
    }

    /// <summary>
    /// Command line options parser
    /// </summary>
    public class ValidatorOptions
    {
        /// <summary>
        /// Paths to scan specified on command line
        /// </summary>
        [ValueList(typeof(List<string>))]
        public List<string> Paths { get; set; } = new List<string>();
    }
}
