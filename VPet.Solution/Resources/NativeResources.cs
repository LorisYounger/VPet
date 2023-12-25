using System.Reflection;

namespace VPet.House.Resources;

/// <summary>
/// 本地资源
/// </summary>
internal class NativeResources
{
    #region Resources

    public const string Wall = ResourcePath + "Wall.png";
    public const string Floor = ResourcePath + "Floor.png";
    public const string Chair = ResourcePath + "Chair.png";
    public const string Table = ResourcePath + "Table.png";
    public const string Bed = ResourcePath + "Bed.png";

    public const string OakPlanks = ResourcePath + "oak_planks.png";
    public const string Stone = ResourcePath + "stone.png";

    #endregion Resources

    /// <summary>
    /// 资源基路径
    /// </summary>
    public const string ResourcePath = $"{nameof(VPet)}.{nameof(House)}.{nameof(Resources)}.";

    #region Native

    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    /// <summary>
    /// 获取资源流
    /// </summary>
    /// <param name="resourceName">资源名</param>
    /// <returns>资源流</returns>
    public static Stream GetStream(string resourceName) =>
        _assembly.GetManifestResourceStream(resourceName)!;

    /// <summary>
    /// 尝试获取资源流
    /// </summary>
    /// <param name="resourceName">资源名</param>
    /// <param name="resourceStream">资源流</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryGetStream(string resourceName, out Stream resourceStream)
    {
        resourceStream = null;
        if (_assembly.GetManifestResourceStream(resourceName) is not Stream stream)
            return false;
        resourceStream = stream;
        return true;
    }

    /// <summary>
    /// 将流保存至文件
    /// </summary>
    /// <param name="resourceName">资源名</param>
    /// <param name="path">文件路径</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool SaveTo(string resourceName, string path)
    {
        if (_assembly.GetManifestResourceStream(resourceName) is not Stream stream)
            return false;
        using var sr = new StreamReader(stream);
        using var sw = new StreamWriter(path);
        sr.BaseStream.CopyTo(sw.BaseStream);
        return true;
    }

    #endregion Native
}
