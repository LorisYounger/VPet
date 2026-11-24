using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// 单个显示器的逻辑坐标快照
    /// </summary>
    public readonly struct ScreenInfo
    {
        public string DeviceName { get; }
        public Rect LogicalBounds { get; }
        public bool IsPrimary { get; }

        public ScreenInfo(string deviceName, Rect logicalBounds, bool isPrimary)
        {
            DeviceName = deviceName ?? string.Empty;
            LogicalBounds = logicalBounds;
            IsPrimary = isPrimary;
        }
    }

    /// <summary>
    /// 多屏幕拓扑计算与边界校验工具（统一管理屏幕快照、热插拔刷新、多屏并集可达边界与防虚空兜底）
    /// </summary>
    public static class ScreenTopology
    {
        /// <summary>
        /// 相邻屏幕拼接容差（处理 DPI 缩放取整带来的 1~2px 缝隙）
        /// </summary>
        public const double AdjacentTolerance = 2.0;

        /// <summary>
        /// 判定区域有效的最小可见尺寸（逻辑像素）
        /// </summary>
        public const double MinValidSize = 50.0;

        private static readonly object _cacheLock = new object();
        private static IReadOnlyList<ScreenInfo>? _cachedScreens;
        private static double _cachedDpiX = 1.0;
        private static double _cachedDpiY = 1.0;
        private static bool _displayEventSubscribed;

        /// <summary>
        /// 当系统显示设置变更（分辨率、缩放、旋转、拔插副屏）时触发
        /// </summary>
        public static event Action? DisplayChanged;

        static ScreenTopology()
        {
            EnsureDisplayEventSubscribed();
        }

        private static void EnsureDisplayEventSubscribed()
        {
            if (_displayEventSubscribed)
                return;
            try
            {
                SystemEvents.DisplaySettingsChanged += OnSystemDisplaySettingsChanged;
                _displayEventSubscribed = true;
            }
            catch
            {
                // 某些受限环境下忽略
            }
        }

        /// <summary>
        /// 应用退出时解除静态系统事件订阅
        /// </summary>
        public static void Shutdown()
        {
            if (!_displayEventSubscribed)
                return;
            try
            {
                SystemEvents.DisplaySettingsChanged -= OnSystemDisplaySettingsChanged;
            }
            catch { }
            _displayEventSubscribed = false;
        }

        private static void OnSystemDisplaySettingsChanged(object? sender, EventArgs e)
        {
            InvalidateCache();
            DisplayChanged?.Invoke();
        }

        /// <summary>
        /// 清空屏幕缓存，下次读取时重新查询系统显示器
        /// </summary>
        public static void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedScreens = null;
            }
        }

        /// <summary>
        /// 获取当前系统级 DPI 缩放比例（将 Screen.Bounds 转为 WPF 逻辑坐标）
        /// </summary>
        public static (double dpiX, double dpiY) GetSystemDpiScale(Visual? referenceVisual = null)
        {
            if (referenceVisual != null)
            {
                try
                {
                    var source = PresentationSource.FromVisual(referenceVisual);
                    if (source?.CompositionTarget != null)
                    {
                        var m = source.CompositionTarget.TransformToDevice;
                        if (m.M11 > 0 && m.M22 > 0)
                            return (m.M11, m.M22);
                    }
                }
                catch { }
            }

            try
            {
                var primary = Screen.PrimaryScreen;
                if (primary != null && SystemParameters.PrimaryScreenWidth > 0 && SystemParameters.PrimaryScreenHeight > 0)
                {
                    double dx = primary.Bounds.Width / SystemParameters.PrimaryScreenWidth;
                    double dy = primary.Bounds.Height / SystemParameters.PrimaryScreenHeight;
                    if (dx > 0.1 && dy > 0.1)
                        return (dx, dy);
                }
            }
            catch { }

            return (1.0, 1.0);
        }

        /// <summary>
        /// 获取当前所有显示器的 WPF 逻辑坐标快照
        /// </summary>
        public static IReadOnlyList<ScreenInfo> GetScreens(Visual? referenceVisual = null)
        {
            var (dpiX, dpiY) = GetSystemDpiScale(referenceVisual);
            lock (_cacheLock)
            {
                if (_cachedScreens != null
                    && Math.Abs(_cachedDpiX - dpiX) < 0.001
                    && Math.Abs(_cachedDpiY - dpiY) < 0.001)
                {
                    return _cachedScreens;
                }

                var list = new List<ScreenInfo>();
                try
                {
                    foreach (var s in Screen.AllScreens)
                    {
                        var rect = new Rect(
                            s.Bounds.X / dpiX,
                            s.Bounds.Y / dpiY,
                            s.Bounds.Width / dpiX,
                            s.Bounds.Height / dpiY
                        );
                        list.Add(new ScreenInfo(s.DeviceName, rect, s.Primary));
                    }
                }
                catch { }

                if (list.Count == 0)
                {
                    list.Add(new ScreenInfo(
                        "PRIMARY",
                        new Rect(0, 0, Math.Max(SystemParameters.PrimaryScreenWidth, 800), Math.Max(SystemParameters.PrimaryScreenHeight, 600)),
                        true
                    ));
                }

                _cachedScreens = list;
                _cachedDpiX = dpiX;
                _cachedDpiY = dpiY;
                return _cachedScreens;
            }
        }

        /// <summary>
        /// 获取主屏幕的逻辑坐标矩形
        /// </summary>
        public static Rect GetPrimaryScreenRect(Visual? referenceVisual = null)
            => GetPrimaryScreenRect(GetScreens(referenceVisual));

        public static Rect GetPrimaryScreenRect(IReadOnlyList<ScreenInfo>? screens)
        {
            if (screens != null)
            {
                for (int i = 0; i < screens.Count; i++)
                {
                    if (screens[i].IsPrimary)
                        return screens[i].LogicalBounds;
                }
                if (screens.Count > 0)
                    return screens[0].LogicalBounds;
            }
            return new Rect(0, 0, Math.Max(SystemParameters.PrimaryScreenWidth, 800), Math.Max(SystemParameters.PrimaryScreenHeight, 600));
        }

        /// <summary>
        /// 按设备名查找显示器
        /// </summary>
        public static ScreenInfo? FindScreenByDeviceName(IReadOnlyList<ScreenInfo>? screens, string? deviceName)
        {
            if (screens == null || string.IsNullOrWhiteSpace(deviceName))
                return null;
            for (int i = 0; i < screens.Count; i++)
            {
                if (string.Equals(screens[i].DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                    return screens[i];
            }
            return null;
        }

        /// <summary>
        /// 查找窗口所在（重叠面积最大）或距离最近的显示器
        /// </summary>
        public static ScreenInfo FindContainingOrNearestScreen(IReadOnlyList<ScreenInfo>? screens, Rect windowRect)
        {
            if (screens == null || screens.Count == 0)
                return new ScreenInfo("PRIMARY", GetPrimaryScreenRect((IReadOnlyList<ScreenInfo>?)null), true);

            double w = Math.Max(windowRect.Width, 1);
            double h = Math.Max(windowRect.Height, 1);
            var safeWin = new Rect(windowRect.X, windowRect.Y, w, h);
            double cx = safeWin.X + w / 2.0;
            double cy = safeWin.Y + h / 2.0;

            int bestIndex = -1;
            double bestArea = 0;

            for (int i = 0; i < screens.Count; i++)
            {
                double area = GetIntersectionArea(screens[i].LogicalBounds, safeWin);
                if (area > bestArea)
                {
                    bestArea = area;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
                return screens[bestIndex];

            for (int i = 0; i < screens.Count; i++)
            {
                if (ContainsPointInclusive(screens[i].LogicalBounds, cx, cy))
                    return screens[i];
            }

            // 完全在虚空中：找中心点距离最近的屏幕
            int nearestIndex = 0;
            double minDistSq = double.MaxValue;
            for (int i = 0; i < screens.Count; i++)
            {
                double d = PointToRectDistanceSquared(cx, cy, screens[i].LogicalBounds);
                if (d < minDistSq)
                {
                    minDistSq = d;
                    nearestIndex = i;
                }
            }
            return screens[nearestIndex];
        }

        /// <summary>
        /// 计算多屏并集下，窗口在当前位置沿水平/垂直方向各自可达的最大边界矩形。
        /// 支持 L 形、错位、竖屏+横屏等任意拓扑，接缝处贯通、空缺虚空处截断。
        /// </summary>
        public static Rect GetUnionBounds(IReadOnlyList<ScreenInfo>? screens, Rect windowRect)
        {
            if (screens == null || screens.Count == 0)
                return GetPrimaryScreenRect((IReadOnlyList<ScreenInfo>?)null);

            var rects = new List<Rect>(screens.Count);
            for (int i = 0; i < screens.Count; i++)
                rects.Add(screens[i].LogicalBounds);

            return GetReachableBounds(rects, windowRect);
        }

        /// <summary>
        /// 在一组有效矩形（屏幕集合或被自定义区域裁剪后的屏幕集合）上计算窗口当前位置的可达边界。
        /// </summary>
        public static Rect GetReachableBounds(IReadOnlyList<Rect>? validRects, Rect windowRect)
        {
            if (validRects == null || validRects.Count == 0)
                return GetPrimaryScreenRect((IReadOnlyList<ScreenInfo>?)null);

            double w = Math.Max(windowRect.Width, 1);
            double h = Math.Max(windowRect.Height, 1);
            var safeWin = new Rect(windowRect.X, windowRect.Y, w, h);
            double cx = safeWin.X + w / 2.0;
            double cy = safeWin.Y + h / 2.0;

            // 1. 寻找锚点矩形（最大交集面积优先；无交集则找最近矩形直接拉回）
            int anchorIdx = -1;
            double bestArea = 0;
            for (int i = 0; i < validRects.Count; i++)
            {
                double area = GetIntersectionArea(validRects[i], safeWin);
                if (area > bestArea)
                {
                    bestArea = area;
                    anchorIdx = i;
                }
            }

            if (anchorIdx < 0)
            {
                for (int i = 0; i < validRects.Count; i++)
                {
                    if (ContainsPointInclusive(validRects[i], cx, cy))
                    {
                        anchorIdx = i;
                        break;
                    }
                }
            }

            if (anchorIdx < 0)
            {
                // 窗口完全位于所有有效区域之外（虚空）：直接返回最近的有效区域以便立刻拉回
                int nearestIdx = 0;
                double minDistSq = double.MaxValue;
                for (int i = 0; i < validRects.Count; i++)
                {
                    double d = PointToRectDistanceSquared(cx, cy, validRects[i]);
                    if (d < minDistSq)
                    {
                        minDistSq = d;
                        nearestIdx = i;
                    }
                }
                return validRects[nearestIdx];
            }

            Rect anchor = validRects[anchorIdx];
            // 侧边隐藏等边缘悬空场景下，将中心点投影到锚点内以保持相邻屏幕贯通判定稳定
            double clampedCx = Math.Clamp(cx, anchor.Left, anchor.Right);
            double clampedCy = Math.Clamp(cy, anchor.Top, anchor.Bottom);

            // 2. 水平方向合并相邻且纵向覆盖 clampedCy 的有效矩形
            double minX = anchor.Left;
            double maxX = anchor.Right;
            bool expanded = true;
            while (expanded)
            {
                expanded = false;
                for (int i = 0; i < validRects.Count; i++)
                {
                    Rect r = validRects[i];
                    if (clampedCy < r.Top - AdjacentTolerance || clampedCy > r.Bottom + AdjacentTolerance)
                        continue;

                    if (r.Left <= maxX + AdjacentTolerance && r.Right >= minX - AdjacentTolerance)
                    {
                        if (r.Left < minX)
                        {
                            minX = r.Left;
                            expanded = true;
                        }
                        if (r.Right > maxX)
                        {
                            maxX = r.Right;
                            expanded = true;
                        }
                    }
                }
            }

            // 3. 垂直方向合并相邻且横向覆盖 clampedCx 的有效矩形
            double minY = anchor.Top;
            double maxY = anchor.Bottom;
            expanded = true;
            while (expanded)
            {
                expanded = false;
                for (int i = 0; i < validRects.Count; i++)
                {
                    Rect r = validRects[i];
                    if (clampedCx < r.Left - AdjacentTolerance || clampedCx > r.Right + AdjacentTolerance)
                        continue;

                    if (r.Top <= maxY + AdjacentTolerance && r.Bottom >= minY - AdjacentTolerance)
                    {
                        if (r.Top < minY)
                        {
                            minY = r.Top;
                            expanded = true;
                        }
                        if (r.Bottom > maxY)
                        {
                            maxY = r.Bottom;
                            expanded = true;
                        }
                    }
                }
            }

            return new Rect(minX, minY, Math.Max(maxX - minX, 0), Math.Max(maxY - minY, 0));
        }

        /// <summary>
        /// 将自定义移动区域与真实显示器集合求交，确保不会包含虚空区域。
        /// 若自定义区域完全落入虚空，则回退到主屏幕。
        /// </summary>
        public static Rect GetCustomAreaBounds(IReadOnlyList<ScreenInfo> screens, Rect customArea, Rect windowRect)
        {
            var clipped = ClipRectToScreens(screens, customArea, MinValidSize);
            if (clipped.Count == 0)
                return GetPrimaryScreenRect(screens);
            return GetReachableBounds(clipped, windowRect);
        }

        /// <summary>
        /// 将矩形裁剪到所有真实显示器内，返回所有有效交集片段
        /// </summary>
        public static List<Rect> ClipRectToScreens(IReadOnlyList<ScreenInfo> screens, Rect area, double minSize = MinValidSize)
        {
            var result = new List<Rect>();
            if (screens == null || area.Width < minSize || area.Height < minSize)
                return result;

            for (int i = 0; i < screens.Count; i++)
            {
                Rect inter = Rect.Intersect(screens[i].LogicalBounds, area);
                if (!inter.IsEmpty && inter.Width >= minSize && inter.Height >= minSize)
                {
                    result.Add(inter);
                }
            }
            return result;
        }

        /// <summary>
        /// 判断矩形是否与任意真实显示器存在有效交集
        /// </summary>
        public static bool HasValidScreenIntersection(IReadOnlyList<ScreenInfo> screens, Rect area, double minSize = MinValidSize)
            => ClipRectToScreens(screens, area, minSize).Count > 0;

        /// <summary>
        /// 计算矩形落在真实显示器内的面积占比（0.0 ~ 1.0）。
        /// 用于识别像旧版 Setting.lps 中部分落入虚空的过期 movearea。
        /// </summary>
        public static double GetScreenCoverageRatio(IReadOnlyList<ScreenInfo> screens, Rect area)
        {
            if (screens == null || area.Width <= 0 || area.Height <= 0)
                return 0.0;

            double totalArea = area.Width * area.Height;
            double coveredArea = 0.0;
            for (int i = 0; i < screens.Count; i++)
            {
                coveredArea += GetIntersectionArea(screens[i].LogicalBounds, area);
            }
            return Math.Min(1.0, coveredArea / totalArea);
        }

        /// <summary>
        /// 尝试修复落入或部分落入虚空的自定义/历史 movearea 矩形。
        /// 返回 true 表示需要修正，repaired 为修正后的安全矩形；若完全失效则 repaired 为 Rect.Empty（应回退主屏）。
        /// </summary>
        public static bool TryRepairMoveArea(IReadOnlyList<ScreenInfo> screens, Rect moveArea, out Rect repaired)
        {
            repaired = moveArea;
            if (moveArea.Width < MinValidSize || moveArea.Height < MinValidSize)
            {
                repaired = Rect.Empty;
                return true;
            }

            var clipped = ClipRectToScreens(screens, moveArea, MinValidSize);
            if (clipped.Count == 0)
            {
                repaired = Rect.Empty;
                return true;
            }

            double coverage = GetScreenCoverageRatio(screens, moveArea);
            if (coverage >= 0.98)
            {
                // 几乎完全位于真实显示器内，无需改动
                return false;
            }

            // 存在明显虚空区域（例如旧布局快照 -1920..0 与竖屏 -1152..0）：
            // 若只与单一屏幕相交，直接裁剪为该交集；若跨多屏则收紧为所有有效交集片段的最小外接矩形（内凹 L 形虚空在运行时由 GetCustomAreaBounds 实时剔除）
            if (clipped.Count == 1)
            {
                repaired = clipped[0];
                return true;
            }

            Rect tightBounds = clipped[0];
            for (int i = 1; i < clipped.Count; i++)
            {
                tightBounds.Union(clipped[i]);
            }

            if (Math.Abs(tightBounds.X - moveArea.X) > 1
                || Math.Abs(tightBounds.Y - moveArea.Y) > 1
                || Math.Abs(tightBounds.Width - moveArea.Width) > 1
                || Math.Abs(tightBounds.Height - moveArea.Height) > 1)
            {
                repaired = tightBounds;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将窗口左上角坐标夹紧到指定边界内
        /// </summary>
        public static Point ClampWindowPosition(Rect bounds, double left, double top, double width, double height)
        {
            double maxLeft = Math.Max(bounds.Left, bounds.Right - Math.Max(width, 0));
            double maxTop = Math.Max(bounds.Top, bounds.Bottom - Math.Max(height, 0));
            return new Point(
                Math.Clamp(left, bounds.Left, maxLeft),
                Math.Clamp(top, bounds.Top, maxTop)
            );
        }

        public static System.Drawing.Rectangle ToDrawingRectangle(Rect rect)
            => new System.Drawing.Rectangle(
                (int)Math.Round(rect.X),
                (int)Math.Round(rect.Y),
                (int)Math.Round(rect.Width),
                (int)Math.Round(rect.Height)
            );

        public static Rect FromDrawingRectangle(System.Drawing.Rectangle rect)
            => new Rect(rect.X, rect.Y, Math.Max(rect.Width, 0), Math.Max(rect.Height, 0));

        private static double GetIntersectionArea(Rect a, Rect b)
        {
            double left = Math.Max(a.Left, b.Left);
            double top = Math.Max(a.Top, b.Top);
            double right = Math.Min(a.Right, b.Right);
            double bottom = Math.Min(a.Bottom, b.Bottom);
            if (right <= left || bottom <= top)
                return 0.0;
            return (right - left) * (bottom - top);
        }

        private static bool ContainsPointInclusive(Rect r, double x, double y)
            => x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom;

        private static double PointToRectDistanceSquared(double px, double py, Rect r)
        {
            double dx = 0.0;
            if (px < r.Left) dx = r.Left - px;
            else if (px > r.Right) dx = px - r.Right;

            double dy = 0.0;
            if (py < r.Top) dy = r.Top - py;
            else if (py > r.Bottom) dy = py - r.Bottom;

            return dx * dx + dy * dy;
        }
    }
}
