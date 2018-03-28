﻿using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using InlineIL;

namespace ZeroLog.Benchmarks.EnumTests
{
    public static class EnumBenchmarksRunner
    {
        public static void Run()
        {
            Validate();
            BenchmarkRunner.Run<EnumBenchmarks>();
        }

        private static void Validate()
        {
            var benchmarks = new EnumBenchmarks();
            var expected = benchmarks.Typeof();

            if (benchmarks.TypeofCached() != expected)
                throw new InvalidOperationException();

            if (benchmarks.TypedRef() != expected)
                throw new InvalidOperationException();

            if (benchmarks.TypeHandleIl() != expected)
                throw new InvalidOperationException();
        }
    }

    [MemoryDiagnoser]
    [ClrJob, CoreJob]
    public unsafe class EnumBenchmarks
    {
        [Benchmark(Baseline = true)]
        public IntPtr Typeof() => TypeofImpl<SomeEnum>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IntPtr TypeofImpl<T>()
            where T : struct
        {
            return typeof(T).TypeHandle.Value;
        }

        [Benchmark]
        public IntPtr TypeofCached() => TypeofCachedImpl<SomeEnum>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IntPtr TypeofCachedImpl<T>()
            where T : struct
        {
            return Cache<T>.TypeHandle;
        }

        private struct Cache<T>
            where T : struct
        {
            public static readonly IntPtr TypeHandle = typeof(T).TypeHandle.Value;
        }

        [Benchmark]
        public IntPtr TypedRef() => TypedRefImpl<SomeEnum>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IntPtr TypedRefImpl<T>()
            where T : struct
        {
            var value = default(T);
            var typedRef = __makeref(value);
            return ((IntPtr*)&typedRef)[1];
        }

        [Benchmark]
        public IntPtr TypeHandleIl() => TypeHandleIlImpl<SomeEnum>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IntPtr TypeHandleIlImpl<T>()
            where T : struct
        {
            IL.DeclareLocals(
                false,
                new LocalVar(typeof(RuntimeTypeHandle))
            );

            IL.Push(typeof(T));
            IL.Emit(OpCodes.Stloc_0);
            IL.Emit(OpCodes.Ldloca_S, 0);
            IL.Emit(OpCodes.Call, new MethodRef(typeof(RuntimeTypeHandle), "get_" + nameof(RuntimeTypeHandle.Value)));
            return IL.Return<IntPtr>();
        }
    }

    public enum SomeEnum
    {
        Foo,
        Bar,
        Baz
    }
}
