using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace GZipTest.Tests.Utils
{
    public class Measurement
    {
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();

        private readonly string _name;
        private readonly int _blocks;
        private readonly TimeSpan _elapsed;
        private readonly TimeSpan _kernelTime;
        private readonly TimeSpan _userTime;
        private readonly MemoryStatistics _gcDelta;

        private Measurement(string name, int blocks, TimeSpan elapsed, TimeSpan kernelTime, TimeSpan userTime, MemoryStatistics gcDelta)
        {
            _name = name;
            _blocks = blocks;
            _elapsed = elapsed;
            _kernelTime = kernelTime;
            _userTime = userTime;
            _gcDelta = gcDelta;
        }

        [NotNull]
        public static Measurement Measure(string name, [NotNull] Action action, int blocks)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var cpuUserTimeBefore = CurrentProcess.UserProcessorTime;
            var cpuKernelTimeBefore = CurrentProcess.PrivilegedProcessorTime;
            var memoryBefore = MemoryStatistics.TakeCurrent();
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();
            var memoryAfter = MemoryStatistics.TakeCurrent();
            var cpuUserTimeAfter = CurrentProcess.UserProcessorTime;
            var cpuKernelTimeAfter = CurrentProcess.PrivilegedProcessorTime;

            var gcDelta = MemoryStatistics.Delta(memoryBefore, memoryAfter);

            return new Measurement(
                name,
                blocks,
                stopwatch.Elapsed,
                (cpuKernelTimeAfter - cpuKernelTimeBefore),
                (cpuUserTimeAfter - cpuUserTimeBefore),
                gcDelta);
        }

        public static void Display([NotNull] ITestOutputHelper output, [NotNull] IReadOnlyList<Measurement> measurements)
        {
            output.WriteLine($"Metrics count: {measurements.Count}");
            output.WriteLine("Rate: {0}", CalcStatistics(measurements, measurement => (double)measurement._blocks / measurement._elapsed.TotalSeconds));
            output.WriteLine("Elapsed: {0}", CalcTimeStatistics(measurements, measurement => measurement._elapsed.TotalMilliseconds));
            output.WriteLine("CPU User Time: {0}",  CalcTimeStatistics(measurements, measurement => measurement._userTime.TotalMilliseconds));
            output.WriteLine("CPU Kernel Time: {0}",  CalcTimeStatistics(measurements, measurement => measurement._kernelTime.TotalMilliseconds));
            output.WriteLine("GC Delta Gen0: {0}", CalcStatistics(measurements, measurement => measurement._gcDelta.Gen0));
            output.WriteLine("GC Delta Gen1: {0}", CalcStatistics(measurements, measurement => measurement._gcDelta.Gen1));
            output.WriteLine("GC Delta Gen2: {0}", CalcStatistics(measurements, measurement => measurement._gcDelta.Gen2));
        }

        public void Display([NotNull] ITestOutputHelper output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.WriteLine($"{_name} statistics:");
            output.WriteLine($"Blocks:          {_blocks}");
            output.WriteLine( "Rate:            {0:0.000} items/sec", (double)_blocks / _elapsed.TotalSeconds);
            output.WriteLine($"Elapsed:         {_elapsed}");
            output.WriteLine($"CPU User Time:   {_userTime}");
            output.WriteLine($"CPU Kernel Time: {_kernelTime}");
            output.WriteLine($"GC Delta:        {_gcDelta.Gen0}/{_gcDelta.Gen1}/{_gcDelta.Gen2}");
            output.WriteLine(string.Empty);
        }

        [NotNull]
        private static string CalcStatistics(
            [NotNull] IReadOnlyList<Measurement> measurements,
            [NotNull] Func<Measurement, double> selector)
        {
            var min = measurements.Min(selector);
            var max = measurements.Max(selector);
            var avg = measurements.Average(selector);

            return $"{min:0.000}/{max:0.000}/{avg:0.000}";
        }


        [NotNull]
        private static string CalcTimeStatistics(
            [NotNull] IReadOnlyList<Measurement> measurements,
            [NotNull] Func<Measurement, double> selector)
        {
            var min = TimeSpan.FromMilliseconds(measurements.Min(selector));
            var max = TimeSpan.FromMilliseconds(measurements.Max(selector));
            var avg = TimeSpan.FromMilliseconds(measurements.Average(selector));

            return $"{min}/{max}/{avg}";
        }

        private sealed class MemoryStatistics
        {
            public readonly int Gen0;
            public readonly int Gen1;
            public readonly int Gen2;

            private MemoryStatistics(int gen0, int gen1, int gen2)
            {
                Gen0 = gen0;
                Gen1 = gen1;
                Gen2 = gen2;
            }

            [NotNull]
            public static MemoryStatistics TakeCurrent()
            {
                return new MemoryStatistics(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
            }

            [NotNull]
            public static MemoryStatistics Delta([NotNull] MemoryStatistics before, [NotNull] MemoryStatistics after)
            {
                if (before == null) throw new ArgumentNullException(nameof(before));
                if (after == null) throw new ArgumentNullException(nameof(after));

                return new MemoryStatistics(after.Gen0 - before.Gen0, after.Gen1 - before.Gen1, after.Gen2 - before.Gen2);
            }
        }
    }
}