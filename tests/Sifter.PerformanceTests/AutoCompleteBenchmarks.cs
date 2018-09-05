// <copyright file="AutoCompleteBenchmarks.cs" company="Berkeleybross">
// Copyright (c) Berkeleybross. All rights reserved.
// </copyright>
namespace Sifter.PerformanceTests
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters.Json;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Mathematics;
    using BenchmarkDotNet.Order;

    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [Config(typeof(Config))]
    public class AutoCompleteBenchmarks
    {
        private AutoComplete<string> sut;

        [GlobalSetup]
        public void Setup()
        {
            string text;
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Sifter.PerformanceTests.corpus.csv"))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }

            this.sut = new AutoComplete<string>(text.Split(), v => v);
        }

        [Benchmark]
        public void NoQuery() => this.sut.Score(null).ToList();

        [Benchmark]
        [Arguments("A")]
        [Arguments("ru")]
        [Arguments("mia")]
        [Arguments("famous somewhat")]
        public void ScoreOneField(string term) => this.sut.Score(term).ToList();

        [Benchmark]
        [Arguments("A")]
        [Arguments("ru")]
        [Arguments("mia")]
        [Arguments("famous somewhat")]
        public void GetSuggestionsOneField(string term) => this.sut.GetSuggestions(term, 5).ToList();

        private class Config
            : ManualConfig
        {
            public Config()
            {
                this.Add(JsonExporter.Full);
                this.Add(new MemoryDiagnoser());
                this.Add(Job.Default
                    .WithUnrollFactor(50)
                    .WithLaunchCount(1)
                    .WithWarmupCount(0)
                    .WithOutlierMode(OutlierMode.OnlyLower));
            }
        }
    }
}
