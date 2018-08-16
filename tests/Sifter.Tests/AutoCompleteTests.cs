// <copyright file="AutoCompleteTests.cs" company="Berkeleybross">
// Copyright (c) Berkeleybross. All rights reserved.
// </copyright>
namespace Sifter.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Xunit;

    public class AutoCompleteTests
    {
        public class GetSuggestions
            : AutoCompleteTests
        {
            [Fact]
            public void Returns_empty_set_when_there_are_no_suggestions()
            {
                // Arrange
                var sut = MakeSut(Enumerable.Empty<string>());

                // Act
                var result = sut.GetSuggestions("term", 5);

                // Assert
                result.Should().BeEmpty();
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("\r\n")]
            public void Returns_all_values_when_term_is_empty(string term)
            {
                // Arrange
                var sut = MakeSut(new[] { "foo", "bar", null, string.Empty });

                // Act
                var result = sut.GetSuggestions(term, 5);

                // Assert
                result.Should().BeEquivalentTo("foo", "bar", null, string.Empty);
            }

            [Fact]
            public void Returns_empty_set_when_term_matches_nothing()
            {
                // Arrange
                var sut = MakeSut(new[] { "foo", "bar", null, string.Empty });

                // Act
                var result = sut.GetSuggestions("term", 5);

                // Assert
                result.Should().BeEmpty();
            }

            [Fact]
            public void Orders_values_by_relevance()
            {
                // Arrange
                var sut = MakeSut(new[] { "infoof", "infoofoofoof" });

                // Act
                var result = sut.GetSuggestions("foo", 5);

                // Assert
                result.Should().BeEquivalentTo(new[] { "infoof", "infoofoofoof" }, o => o.WithStrictOrdering());
            }

            [Fact]
            public void Limits_matches()
            {
                // Arrange
                var sut = MakeSut(new[] { "foo", "foo", "foo", "foo", "foo", "foo" });

                // Act
                var result = sut.GetSuggestions("foo", 5);

                // Assert
                result.Should().BeEquivalentTo("foo", "foo", "foo", "foo", "foo");
            }
        }

        public class Score
            : AutoCompleteTests
        {
            [Fact]
            public void Returns_empty_set_when_there_are_no_suggestions()
            {
                // Arrange
                var sut = MakeSut(Enumerable.Empty<string>());

                // Act
                var result = sut.Score("term");

                // Assert
                result.Should().BeEmpty();
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("\r\n")]
            public void Scores_all_values_equally_when_there_is_no_term(string term)
            {
                // Arrange
                var sut = MakeSut(new[] { "foo", "bar", null, string.Empty });

                // Act
                var result = sut.Score(term);

                // Assert
                result.Should().BeEquivalentTo(
                    Suggestion("foo", 1.0d),
                    Suggestion("bar", 1.0d),
                    Suggestion(null, 1.0d),
                    Suggestion(string.Empty, 1.0d));
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("foo")]
            public void Scores_value_as_zero_when_it_doesnt_match_term(string value)
            {
                // Arrange
                var sut = MakeSut(new[] { value });

                // Act
                var result = sut.Score("term");

                // Assert
                result.Should().BeEquivalentTo(Suggestion(value, 0));
            }

            [Theory]
            [InlineData("foo")]
            [InlineData("FOO")]
            [InlineData("FoO")]
            public void Returns_exact_match(string term)
            {
                // Arrange
                var sut = MakeSut(new[] { "foo" });

                // Act
                var result = sut.Score(term);

                // Assert
                result.Should().BeEquivalentTo(Suggestion("foo", 1.25));
            }

            [Fact]
            public void Returns_values_which_contain_term()
            {
                // Arrange
                var sut = MakeSut(new[]
                {
                    "foo",
                    "foobar",
                    "infoof",
                    "barfoo",
                    "INFOOF",
                    "infoOf"
                });

                // Act
                var result = sut.Score("foo");

                // Assert
                result.Should().BeEquivalentTo(
                    Suggestion("foo", 1.25),
                    Suggestion("foobar", 0.75),
                    Suggestion("infoof", 0.5),
                    Suggestion("barfoo", 0.5),
                    Suggestion("INFOOF", 0.5),
                    Suggestion("infoOf", 0.5));
            }

            [Fact]
            public void Boosts_relevance_for_matches_at_start_of_string()
            {
                // Arrange
                var sut = MakeSut(new[] { "foob", "bfoo" });

                // Act
                var result = sut.Score("foo").ToList();

                // Assert
                result.Should().BeEquivalentTo(Suggestion("foob", 1), Suggestion("bfoo", 0.75));
            }

            [Fact]
            public void Boosts_relevance_for_matches_at_start_of_word()
            {
                // Arrange
                var sut = MakeSut(new[] { "bar foob", "bar bfoo" });

                // Act
                var result = sut.Score("foo").ToList();

                // Assert
                result.Should().BeEquivalentTo(Suggestion("bar foob", 0.625), Suggestion("bar bfoo", 0.375));
            }

            [Fact]
            public void Only_matches_all_words_in_term()
            {
                // Arrange
                var sut = MakeSut(new[] { "foo", "bar", "foo bar", "bar foo", "foo bar baz" });

                // Act
                var result = sut.Score("foo bar").ToList();

                // Assert
                result.Should().BeEquivalentTo(
                    new[]
                    {
                        Suggestion("foo", 0),
                        Suggestion("bar", 0),
                        Suggestion("foo bar", 1.357),
                        Suggestion("bar foo", 1.357),
                        Suggestion("foo bar baz", 1.045)
                    },
                    o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.001))
                        .WhenTypeIs<double>());
            }

            [Fact]
            public void Matches_words_in_any_order()
            {
                // Arrange
                var sut = MakeSut(new[] { "foo bar", "bar foo" });

                // Act
                var result = sut.Score("foo bar").ToList();

                // Assert
                result.Should().BeEquivalentTo(
                    new[]
                    {
                        Suggestion("foo bar", 1.357),
                        Suggestion("bar foo", 1.357)
                    },
                    o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.001))
                        .WhenTypeIs<double>());
            }

            [Theory]
            [InlineData("foo  bar")]
            [InlineData(" foo bar")]
            [InlineData("foo bar ")]
            [InlineData("foo\r\nbar")]
            public void Ignores_extra_whitespace_in_term(string term)
            {
                // Arrange
                var sut = MakeSut(new[] { "foo bar" });

                // Act
                var result = sut.Score(term).ToList();

                // Assert
                result.Should().BeEquivalentTo(
                    new[]
                    {
                        Suggestion("foo bar", 1.607)
                    },
                    o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.001))
                        .WhenTypeIs<double>());
            }

            public static IEnumerable<object[]> DiacriticsSource()
            {
                // More advanced unicode source can be found at https://github.com/apache/lucenenet/blob/872d6e3e4c3543ba45466a64ea879c0d170ee03b/src/Lucene.Net.Analysis.Common/Analysis/Miscellaneous/ASCIIFoldingFilter.cs
                var characters = new Dictionary<char, string>
                {
                    ['a'] = "ḀḁĂăÂâǍǎȺⱥȦȧẠạÄäÀàÁáĀāÃãÅåąĄÃąĄ",
                    ['c'] = "ĆćĈĉČčĊċC̄c̄ÇçḈḉȻȼƇƈɕ",
                    ['d'] = "ĎďḊḋḐḑḌḍḒḓḎḏĐđD̦d̦ƊɗƋƌᵭᶁᶑð",
                    ['e'] = "ÉéÈèÊêḘḙĚěĔĕẼẽḚḛẺẻĖėËëĒēȨȩĘęᶒɆɇȄȅẾếỀềỄễỂểḜḝḖḗḔḕȆȇẸẹỆệⱸɘǝƏ",
                    ['f'] = "ƑƒḞḟ",
                    ['g'] = "ǤǥĜĝĞğĢģƓɠĠġ",
                    ['h'] = "ĤĥĦħḨḩẖẖḤḥḢḣɦʰ",
                    ['i'] = "ÍíÌìĬĭÎîǏǐÏïḮḯĨĩĮįĪīỈỉȈȉȊȋỊịḬḭƗɨɨ̆ᶖİiIı",
                    ['j'] = "ȷĴĵɈɉʝɟʲ",
                    ['k'] = "ƘƙꝀꝁḰḱǨǩḲḳḴḵ",
                    ['l'] = "ŁłĽľĻļĹĺḶḷḸḹḼḽḺḻĿŀȽƚⱠⱡⱢɫɬᶅɭ",
                    ['n'] = "ŃńǸǹŇňÑñṄṅŅņṆṇṊṋṈṉN̈n̈ƝɲȠƞᵰᶇɳ",
                    ['o'] = "ØøÖöÓóÒòÔôǑǒŐőŎŏȮȯỌọƟɵƠơỎỏŌōÕõǪǫȌȍ",
                    ['p'] = "ṔṕṖṗⱣᵽƤƥᵱ",
                    ['q'] = "ꝖꝗʠɊɋꝘꝙq̃",
                    ['r'] = "ŔŕɌɍŘřŖŗṘṙȐȑȒȓṚṛⱤɽ",
                    ['s'] = "ŚśṠṡṢṣŜŝŠšŞşȘșS̈s̈",
                    ['t'] = "ŤťṪṫŢţṬṭƮʈȚțṰṱṮṯƬƭ",
                    ['u'] = "ŬŭɄʉỤụÜüÚúÙùÛûǓǔŰűŬŭƯưỦủŪūŨũŲųȔȕ",
                    ['v'] = "ṼṽṾṿƲʋꝞꝟⱱʋ",
                    ['w'] = "ẂẃẀẁŴŵẄẅẆẇẈẉ",
                    ['x'] = "ẌẍẊẋ",
                    ['y'] = "ÝýỲỳŶŷŸÿỸỹẎẏỴỵɎɏƳƴ",
                    ['z'] = "ŹźẐẑŽžŻżẒẓẔẕƵƶ"
                };

                return characters.SelectMany(c => GetCharacters(c.Value), (kvp, c) => new object[] { kvp.Key, c.ToString() });
            }

            [Theory]
            [InlineData("díåcritîçs", "diacritics")]
            [InlineData("montana", "montaña")]
            [MemberData(nameof(DiacriticsSource))]
            public void Ignores_diacritics(string value, string term)
            {
                // Arrange
                var sut = MakeSut(new[] { value });

                // Act
                var result = sut.Score(term).ToList();

                // Assert
                result.Should().BeEquivalentTo(
                    new[]
                    {
                        Suggestion(value, 1.25)
                    },
                    o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.001))
                        .WhenTypeIs<double>());
            }

            private static AutoCompleteSuggestion<string> Suggestion(string value, double score)
            {
                return new AutoCompleteSuggestion<string>(value, score);
            }

            private static List<string> GetCharacters(string text)
            {
                var ca = text.ToCharArray();
                var characters = new List<string>();
                for (var i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    if (c > 65535)
                    {
                        continue;
                    }

                    if (char.IsHighSurrogate(c))
                    {
                        i++;
                        characters.Add(new string(new[] { c, ca[i] }));
                    }
                    else
                    {
                        characters.Add(new string(new[] { c }));
                    }
                }

                return characters;
            }
        }

        private static AutoComplete<string> MakeSut(IEnumerable<string> values)
        {
            return new AutoComplete<string>(values, v => v);
        }
    }
}