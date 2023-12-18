namespace HKW.HKWUtils;

/// <summary>
/// 哈希值
/// </summary>
public class HashCode
{
    /// <summary>
    /// 默认种子
    /// </summary>
    public const int DefaultSeed = 114514;

    /// <summary>
    /// 默认系数
    /// </summary>
    public const int DefaultFactor = 1919810;

    /// <summary>
    /// 组合哈希值
    /// </summary>
    /// <param name="values">值</param>
    /// <returns>组合的哈希值</returns>
    public static int Combine(params object[] values)
    {
        return CustomHash(DefaultSeed, DefaultFactor, values.Select(v => v.GetHashCode()));
    }

    /// <summary>
    /// 组合哈希值
    /// </summary>
    /// <param name="seed">种子</param>
    /// <param name="factor">系数</param>
    /// <param name="values">值</param>
    /// <returns>组合的哈希值</returns>
    public static int Combine(int seed, int factor, params object[] values)
    {
        return CustomHash(seed, factor, values.Select(v => v.GetHashCode()));
    }

    /// <summary>
    /// 自定义组合哈希
    /// </summary>
    /// <param name="seed">种子</param>
    /// <param name="factor">系数</param>
    /// <param name="collection">哈希集合</param>
    /// <returns>组合的哈希</returns>
    public static int CustomHash(int seed, int factor, IEnumerable<int> collection)
    {
        int hash = seed;
        foreach (int i in collection)
            hash = unchecked((hash * factor) + i);
        return hash;
    }

    /// <summary>
    /// 自定义组合哈希
    /// </summary>
    /// <param name="seed">种子</param>
    /// <param name="factor">系数</param>
    /// <param name="values">哈希集合</param>
    /// <returns>组合的哈希</returns>
    public static int CustomHash(int seed, int factor, params int[] values)
    {
        return CustomHash(seed, factor, collection: values);
    }
}
