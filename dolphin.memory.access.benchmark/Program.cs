using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Dolphin.Memory.Access.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<GetDolphinAddressBenchmark>();
        }
    }

    [CoreJob()]
    public class GetDolphinAddressBenchmark
    {
        /* Dolphin should be running a game for this benchmark to yield correct results. */
        private Dolphin _dolphin;

        public GetDolphinAddressBenchmark()
        {
            try
            {
                var process = Process.GetProcessesByName("dolphin")[0];
                _dolphin = new Dolphin(process);
            }
            catch (Exception e)
            {
                throw new Exception("Dolphin is not running.");
            }
        }

        /* Go fast! */

        [Benchmark]
        public IntPtr GetDolphinBaseAddress()
        {
            _dolphin.TryGetBaseAddress(out var baseAddress);
            return baseAddress;
        }
    }
}
