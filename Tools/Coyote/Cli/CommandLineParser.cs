// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Testing;
using Microsoft.Coyote.Visualization;

namespace Microsoft.Coyote.Cli
{
    internal sealed class CommandLineParser
    {
        /// <summary>
        /// The Coyote runtime and testing configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The Coyote rewriting options.
        /// </summary>
        private readonly RewritingOptions RewritingOptions;

        /// <summary>
        /// The test command.
        /// </summary>
        private readonly Command TestCommand;

        /// <summary>
        /// The replay command.
        /// </summary>
        private readonly Command ReplayCommand;

        /// <summary>
        /// The rewrite command.
        /// </summary>
        private readonly Command RewriteCommand;

        /// <summary>
        /// Map from argument names to arguments.
        /// </summary>
        private readonly Dictionary<string, Argument> Arguments;

        /// <summary>
        /// Map from option names to options.
        /// </summary>
        private readonly Dictionary<string, Option> Options;

        /// <summary>
        /// The parse results.
        /// </summary>
        private readonly ParseResult Results;

        /// <summary>
        /// True if parsing was successful, else false.
        /// </summary>
        internal bool IsSuccessful { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParser"/> class.
        /// </summary>
        internal CommandLineParser(string[] args)
        {
            this.Configuration = Configuration.Create();
            this.RewritingOptions = RewritingOptions.Create();
            this.Arguments = new Dictionary<string, Argument>();
            this.Options = new Dictionary<string, Option>();

            var allowedVerbosityLevels = new HashSet<string>
            {
                "none",
                "error",
                "warning",
                "info",
                "debug",
                "exhaustive"
            };

            var verbosityOption = new Option<string>("--verbosity", "-v")
            {
                Description = "Enable verbosity with an optional verbosity level. " +
                    $"Allowed values are {string.Join(", ", allowedVerbosityLevels)}. " +
                    "Skipping the argument sets the verbosity level to 'info'.",
                DefaultValueFactory = _ => "error",
                HelpName = "LEVEL",
                Arity = ArgumentArity.ZeroOrOne,
                Recursive = true
            };

            var consoleLoggingOption = new Option<bool>("--console")
            {
                Description = "Log all runtime messages to the console unless overridden by a custom ILogger.",
                Arity = ArgumentArity.Zero,
                Recursive = true
            };

            // Add validators.
            verbosityOption.Validators.Add(result => ValidateOptionValueIsAllowed(result, allowedVerbosityLevels));

            // Create the commands.
            this.TestCommand = this.CreateTestCommand(this.Configuration);
            this.ReplayCommand = this.CreateReplayCommand();
            this.RewriteCommand = this.CreateRewriteCommand(this.RewritingOptions);

            // Create the root command.
            var rootCommand = new RootCommand("The Coyote systematic testing tool.\n\n" +
                $"Learn how to use Coyote at {Documentation.LearnAboutCoyoteUrl}.\nLearn what is new at {Documentation.LearnWhatIsNewUrl}.");
            this.AddGlobalOption(rootCommand, verbosityOption);
            this.AddGlobalOption(this.TestCommand, consoleLoggingOption);
            this.AddGlobalOption(this.ReplayCommand, consoleLoggingOption);
            rootCommand.Subcommands.Add(this.TestCommand);
            rootCommand.Subcommands.Add(this.ReplayCommand);
            rootCommand.Subcommands.Add(this.RewriteCommand);
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            var parserConfiguration = new ParserConfiguration
            {
                EnablePosixBundling = false
            };

            this.Results = rootCommand.Parse(args, parserConfiguration);
            if (this.Results.Errors.Any() || this.Results.Action is not null)
            {
                // There are parsing errors, or a help or version request materialized as a
                // parse-time action (the command handlers are attached after parsing, so any
                // action present here cannot be one of them). Invoke the result to print the
                // errors, help or version message.
                this.Results.Invoke();
                this.IsSuccessful = false;
            }
            else
            {
                // There were no errors, so use the parsed results to update the default configurations.
                this.UpdateConfigurations(this.Results);
                this.IsSuccessful = true;
            }
        }

        /// <summary>
        /// Invokes the command selected by the user.
        /// </summary>
        internal ExitCode InvokeCommand()
        {
            PrintDetailedCoyoteVersion();
            return (ExitCode)this.Results.Invoke();
        }

        /// <summary>
        /// Sets the handler to be invoked when the test command is selected by the user.
        /// </summary>
        internal void SetTestCommandHandler(Func<Configuration, ExitCode> testHandler)
        {
            this.TestCommand.SetAction(parseResult => (int)testHandler(this.Configuration));
        }

        /// <summary>
        /// Sets the handler to be invoked when the replay command is selected by the user.
        /// </summary>
        internal void SetReplayCommandHandler(Func<Configuration, ExitCode> replayHandler)
        {
            this.ReplayCommand.SetAction(parseResult => (int)replayHandler(this.Configuration));
        }

        /// <summary>
        /// Sets the handler to be invoked when the rewrite command is selected by the user.
        /// </summary>
        internal void SetRewriteCommandHandler(Func<Configuration, RewritingOptions, ExitCode> rewriteHandler)
        {
            this.RewriteCommand.SetAction(parseResult => (int)rewriteHandler(
                this.Configuration, this.RewritingOptions));
        }

        /// <summary>
        /// Creates the test command.
        /// </summary>
        private Command CreateTestCommand(Configuration configuration)
        {
            var pathArg = new Argument<string>("path")
            {
                Description = $"Path to the assembly (*.dll, *.exe) to test.",
                HelpName = "PATH"
            };

            var methodOption = new Option<string>("--method", "-m")
            {
                Description = "Suffix of the test method to execute.",
                HelpName = "METHOD",
                Arity = ArgumentArity.ExactlyOne
            };

            var iterationsOption = new Option<int>("--iterations", "-i")
            {
                Description = "Number of testing iterations to run.",
                DefaultValueFactory = _ => (int)configuration.TestingIterations,
                HelpName = "ITERATIONS",
                Arity = ArgumentArity.ExactlyOne
            };

            var timeoutOption = new Option<int>("--timeout", "-t")
            {
                Description = "Timeout in seconds after which no more testing iterations will run (disabled by default).",
                DefaultValueFactory = _ => configuration.TestingTimeout,
                HelpName = "TIMEOUT",
                Arity = ArgumentArity.ExactlyOne
            };

            var allowedStrategies = new HashSet<string>
            {
                "random",
                "probabilistic",
                "prioritization",
                "fair-prioritization",
                "delay-bounding",
                "fair-delay-bounding",
                "q-learning"
            };

            var strategyOption = new Option<string>("--strategy", "-s")
            {
                Description = "Set exploration strategy to use during testing. The exploration strategy controls " +
                    "all scheduling decisions and nondeterministic choices. Note that explicitly setting this " +
                    "value disables the default exploration mode that uses a tuned portfolio of strategies. " +
                    $"Allowed values are {string.Join(", ", allowedStrategies)}.",
                DefaultValueFactory = _ => configuration.ExplorationStrategy.GetName(),
                HelpName = "STRATEGY",
                Arity = ArgumentArity.ExactlyOne
            };

            var strategyValueOption = new Option<int>("--strategy-value", "-sv")
            {
                Description = "Set exploration strategy specific value. Supported strategies (and values): " +
                    "probabilistic (probability of deviating from a scheduled operation), " +
                    "(fair-)prioritization (maximum number of priority changes per iteration), " +
                    "(fair-)delay-bounding (maximum number of delays per iteration).",
                HelpName = "VALUE",
                Arity = ArgumentArity.ExactlyOne
            };

            var allowedPortfolioMode = new HashSet<string>
            {
                "fair",
                "unfair"
            };

            var portfolioModeOption = new Option<string>("--portfolio-mode")
            {
                Description = "Set the portfolio mode to use during testing. Portfolio mode uses a tuned portfolio " +
                    "of strategies, instead of the default or user-specified strategy. If fair mode is enabled, " +
                    "then the portfolio will upgrade any unfair strategies to fair, by adding a fair execution " +
                    "suffix after the the max fair scheduling steps bound has been reached. " +
                    $"Allowed values are {string.Join(", ", allowedPortfolioMode)}.",
                DefaultValueFactory = _ => configuration.PortfolioMode.ToString().ToLower(),
                HelpName = "MODE",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxStepsOption = new Option<int>("--max-steps", "-ms")
            {
                Description = "Max scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Choosing value 'STEPS' sets 'STEPS' unfair max-steps and 'STEPS*10' fair steps.",
                HelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxFairStepsOption = new Option<int>("--max-fair-steps")
            {
                Description = "Max fair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform fair scheduling.",
                DefaultValueFactory = _ => configuration.MaxFairSchedulingSteps,
                HelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxUnfairStepsOption = new Option<int>("--max-unfair-steps")
            {
                Description = "Max unfair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform unfair scheduling.",
                DefaultValueFactory = _ => configuration.MaxUnfairSchedulingSteps,
                HelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
            };

            var fuzzOption = new Option<bool>("--fuzz")
            {
                Description = "Use systematic fuzzing instead of controlled testing.",
                Arity = ArgumentArity.Zero
            };

            var coverageOption = new Option<bool>("--coverage", "-c")
            {
                Description = "Generate coverage reports if supported for the programming model used by the test.",
                Arity = ArgumentArity.Zero
            };

            var scheduleCoverageOption = new Option<bool>("--schedule-coverage")
            {
                Description = "Output a '.coverage.schedule.txt' file containing scheduling coverage information during testing.",
                Arity = ArgumentArity.Zero
            };

            var serializeCoverageInfoOption = new Option<bool>("--serialize-coverage")
            {
                Description = "Output a '.coverage.ser' file that contains the serialized coverage information.",
                Arity = ArgumentArity.Zero
            };

            var graphOption = new Option<bool>("--actor-graph")
            {
                Description = "Output a DGML graph that visualizes the failing actor execution path if a bug is found.",
                Arity = ArgumentArity.Zero
            };

            var xmlLogOption = new Option<bool>("--xml-trace")
            {
                Description = "Output an XML formatted runtime log file.",
                Arity = ArgumentArity.Zero
            };

            var reduceExecutionTraceCyclesOption = new Option<bool>("--reduce-execution-trace-cycles")
            {
                Description = "Enable execution trace cycle detection and reduction heuristics.",
                Arity = ArgumentArity.Zero
            };

            var samplePartialOrdersOption = new Option<bool>("--partial-order-sampling")
            {
                Description = "Enable partial-order sampling based on 'READ' and 'WRITE' scheduling points.",
                Arity = ArgumentArity.Zero
            };

            var seedOption = new Option<uint>("--seed")
            {
                Description = "Specify the random value generator seed.",
                HelpName = "VALUE",
                Arity = ArgumentArity.ExactlyOne
            };

            var livenessTemperatureThresholdOption = new Option<int>("--liveness-temperature-threshold")
            {
                Description = "Specify the threshold (in number of steps) that triggers a liveness bug.",
                DefaultValueFactory = _ => configuration.LivenessTemperatureThreshold,
                HelpName = "THRESHOLD",
                Arity = ArgumentArity.ExactlyOne
            };

            var timeoutDelayOption = new Option<int>("--timeout-delay")
            {
                Description = "Specify the frequency of timeouts (not a unit of time).",
                DefaultValueFactory = _ => (int)configuration.TimeoutDelay,
                HelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var deadlockTimeoutOption = new Option<int>("--deadlock-timeout")
            {
                Description = "Specify how much time (in ms) to wait before reporting a potential deadlock.",
                DefaultValueFactory = _ => (int)configuration.DeadlockTimeout,
                HelpName = "TIMEOUT",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxFuzzDelayOption = new Option<int>("--max-fuzz-delay")
            {
                Description = "Specify the maximum time (in number of busy loops) an operation might " +
                    "get delayed during systematic fuzzing.",
                DefaultValueFactory = _ => (int)configuration.MaxFuzzingDelay,
                HelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var uncontrolledConcurrencyResolutionAttemptsOption = new Option<int>("--resolve-uncontrolled-concurrency-attempts")
            {
                Description = "Specify how many times to try resolve each instance of uncontrolled concurrency.",
                DefaultValueFactory = _ => (int)configuration.UncontrolledConcurrencyResolutionAttempts,
                HelpName = "ATTEMPTS",
                Arity = ArgumentArity.ExactlyOne
            };

            var uncontrolledConcurrencyResolutionDelayOption = new Option<int>("--resolve-uncontrolled-concurrency-delay")
            {
                Description = "Specify how much time (in number of busy loops) to wait between each attempt to " +
                    "resolve each instance of uncontrolled concurrency.",
                DefaultValueFactory = _ => (int)configuration.UncontrolledConcurrencyResolutionDelay,
                HelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var skipTraceAnalysisOption = new Option<bool>("--skip-trace-analysis")
            {
                Description = "Disable execution graph analysis during testing.",
                Arity = ArgumentArity.Zero
            };

            var skipPotentialDeadlocksOption = new Option<bool>("--skip-potential-deadlocks")
            {
                Description = "Only report a deadlock when the runtime can fully determine that it is genuine " +
                    "and not due to partially-controlled concurrency.",
                Arity = ArgumentArity.Zero
            };

            var skipCollectionRacesOption = new Option<bool>("--skip-collection-races")
            {
                Description = "Disable exploration of race conditions when accessing collections.",
                Arity = ArgumentArity.Zero
            };

            var skipLockRacesOption = new Option<bool>("--skip-lock-races")
            {
                Description = "Disable exploration of race conditions when accessing lock-based synchronization primitives.",
                Arity = ArgumentArity.Zero
            };

            var skipAtomicRacesOption = new Option<bool>("--skip-atomic-races")
            {
                Description = "Disable exploration of race conditions when performing atomic operations.",
                Arity = ArgumentArity.Zero
            };

            var skipVolatileRacesOption = new Option<bool>("--skip-volatile-races")
            {
                Description = "Disable exploration of race conditions when performing volatile operations.",
                Arity = ArgumentArity.Zero
            };

            var enableMemoryAccessRacesOption = new Option<bool>("--enable-memory-access-races")
            {
                Description = "Enable exploration of race conditions at memory-access locations.",
                Arity = ArgumentArity.Zero
            };

            var enableControlFlowRacesOption = new Option<bool>("--enable-control-flow-races")
            {
                Description = "Enable exploration of race conditions at control-flow branching locations.",
                Arity = ArgumentArity.Zero
            };

            var fuzzingFallbackOption = new Option<bool>("--fuzzing-fallback")
            {
                Description = "Enable automatic fallback to systematic fuzzing upon detecting uncontrolled concurrency.",
                Arity = ArgumentArity.Zero
            };

            var allowedPartialControlModes = new HashSet<string>
            {
                "none",
                "concurrency",
                "data"
            };

            var partialControlOption = new Option<string>("--partial-control")
            {
                Description = "Set the partial controlled mode to use during testing. If set to 'concurrency' then " +
                    "only concurrency can be partially controlled. If set to 'data' then only data non-determinism " +
                    "can be partially controlled. If set to 'none' then partially controlled testing is disabled. " +
                    "By default, both concurrency and data non-determinism can be partially controlled. " +
                    $"Allowed values are {string.Join(", ", allowedPartialControlModes)}.",
                HelpName = "MODE",
                Arity = ArgumentArity.ExactlyOne
            };

            var noReproOption = new Option<bool>("--no-repro")
            {
                Description = "Disable bug trace repro to ignore uncontrolled concurrency errors.",
                Arity = ArgumentArity.Zero
            };

            var logUncontrolledInvocationStackTracesOption = new Option<bool>("--log-uncontrolled-invocation-stack-traces")
            {
                Description = "Enable logging the stack traces of uncontrolled invocations detected during testing.",
                Arity = ArgumentArity.Zero
            };

            var failOnMaxStepsOption = new Option<bool>("--fail-on-max-steps")
            {
                Description = "Reaching the specified max-steps is treated as a bug.",
                Arity = ArgumentArity.Zero
            };

            var exploreOption = new Option<bool>("--explore")
            {
                Description = "Keep testing until the bound (e.g. iteration or time) is reached.",
                Arity = ArgumentArity.Zero,
                Hidden = true
            };

            var breakOption = new Option<bool>("--break", "-b")
            {
                Description = "Attach the debugger and add a breakpoint when an assertion fails.",
                Arity = ArgumentArity.Zero
            };

            var outputDirectoryOption = new Option<string>("--outdir", "-o")
            {
                Description = "Output directory for emitting reports. This can be an absolute path or relative to current directory.",
                HelpName = "PATH",
                Arity = ArgumentArity.ExactlyOne
            };

            // Add validators.
            pathArg.Validators.Add(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe"));
            iterationsOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            strategyOption.Validators.Add(result => ValidateOptionValueIsAllowed(result, allowedStrategies));
            strategyOption.Validators.Add(result => ValidateExclusiveOptionValueIsAvailable(result, portfolioModeOption));
            strategyValueOption.Validators.Add(result => ValidatePrerequisiteOptionValueIsAvailable(result, strategyOption));
            portfolioModeOption.Validators.Add(result => ValidateOptionValueIsAllowed(result, allowedPortfolioMode));
            maxStepsOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            maxStepsOption.Validators.Add(result => ValidateExclusiveOptionValueIsAvailable(result, maxFairStepsOption));
            maxStepsOption.Validators.Add(result => ValidateExclusiveOptionValueIsAvailable(result, maxUnfairStepsOption));
            maxFairStepsOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            maxFairStepsOption.Validators.Add(result => ValidateExclusiveOptionValueIsAvailable(result, maxStepsOption));
            maxUnfairStepsOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            maxUnfairStepsOption.Validators.Add(result => ValidateExclusiveOptionValueIsAvailable(result, maxStepsOption));
            serializeCoverageInfoOption.Validators.Add(result => ValidatePrerequisiteOptionValueIsAvailable(result, coverageOption));
            seedOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            livenessTemperatureThresholdOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutDelayOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            deadlockTimeoutOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            maxFuzzDelayOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyResolutionAttemptsOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyResolutionDelayOption.Validators.Add(result => ValidateOptionValueIsUnsignedInteger(result));
            partialControlOption.Validators.Add(result => ValidateOptionValueIsAllowed(result, allowedPartialControlModes));

            // Build command.
            var command = new Command("test", "Run tests using the Coyote systematic testing engine.\n" +
                $"Learn more at {Documentation.LearnAboutTestUrl}.");
            this.AddArgument(command, pathArg);
            this.AddOption(command, methodOption);
            this.AddOption(command, iterationsOption);
            this.AddOption(command, timeoutOption);
            this.AddOption(command, strategyOption);
            this.AddOption(command, strategyValueOption);
            this.AddOption(command, portfolioModeOption);
            this.AddOption(command, maxStepsOption);
            this.AddOption(command, maxFairStepsOption);
            this.AddOption(command, maxUnfairStepsOption);
            this.AddOption(command, fuzzOption);
            this.AddOption(command, coverageOption);
            this.AddOption(command, scheduleCoverageOption);
            this.AddOption(command, serializeCoverageInfoOption);
            this.AddOption(command, graphOption);
            this.AddOption(command, xmlLogOption);
            this.AddOption(command, reduceExecutionTraceCyclesOption);
            this.AddOption(command, samplePartialOrdersOption);
            this.AddOption(command, seedOption);
            this.AddOption(command, livenessTemperatureThresholdOption);
            this.AddOption(command, timeoutDelayOption);
            this.AddOption(command, deadlockTimeoutOption);
            this.AddOption(command, maxFuzzDelayOption);
            this.AddOption(command, uncontrolledConcurrencyResolutionAttemptsOption);
            this.AddOption(command, uncontrolledConcurrencyResolutionDelayOption);
            this.AddOption(command, skipTraceAnalysisOption);
            this.AddOption(command, skipPotentialDeadlocksOption);
            this.AddOption(command, skipCollectionRacesOption);
            this.AddOption(command, skipLockRacesOption);
            this.AddOption(command, skipAtomicRacesOption);
            this.AddOption(command, skipVolatileRacesOption);
            this.AddOption(command, enableMemoryAccessRacesOption);
            this.AddOption(command, enableControlFlowRacesOption);
            this.AddOption(command, fuzzingFallbackOption);
            this.AddOption(command, partialControlOption);
            this.AddOption(command, noReproOption);
            this.AddOption(command, logUncontrolledInvocationStackTracesOption);
            this.AddOption(command, failOnMaxStepsOption);
            this.AddOption(command, exploreOption);
            this.AddOption(command, breakOption);
            this.AddOption(command, outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the replay command.
        /// </summary>
        private Command CreateReplayCommand()
        {
            var pathArg = new Argument<string>("path")
            {
                Description = $"Path to the assembly (*.dll, *.exe) to replay.",
                HelpName = "PATH"
            };

            var traceFileArg = new Argument<string>("trace")
            {
                Description = $"*.trace file containing the execution path to replay.",
                HelpName = "TRACE_FILE"
            };

            var methodOption = new Option<string>("--method", "-m")
            {
                Description = "Suffix of the test method to execute.",
                HelpName = "METHOD",
                Arity = ArgumentArity.ExactlyOne
            };

            var breakOption = new Option<bool>("--break", "-b")
            {
                Description = "Attaches the debugger and adds a breakpoint when an assertion fails.",
                Arity = ArgumentArity.Zero
            };

            var outputDirectoryOption = new Option<string>("--outdir", "-o")
            {
                Description = "Output directory for emitting reports. This can be an absolute path or relative to current directory.",
                HelpName = "PATH",
                Arity = ArgumentArity.ExactlyOne
            };

            // Add validators.
            pathArg.Validators.Add(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe"));
            traceFileArg.Validators.Add(result => ValidateArgumentValueIsExpectedFile(result, ".trace"));

            // Build command.
            var command = new Command("replay", "Replay bugs that Coyote discovered during systematic testing.\n" +
                $"Learn more at {Documentation.LearnAboutReplayUrl}.");
            this.AddArgument(command, pathArg);
            this.AddArgument(command, traceFileArg);
            this.AddOption(command, methodOption);
            this.AddOption(command, breakOption);
            this.AddOption(command, outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the rewrite command.
        /// </summary>
        private Command CreateRewriteCommand(RewritingOptions options)
        {
            var pathArg = new Argument<string>("path")
            {
                Description = "Path to the assembly (*.dll, *.exe) to rewrite or to a JSON rewriting configuration file.",
                HelpName = "PATH"
            };

            var rewriteMemoryLocationsOption = new Option<bool>("--rewrite-memory-locations")
            {
                Description = "Rewrite memory locations (such as field loads and stores) for race checking.",
                DefaultValueFactory = _ => options.IsRewritingMemoryLocations,
                Arity = ArgumentArity.ExactlyOne
            };

            var rewriteConcurrentCollectionsOption = new Option<bool>("--rewrite-concurrent-collections")
            {
                Description = "Rewrite concurrent collections for race checking.",
                DefaultValueFactory = _ => options.IsRewritingConcurrentCollections,
                Arity = ArgumentArity.ExactlyOne
            };

            var rewriteDependenciesOption = new Option<bool>("--rewrite-dependencies")
            {
                Description = "Rewrite all dependent assemblies that are found in the same location as the given path.",
                DefaultValueFactory = _ => options.IsRewritingDependencies,
                Arity = ArgumentArity.Zero,
                Hidden = true
            };

            var rewriteUnitTestsOption = new Option<bool>("--rewrite-unit-tests")
            {
                Description = "Rewrite unit tests to automatically inject the Coyote testing engine.",
                DefaultValueFactory = _ => options.IsRewritingUnitTests,
                Arity = ArgumentArity.Zero,
                Hidden = true
            };

            var assertDataRacesOption = new Option<bool>("--assert-data-races")
            {
                Description = "Add assertions for read/write data races.",
                DefaultValueFactory = _ => options.IsDataRaceCheckingEnabled,
                Arity = ArgumentArity.Zero,
                Hidden = true
            };

            var dumpILOption = new Option<bool>("--dump-il")
            {
                Description = "Dumps the original and rewritten IL in JSON for debugging purposes.",
                DefaultValueFactory = _ => options.IsLoggingAssemblyContents,
                Arity = ArgumentArity.Zero
            };

            var dumpILDiffOption = new Option<bool>("--dump-il-diff")
            {
                Description = "Dumps the IL diff in JSON for debugging purposes.",
                DefaultValueFactory = _ => options.IsDiffingAssemblyContents,
                Arity = ArgumentArity.Zero
            };

            // Add validators.
            pathArg.Validators.Add(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe", ".json"));

            // Build command.
            var command = new Command("rewrite", "Rewrite your assemblies to inject logic that allows " +
                "Coyote to take control of the execution during systematic testing.\n" +
                $"Learn more at {Documentation.LearnAboutRewritingUrl}.");
            this.AddArgument(command, pathArg);
            this.AddOption(command, rewriteMemoryLocationsOption);
            this.AddOption(command, rewriteConcurrentCollectionsOption);
            this.AddOption(command, rewriteDependenciesOption);
            this.AddOption(command, rewriteUnitTestsOption);
            this.AddOption(command, assertDataRacesOption);
            this.AddOption(command, dumpILOption);
            this.AddOption(command, dumpILDiffOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Adds an argument to the specified command.
        /// </summary>
        private void AddArgument(Command command, Argument argument)
        {
            command.Arguments.Add(argument);
            if (!this.Arguments.ContainsKey(argument.Name))
            {
                this.Arguments.Add(argument.Name, argument);
            }
        }

        /// <summary>
        /// Adds a global option to the specified command.
        /// </summary>
        private void AddGlobalOption(Command command, Option option)
        {
            option.Recursive = true;
            this.AddOption(command, option);
        }

        /// <summary>
        /// Adds an option to the specified command.
        /// </summary>
        private void AddOption(Command command, Option option)
        {
            command.Options.Add(option);
            string name = GetNormalizedName(option);
            if (!this.Options.ContainsKey(name))
            {
                this.Options.Add(name, option);
            }
        }

        /// <summary>
        /// Returns the name of the specified option without its '--' prefix, matching
        /// the prefix-less names this parser keys its lookup tables and switches on.
        /// </summary>
        private static string GetNormalizedName(Option option) => option.Name.TrimStart('-');

        /// <summary>
        /// Validates that the specified argument result is found and has an expected file extension.
        /// </summary>
        private static void ValidateArgumentValueIsExpectedFile(ArgumentResult result, params string[] extensions)
        {
            string fileName = result.GetValueOrDefault<string>();
            string foundExtension = Path.GetExtension(fileName);
            if (!extensions.Any(extension => extension == foundExtension))
            {
                if (extensions.Length is 1)
                {
                    result.AddError($"File '{fileName}' does not have the expected '{extensions[0]}' extension.");
                }
                else
                {
                    result.AddError($"File '{fileName}' does not have one of the expected extensions: " +
                        $"{string.Join(", ", extensions)}.");
                }
            }
            else if (!File.Exists(fileName))
            {
                result.AddError($"File '{fileName}' does not exist.");
            }
        }

        /// <summary>
        /// Validates that the specified option result is an unsigned integer.
        /// </summary>
        private static void ValidateOptionValueIsUnsignedInteger(OptionResult result)
        {
            if (result.Tokens.Select(token => token.Value).Where(v => !uint.TryParse(v, out _)).Any())
            {
                result.AddError($"Please give a positive integer to option '{result.Option.Name}'.");
            }
        }

        /// <summary>
        /// Validates that the specified option result has an allowed value.
        /// </summary>
        private static void ValidateOptionValueIsAllowed(OptionResult result, IEnumerable<string> allowedValues)
        {
            if (result.Tokens.Select(token => token.Value).Where(v => !allowedValues.Contains(v)).Any())
            {
                result.AddError($"Please give an allowed value to option '{result.Option.Name}': " +
                    $"{string.Join(", ", allowedValues)}.");
            }
        }

        /// <summary>
        /// Validates that the specified prerequisite option is available.
        /// </summary>
        private static void ValidatePrerequisiteOptionValueIsAvailable(OptionResult result, Option prerequisite)
        {
            OptionResult prerequisiteResult = result.GetResult(prerequisite);
            if (!result.Implicit && (prerequisiteResult is null || prerequisiteResult.Implicit))
            {
                result.AddError($"Setting option '{result.Option.Name}' requires option '{prerequisite.Name}'.");
            }
        }

        /// <summary>
        /// Validates that the specified exclusive option is available.
        /// </summary>
        private static void ValidateExclusiveOptionValueIsAvailable(OptionResult result, Option exclusive)
        {
            OptionResult exclusiveResult = result.GetResult(exclusive);
            if (!result.Implicit && exclusiveResult != null && !exclusiveResult.Implicit)
            {
                result.AddError($"Setting options '{result.Option.Name}' and '{exclusive.Name}' at the same time is not allowed.");
            }
        }

        /// <summary>
        /// Populates the configurations from the specified parse result.
        /// </summary>
        private void UpdateConfigurations(ParseResult result)
        {
            CommandResult commandResult = result.CommandResult;
            Command command = commandResult.Command;
            foreach (var symbolResult in commandResult.Children)
            {
                if (symbolResult is ArgumentResult argument)
                {
                    this.UpdateConfigurationsWithParsedArgument(command, argument);
                }
                else if (symbolResult is OptionResult option)
                {
                    this.UpdateConfigurationsWithParsedOption(option);
                }
            }
        }

        /// <summary>
        /// Updates the configuration with the specified parsed argument.
        /// </summary>
        private void UpdateConfigurationsWithParsedArgument(Command command, ArgumentResult result)
        {
            switch (result.Argument.Name)
            {
                case "path":
                    if (command.Name is "test" || command.Name is "replay")
                    {
                        // In the case of 'coyote test' or 'replay', the path is the assembly to be tested.
                        string path = Path.GetFullPath(result.GetValueOrDefault<string>());
                        this.Configuration.AssemblyToBeAnalyzed = path;
                    }
                    else if (command.Name is "rewrite")
                    {
                        // In the case of 'coyote rewrite', the path is the JSON this.Configuration file
                        // with the binary rewriting options.
                        string filename = result.GetValueOrDefault<string>();
                        if (Directory.Exists(filename))
                        {
                            // Then we want to rewrite a whole folder full of assemblies.
                            var assembliesDir = Path.GetFullPath(filename);
                            this.RewritingOptions.AssembliesDirectory = assembliesDir;
                            this.RewritingOptions.OutputDirectory = assembliesDir;
                        }
                        else
                        {
                            string extension = Path.GetExtension(filename);
                            if (string.Compare(extension, ".json", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                // Parse the rewriting options from the JSON file.
                                RewritingOptions.ParseFromJSON(this.RewritingOptions, filename);
                            }
                            else if (string.Compare(extension, ".dll", StringComparison.OrdinalIgnoreCase) is 0 ||
                                string.Compare(extension, ".exe", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                this.Configuration.AssemblyToBeAnalyzed = filename;
                                var fullPath = Path.GetFullPath(filename);
                                var assembliesDir = Path.GetDirectoryName(fullPath);
                                this.RewritingOptions.AssembliesDirectory = assembliesDir;
                                this.RewritingOptions.OutputDirectory = assembliesDir;
                                this.RewritingOptions.AssemblyPaths.Add(fullPath);
                            }
                        }
                    }

                    break;
                case "trace":
                    if (command.Name is "replay")
                    {
                        string traceFile = result.GetValueOrDefault<string>();
                        string traceFileContents = File.ReadAllText(traceFile);
                        this.Configuration.WithReproducibleTrace(traceFileContents);
                    }

                    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument '{0}'.", result.Argument.Name));
            }
        }

        /// <summary>
        /// Updates the configuration with the specified parsed option.
        /// </summary>
        private void UpdateConfigurationsWithParsedOption(OptionResult result)
        {
            if (!result.Implicit)
            {
                switch (GetNormalizedName(result.Option))
                {
                    case "method":
                        this.Configuration.TestMethodName = result.GetValueOrDefault<string>();
                        break;
                    case "iterations":
                        this.Configuration.TestingIterations = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "timeout":
                        this.Configuration.TestingTimeout = result.GetValueOrDefault<int>();
                        break;
                    case "strategy":
                        var strategyBound = result.GetResult(this.Options["strategy-value"]);
                        string strategy = result.GetValueOrDefault<string>();
                        switch (strategy)
                        {
                            case "probabilistic":
                                if (strategyBound is null)
                                {
                                    this.Configuration.StrategyBound = 3;
                                }

                                break;
                            case "prioritization":
                            case "fair-prioritization":
                            case "delay-bounding":
                            case "fair-delay-bounding":
                                if (strategyBound is null)
                                {
                                    this.Configuration.StrategyBound = 10;
                                }

                                break;
                            case "q-learning":
                                this.Configuration.IsImplicitProgramStateHashingEnabled = true;
                                break;
                            case "random":
                            default:
                                break;
                        }

                        this.Configuration.ExplorationStrategy = ExplorationStrategyExtensions.FromName(strategy);
                        this.Configuration.PortfolioMode = PortfolioMode.None;
                        break;
                    case "strategy-value":
                        this.Configuration.StrategyBound = result.GetValueOrDefault<int>();
                        break;
                    case "portfolio-mode":
                        this.Configuration.PortfolioMode = PortfolioModeExtensions.FromString(result.GetValueOrDefault<string>());
                        break;
                    case "max-steps":
                        this.Configuration.WithMaxSchedulingSteps((uint)result.GetValueOrDefault<int>());
                        break;
                    case "max-fair-steps":
                        var maxUnfairSteps = result.GetResult(this.Options["max-unfair-steps"]);
                        this.Configuration.WithMaxSchedulingSteps(
                            (uint)(maxUnfairSteps?.GetValueOrDefault<int>() ?? this.Configuration.MaxUnfairSchedulingSteps),
                            (uint)result.GetValueOrDefault<int>());
                        break;
                    case "max-unfair-steps":
                        var maxFairSteps = result.GetResult(this.Options["max-fair-steps"]);
                        this.Configuration.WithMaxSchedulingSteps(
                            (uint)result.GetValueOrDefault<int>(),
                            (uint)(maxFairSteps?.GetValueOrDefault<int>() ?? this.Configuration.MaxFairSchedulingSteps));
                        break;
                    case "fuzz":
                    case "no-repro":
                        this.Configuration.IsSystematicFuzzingEnabled = true;
                        break;
                    case "coverage":
                        this.Configuration.IsActivityCoverageReported = true;
                        break;
                    case "schedule-coverage":
                        this.Configuration.IsScheduleCoverageReported = true;
                        break;
                    case "serialize-coverage":
                        this.Configuration.IsCoverageInfoSerialized = true;
                        break;
                    case "actor-graph":
                        this.Configuration.IsActorTraceVisualizationEnabled = true;
                        break;
                    case "xml-trace":
                        this.Configuration.IsXmlLogEnabled = true;
                        break;
                    case "reduce-execution-trace-cycles":
                        this.Configuration.IsExecutionTraceCycleReductionEnabled = true;
                        break;
                    case "partial-order-sampling":
                        this.Configuration.IsPartialOrderSamplingEnabled = true;
                        break;
                    case "seed":
                        this.Configuration.RandomGeneratorSeed = result.GetValueOrDefault<uint>();
                        break;
                    case "liveness-temperature-threshold":
                        this.Configuration.LivenessTemperatureThreshold = result.GetValueOrDefault<int>();
                        this.Configuration.UserExplicitlySetLivenessTemperatureThreshold = true;
                        break;
                    case "timeout-delay":
                        this.Configuration.TimeoutDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "deadlock-timeout":
                        this.Configuration.DeadlockTimeout = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "max-fuzz-delay":
                        this.Configuration.MaxFuzzingDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "resolve-uncontrolled-concurrency-attempts":
                        this.Configuration.UncontrolledConcurrencyResolutionAttempts = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "resolve-uncontrolled-concurrency-delay":
                        this.Configuration.UncontrolledConcurrencyResolutionDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "skip-trace-analysis":
                        this.Configuration.IsTraceAnalysisEnabled = false;
                        break;
                    case "skip-potential-deadlocks":
                        this.Configuration.ReportPotentialDeadlocksAsBugs = false;
                        break;
                    case "skip-collection-races":
                        this.Configuration.IsCollectionAccessRaceCheckingEnabled = false;
                        break;
                    case "skip-lock-races":
                        this.Configuration.IsLockAccessRaceCheckingEnabled = false;
                        break;
                    case "skip-atomic-races":
                        this.Configuration.IsAtomicOperationRaceCheckingEnabled = false;
                        break;
                    case "skip-volatile-races":
                        this.Configuration.IsVolatileOperationRaceCheckingEnabled = false;
                        break;
                    case "enable-memory-access-races":
                        this.Configuration.IsMemoryAccessRaceCheckingEnabled = true;
                        break;
                    case "enable-control-flow-races":
                        this.Configuration.IsControlFlowRaceCheckingEnabled = true;
                        break;
                    case "fuzzing-fallback":
                        this.Configuration.IsSystematicFuzzingFallbackEnabled = false;
                        break;
                    case "partial-control":
                        string mode = result.GetValueOrDefault<string>();
                        switch (mode)
                        {
                            case "concurrency":
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = true;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = false;
                                break;
                            case "data":
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = false;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = true;
                                break;
                            case "none":
                            default:
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = false;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = false;
                                break;
                        }

                        break;
                    case "log-uncontrolled-invocation-stack-traces":
                        this.Configuration.WithUncontrolledInvocationStackTraceLoggingEnabled();
                        break;
                    case "fail-on-max-steps":
                        this.Configuration.FailOnMaxStepsBound = true;
                        break;
                    case "explore":
                        this.Configuration.RunTestIterationsToCompletion = true;
                        break;
                    case "break":
                        this.Configuration.AttachDebugger = true;
                        break;
                    case "outdir":
                        this.Configuration.OutputFilePath = result.GetValueOrDefault<string>();
                        break;
                    case "rewrite-memory-locations":
                        this.RewritingOptions.IsRewritingMemoryLocations = (bool)result.GetValueOrDefault<bool>();
                        break;
                    case "rewrite-concurrent-collections":
                        this.RewritingOptions.IsRewritingConcurrentCollections = (bool)result.GetValueOrDefault<bool>();
                        break;
                    case "rewrite-dependencies":
                        this.RewritingOptions.IsRewritingDependencies = true;
                        break;
                    case "rewrite-unit-tests":
                        this.RewritingOptions.IsRewritingUnitTests = true;
                        break;
                    case "assert-data-races":
                        this.RewritingOptions.IsDataRaceCheckingEnabled = true;
                        break;
                    case "dump-il":
                        this.RewritingOptions.IsLoggingAssemblyContents = true;
                        break;
                    case "dump-il-diff":
                        this.RewritingOptions.IsDiffingAssemblyContents = true;
                        break;
                    case "verbosity":
                        // A bare '-v' with no level token means 'info'; unlike the previous
                        // parser version, GetValueOrDefault falls back to the option's default
                        // value in that case, so detect it from the token count instead.
                        switch (result.Tokens.Count is 0 ? "info" : result.GetValueOrDefault<string>())
                        {
                            case "error":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Error);
                                break;
                            case "warning":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Warning);
                                break;
                            case "debug":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Debug);
                                break;
                            case "exhaustive":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Exhaustive);
                                break;
                            case "info":
                            default:
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Info);
                                break;
                        }

                        break;
                    case "console":
                        this.Configuration.WithConsoleLoggingEnabled(true);
                        break;
                    case "help":
                        break;
                    default:
                        throw new Exception(string.Format("Unhandled parsed option '{0}.", result.Option.Name));
                }
            }
        }

        /// <summary>
        /// Prints the detailed Coyote version.
        /// </summary>
        private static void PrintDetailedCoyoteVersion()
        {
            Console.WriteLine("Microsoft (R) Coyote version {0} for .NET{1}",
                typeof(CommandLineParser).Assembly.GetName().Version, GetDotNetVersion());
            Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.\n");
        }

        /// <summary>
        /// Returns the current .NET version.
        /// </summary>
        private static string GetDotNetVersion()
        {
            var path = typeof(string).Assembly.Location;
            string result = string.Empty;

            string[] parts = path.Replace("\\", "/").Split('/');
            if (parts.Length > 2)
            {
                var version = parts[parts.Length - 2];
                if (char.IsDigit(version[0]))
                {
                    result += " " + version;
                }
            }

            return result;
        }
    }
}
