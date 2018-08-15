// <copyright file="AutoCompleteSuggestion.cs" company="Berkeleybross">
// Copyright (c) Berkeleybross. All rights reserved.
// </copyright>
namespace Sifter
{
    /// <summary>
    /// Holds a suggested value to autocomplete a search, with it's score.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    public class AutoCompleteSuggestion<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCompleteSuggestion{T}"/> class.
        /// </summary>
        /// <param name="value">The value to suggest.</param>
        /// <param name="score">The relative score of the value.</param>
        public AutoCompleteSuggestion(T value, double score)
        {
            this.Value = value;
            this.Score = score;
        }

        /// <summary>
        /// Gets the value to suggest.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the relative score of the value.
        /// </summary>
        public double Score { get; }
    }
}