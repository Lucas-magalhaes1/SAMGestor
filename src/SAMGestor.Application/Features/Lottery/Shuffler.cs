using System.Security.Cryptography;

namespace SAMGestor.Application.Features.Lottery;

internal static class Shuffler
{
    public static void ShuffleInPlace<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}