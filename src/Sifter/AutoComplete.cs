// <copyright file="AutoComplete.cs" company="Berkeleybross">
// Copyright (c) Berkeleybross. All rights reserved.
// </copyright>
namespace Sifter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Scores possible matches to find the best possible suggestion.
    /// </summary>
    /// <typeparam name="T">The type to autocomplete for.</typeparam>
    public class AutoComplete<T>
    {
        private readonly IReadOnlyCollection<SuggestionBuilder> values;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoComplete{T}"/> class.
        /// </summary>
        /// <param name="values">The set of possible values.</param>
        /// <param name="valueFactory">The selector to generate a string representation to search.</param>
        public AutoComplete(IEnumerable<T> values, Func<T, string> valueFactory)
        {
            this.values = values.Select(v => new SuggestionBuilder(v, valueFactory(v))).ToList();
        }

        /// <summary>
        /// Ranks the possible suggestions and returns the top <paramref name="limit"/> matches.
        /// </summary>
        /// <param name="term">The term to suggest possible terms for.</param>
        /// <param name="limit">The number of suggestions to retrieve.</param>
        /// <returns>The top matches.</returns>
        public IEnumerable<T> GetSuggestions(string term, int limit)
        {
            return this.Score(term)
                .Where(t => t.Score > 0)
                .OrderByDescending(t => t.Score)
                .Take(limit)
                .Select(t => t.Value);
        }

        /// <summary>
        /// Scores the possible suggestions and returns all of them, without sorting or filtering.
        /// </summary>
        /// <param name="term">The term to suggest possible terms for.</param>
        /// <returns>All suggestions. A score of zero indicates no match.</returns>
        public IEnumerable<AutoCompleteSuggestion<T>> Score(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return this.values.Select(v => new AutoCompleteSuggestion<T>(v.Value, 1.0d));
            }

            Func<SuggestionBuilder, double> calculateScore;

            var tokens = term.Split();
            if (tokens.Length == 1)
            {
                calculateScore = v => v.CalculateScore(term);
            }
            else
            {
                calculateScore = v => v.CalculateDisjunctionScore(tokens);
            }

            return this.values.Select(v => new AutoCompleteSuggestion<T>(v.Value, calculateScore(v)));
        }

        private class SuggestionBuilder
        {
            private readonly string normalizedValue;

            public SuggestionBuilder(T value, string normalizedValue)
            {
                this.normalizedValue = normalizedValue;
                this.Value = value;
            }

            public T Value { get; }

            public double CalculateDisjunctionScore(IEnumerable<string> tokens)
            {
                var totalScore = 0d;
                foreach (var token in tokens)
                {
                    var tokenScore = this.CalculateScore(token);
                    if (tokenScore <= 0)
                    {
                        return 0;
                    }

                    totalScore += tokenScore;
                }

                return totalScore;
            }

            public double CalculateScore(string term)
            {
                if (string.IsNullOrEmpty(this.normalizedValue))
                {
                    return 0;
                }

                var position = CultureInfo.InvariantCulture.CompareInfo.IndexOf(this.normalizedValue, term, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
                if (position < 0)
                {
                    return 0;
                }

                double boost = 0;
                if (position == 0)
                {
                    boost = 0.25;
                }
                else if (char.IsWhiteSpace(this.normalizedValue, position - 1))
                {
                    boost = 0.25;
                }

                return (term.Length / (double)this.normalizedValue.Length) + boost;
            }
        }
    }
}