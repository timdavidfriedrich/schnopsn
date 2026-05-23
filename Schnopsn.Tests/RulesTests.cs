using Schnopsn.components.card;
using Schnopsn.core.Utilities;
using Xunit;

namespace Schnopsn.Tests;

public class RulesTests
{
    // --- Points ---

    [Fact] public void Points_Sau_Returns11() => Assert.Equal(11, Rules.Points(CardValue.sau));
    [Fact] public void Points_Zehn_Returns10() => Assert.Equal(10, Rules.Points(CardValue.zehn));
    [Fact] public void Points_Koenig_Returns4() => Assert.Equal(4, Rules.Points(CardValue.koenig));
    [Fact] public void Points_Ober_Returns3() => Assert.Equal(3, Rules.Points(CardValue.ober));
    [Fact] public void Points_Unter_Returns2() => Assert.Equal(2, Rules.Points(CardValue.unter));

    // --- Rank ordering ---

    [Fact] public void Rank_SauBeatsZehn() => Assert.True(Rules.Rank(CardValue.sau) > Rules.Rank(CardValue.zehn));
    [Fact] public void Rank_ZehnBeatsKoenig() => Assert.True(Rules.Rank(CardValue.zehn) > Rules.Rank(CardValue.koenig));
    [Fact] public void Rank_KoenigBeatsOber() => Assert.True(Rules.Rank(CardValue.koenig) > Rules.Rank(CardValue.ober));
    [Fact] public void Rank_OberBeatsUnter() => Assert.True(Rules.Rank(CardValue.ober) > Rules.Rank(CardValue.unter));

    // --- DetermineWinner ---

    [Fact]
    public void DetermineWinner_SameSuit_HigherRankSecondWins()
    {
        var first  = new CardData(CardColor.herz, CardValue.zehn);
        var second = new CardData(CardColor.herz, CardValue.sau);
        var winner = Rules.DetermineWinner(first, second, CardColor.schellen);
        Assert.Equal(second, winner);
    }

    [Fact]
    public void DetermineWinner_SameSuit_LowerRankFirstWins()
    {
        var first  = new CardData(CardColor.herz, CardValue.sau);
        var second = new CardData(CardColor.herz, CardValue.unter);
        var winner = Rules.DetermineWinner(first, second, CardColor.schellen);
        Assert.Equal(first, winner);
    }

    [Fact]
    public void DetermineWinner_OffSuitNonTrump_FirstWins()
    {
        var first  = new CardData(CardColor.herz, CardValue.sau);
        var second = new CardData(CardColor.pik, CardValue.sau);
        var winner = Rules.DetermineWinner(first, second, CardColor.herz);
        Assert.Equal(first, winner);
    }

    [Fact]
    public void DetermineWinner_SecondPlaysTrump_SecondWins()
    {
        var first  = new CardData(CardColor.herz, CardValue.sau);
        var second = new CardData(CardColor.schellen, CardValue.unter);
        var winner = Rules.DetermineWinner(first, second, CardColor.schellen);
        Assert.Equal(second, winner);
    }

    [Fact]
    public void DetermineWinner_BothTrump_HigherRankWins()
    {
        var first  = new CardData(CardColor.schellen, CardValue.unter);
        var second = new CardData(CardColor.schellen, CardValue.sau);
        var winner = Rules.DetermineWinner(first, second, CardColor.schellen);
        Assert.Equal(second, winner);
    }
}
