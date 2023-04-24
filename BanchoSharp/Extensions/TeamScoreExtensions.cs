using BanchoSharp.Interfaces;

namespace BanchoSharp.Extensions;

public static class TeamScoreExtensions
{
	public static int ScoreSum(this IEnumerable<IMultiplayerScoreReport> teamScores) => teamScores.Sum(x => x.Score);
}