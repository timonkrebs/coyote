// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Tests.Common;
using Xunit;

namespace Microsoft.Coyote.Runtime.Tests.Logging
{
    public class MemoryLogWriterTests : BaseRuntimeTest
    {
        public MemoryLogWriterTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private (string, string) WriteAllSeverityMessages(VerbosityLevel level)
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(level);
            using var logger = new MemoryLogger(config.VerbosityLevel);
            using var logWriter = new MemoryLogWriter(config);
            logWriter.SetLogger(logger);
            Assert.IsType<MemoryLogger>(logWriter.Logger);

            logWriter.LogDebug(VerbosityMessages.DebugMessage);
            logWriter.LogInfo(VerbosityMessages.InfoMessage);
            logWriter.LogWarning(VerbosityMessages.WarningMessage);
            logWriter.LogError(VerbosityMessages.ErrorMessage);
            logWriter.LogImportant(VerbosityMessages.ImportantMessage);

            string logged = logger.ToString().NormalizeNewLines();
            string observed = logWriter.GetObservedMessages().NormalizeNewLines();
            this.TestOutput.WriteLine($"Logged (length: {logged.Length}):");
            this.TestOutput.WriteLine(logged);
            this.TestOutput.WriteLine($"Observed (length: {observed.Length}):");
            this.TestOutput.WriteLine(observed);
            return (logged, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterNoneVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.None);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.ImportantMessage), logged);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterErrorVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.Error);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), logged);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterWarningVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.Warning);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), logged);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterInfoVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.Info);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, logged);
            Assert.Equal(expected, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterDebugVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.Debug);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.DebugMessage,
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, logged);
            Assert.Equal(expected, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterExhaustiveVerbosity()
        {
            (string logged, string observed) = this.WriteAllSeverityMessages(VerbosityLevel.Exhaustive);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.DebugMessage,
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, logged);
            Assert.Equal(expected, observed);
        }

        private (string, string) WriteAllSeverityMessages(MemoryLogWriter logWriter)
        {
            using var stream = new MemoryStream();
            using (var interceptor = new ConsoleOutputInterceptor(stream))
            {
                logWriter.LogDebug(VerbosityMessages.DebugMessage);
                logWriter.LogInfo(VerbosityMessages.InfoMessage);
                logWriter.LogWarning(VerbosityMessages.WarningMessage);
                logWriter.LogError(VerbosityMessages.ErrorMessage);
                logWriter.LogImportant(VerbosityMessages.ImportantMessage);
            }

            string logged = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines();
            string observed = logWriter.GetObservedMessages().NormalizeNewLines();
            this.TestOutput.WriteLine($"Logged (length: {logged.Length}):");
            this.TestOutput.WriteLine(logged);
            this.TestOutput.WriteLine($"Observed (length: {observed.Length}):");
            this.TestOutput.WriteLine(observed);
            return (logged, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterConsoleOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled().WithConsoleLoggingEnabled();
            using var logWriter = new MemoryLogWriter(config);
            Assert.IsType<ConsoleLogger>(logWriter.Logger);
            (string logged, string observed) = this.WriteAllSeverityMessages(logWriter);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, logged);
            Assert.Equal(expected, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterForceConsoleOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled().WithConsoleLoggingEnabled(false);
            using var logWriter = new MemoryLogWriter(config, true);
            Assert.IsType<ConsoleLogger>(logWriter.Logger);
            (string logged, string observed) = this.WriteAllSeverityMessages(logWriter);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, logged);
            Assert.Equal(expected, observed);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLogWriterNullOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled();
            using var logWriter = new MemoryLogWriter(config);
            Assert.IsType<NullLogger>(logWriter.Logger);
            (string logged, string observed) = this.WriteAllSeverityMessages(logWriter);
            Assert.Equal(string.Empty, logged);
            Assert.Equal(StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage), observed);
        }
    }
}
