using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HKW.HKWUtils;

/// <summary>
/// 拓展
/// </summary>
public static class Extensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="value"></param>
    /// <param name="comparisonType"></param>
    /// <returns></returns>
    public static bool Contains(this string source, string value, StringComparison comparisonType)
    {
        return source.IndexOf(value, comparisonType) >= 0;
    }

    //public static string GetSourceFile(this BitmapImage image)
    //{
    //    return ((FileStream)image.StreamSource).Name;
    //}

    /// <summary>
    /// 关闭流
    /// </summary>
    /// <param name="source">图像资源</param>
    public static void CloseStream(this ImageSource source)
    {
        if (source is not BitmapImage image)
            return;
        image.StreamSource?.Close();
    }

    /// <summary>
    /// 图像复制
    /// </summary>
    /// <param name="image">图像</param>
    /// <returns>复制的图像</returns>
    public static BitmapImage Copy(this BitmapImage image)
    {
        if (image is null)
            return null;
        BitmapImage newImage = new();
        newImage.BeginInit();
        newImage.DecodePixelWidth = image.DecodePixelWidth;
        newImage.DecodePixelHeight = image.DecodePixelHeight;
        try
        {
            using var bitmap = new Bitmap(image.StreamSource);
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            image.StreamSource.CopyTo(ms);
            newImage.StreamSource = ms;
        }
        finally
        {
            newImage.EndInit();
        }
        return newImage;
    }

    /// <summary>
    /// 保存至Png图片
    /// </summary>
    /// <param name="image">图片资源</param>
    /// <param name="path">路径</param>
    //public static void SaveToPng(this BitmapImage image, string path)
    //{
    //    if (image is null)
    //        return;
    //    if (path.EndsWith(".png") is false)
    //        path += ".png";
    //    var encoder = new PngBitmapEncoder();
    //    var stream = image.StreamSource;
    //    // 保存位置
    //    var position = stream.Position;
    //    // 必须要重置位置, 否则EndInit将出错
    //    stream.Seek(0, SeekOrigin.Begin);
    //    encoder.Frames.Add(BitmapFrame.Create(image.StreamSource));
    //    // 恢复位置
    //    stream.Seek(position, SeekOrigin.Begin);
    //    using var fs = new FileStream(path, FileMode.Create);
    //    encoder.Save(fs);
    //}
    public static void SaveToPng(this BitmapImage image, string path)
    {
        if (image is null)
            return;
        if (path.EndsWith(".png") is false)
            path += ".png";
        var stream = image.StreamSource;
        // 保存位置
        var position = stream.Position;
        // 必须要重置位置, 否则EndInit将出错
        stream.Seek(0, SeekOrigin.Begin);
        using var fs = new FileStream(path, FileMode.Create);
        stream.CopyTo(fs);
        // 恢复位置
        stream.Seek(position, SeekOrigin.Begin);
    }

    /// <summary>
    /// 尝试添加
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dictionary"></param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value
    )
    {
        if (dictionary.ContainsKey(key))
            return false;
        dictionary.Add(key, value);
        return true;
    }

    /// <summary>
    /// 流内容对比
    /// </summary>
    /// <param name="source">原始流</param>
    /// <param name="target">目标流</param>
    /// <param name="bufferLength">缓冲区大小 (越大速度越快(流内容越大效果越明显), 但会提高内存占用 (bufferSize = bufferLength * sizeof(long) * 2))</param>
    /// <returns>内容相同为 <see langword="true"/> 否则为 <see langword="false"/></returns>
    public static bool ContentsEqual(this Stream source, Stream target, int bufferLength = 8)
    {
        int bufferSize = bufferLength * sizeof(long);
        var sourceBuffer = new byte[bufferSize];
        var targetBuffer = new byte[bufferSize];
        while (true)
        {
            int sourceCount = ReadFullBuffer(source, sourceBuffer);
            int targetCount = ReadFullBuffer(target, targetBuffer);
            if (sourceCount != targetCount)
                return false;
            if (sourceCount == 0)
                return true;

            for (int i = 0; i < sourceCount; i += sizeof(long))
                if (BitConverter.ToInt64(sourceBuffer, i) != BitConverter.ToInt64(targetBuffer, i))
                    return false;
        }
        static int ReadFullBuffer(Stream stream, byte[] buffer)
        {
            int bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                int read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                if (read == 0)
                    return bytesRead;
                bytesRead += read;
            }
            return bytesRead;
        }
    }

    public static T? FindVisualChild<T>(this DependencyObject obj)
        where T : DependencyObject
    {
        if (obj is null)
            return null;
        var count = VisualTreeHelper.GetChildrenCount(obj);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t)
                return t;
            if (FindVisualChild<T>(child) is T childItem)
                return childItem;
        }
        return null;
    }

    public static T FindParent<T>(this DependencyObject obj)
        where T : class
    {
        while (obj != null)
        {
            if (obj is T)
                return obj as T;
            obj = VisualTreeHelper.GetParent(obj);
        }
        return null;
    }

    public static string GetFullInfo(this CultureInfo cultureInfo)
    {
        return $"{cultureInfo.DisplayName} [{cultureInfo.Name}]";
    }

    /// <summary>
    /// 尝试使用索引获取值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryGetValue<T>(this IList<T> list, int index, out T value)
    {
        value = default;
        if (index < 0 || index >= list.Count)
            return false;
        value = list[index];
        return true;
    }

    /// <summary>
    /// 尝试使用索引获取值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <returns>成功为 <see langword="true"/> 失败为 <see langword="false"/></returns>
    public static bool TryGetValue<T>(this IList list, int index, out object value)
    {
        value = default;
        if (index < 0 || index >= list.Count)
            return false;
        value = list[index];
        return true;
    }

    /// <summary>
    /// 获取目标
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="weakReference">弱引用</param>
    /// <returns>获取成功返回目标值, 获取失败则返回 <see langword="null"/></returns>
    public static T? GetTarget<T>(this WeakReference<T> weakReference)
        where T : class
    {
        return weakReference.TryGetTarget(out var t) ? t : null;
    }

    /// <summary>
    /// 枚举出带有索引值的枚举值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="collection">集合</param>
    /// <returns>带有索引的枚举值</returns>
    public static IEnumerable<ItemInfo<T>> Enumerate<T>(this IEnumerable<T> collection)
    {
        var index = 0;
        foreach (var item in collection)
            yield return new(index++, item);
    }

    /// <summary>
    /// 设置视图模型
    /// </summary>
    /// <typeparam name="T">视图模型类型</typeparam>
    /// <param name="window">窗口</param>
    public static T SetViewModel<T>(this Window window, EventHandler? closedEvent = null)
        where T : new()
    {
        if (window.DataContext is null)
        {
            window.DataContext = new T();
            window.Closed += (s, e) =>
            {
                try
                {
                    window.DataContext = null;
                }
                catch { }
            };
            window.Closed += closedEvent;
        }
        return (T)window.DataContext;
    }

    /// <summary>
    /// 设置视图模型
    /// </summary>
    /// <typeparam name="T">视图模型类型</typeparam>
    /// <param name="page">页面</param>
    public static T SetViewModel<T>(this Page page)
        where T : new()
    {
        return (T)(page.DataContext ??= new T());
    }

    private static Dictionary<Window, WindowCloseState> _windowCloseStates = new();

    /// <summary>
    /// 设置关闭状态
    /// </summary>
    /// <param name="window"></param>
    /// <param name="state">关闭状态</param>
    public static void SetCloseState(this Window window, WindowCloseState state)
    {
        window.Closing -= WindowCloseState_Closing;
        window.Closing += WindowCloseState_Closing;
        _windowCloseStates[window] = state;
    }

    /// <summary>
    /// 强制关闭
    /// </summary>
    /// <param name="window"></param>
    public static void CloseX(this Window? window)
    {
        if (window is null)
            return;
        _windowCloseStates.Remove(window);
        window.Closing -= WindowCloseState_Closing;
        window.Close();
    }

    /// <summary>
    /// 显示或者聚焦
    /// </summary>
    /// <param name="window"></param>
    public static void ShowOrActivate(this Window? window)
    {
        if (window is null)
            return;
        if (window.IsVisible is false)
            window.Show();
        window.Activate();
    }

    private static void WindowCloseState_Closing(object sender, CancelEventArgs e)
    {
        if (sender is not Window window)
            return;
        if (_windowCloseStates.TryGetValue(window, out var state) is false)
            return;
        if (state.HasFlag(WindowCloseState.SkipNext))
        {
            _windowCloseStates[window] = state &= WindowCloseState.SkipNext;
            return;
        }
        if (state is WindowCloseState.Close)
            return;
        e.Cancel = true;
        window.Visibility =
            state is WindowCloseState.Hidden ? Visibility.Hidden : Visibility.Collapsed;
        return;
    }
}

[Flags]
public enum WindowCloseState
{
    SkipNext = 0,
    Close = 1 << 0,
    Hidden = 1 << 1,
    Collapsed = 1 << 2
}

/// <summary>
/// 项信息
/// </summary>
/// <typeparam name="T"></typeparam>
[DebuggerDisplay("[{Index}, {Value}]")]
public readonly struct ItemInfo<T>
{
    /// <summary>
    /// 索引值
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 值
    /// </summary>
    public T Value { get; }

    /// <inheritdoc/>
    /// <param name="value">值</param>
    /// <param name="index">索引值</param>
    public ItemInfo(int index, T value)
    {
        Index = index;
        Value = value;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[{Index}, {Value}]";
    }
}
