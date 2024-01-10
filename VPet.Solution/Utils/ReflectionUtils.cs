using FastMember;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HKW.HKWUtils;

public static class ReflectionUtils
{
    /// <summary>
    /// 目标名称
    /// <para>
    /// (TargetType, (PropertyName, TargetPropertyName))
    /// </para>
    /// </summary>
    private static readonly Dictionary<Type, ReflectionObjectInfo> _typePropertyReflectionInfos =
        new();

    public static void SetValue<TSource, TTarget>(
        TSource source,
        TTarget target,
        ReflectionOptions options = null!
    )
    {
        options ??= new();
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_typePropertyReflectionInfos.TryGetValue(sourceType, out var sourceInfo) is false)
            sourceInfo = _typePropertyReflectionInfos[sourceType] = GetReflectionObjectInfo(
                sourceType
            );
        if (_typePropertyReflectionInfos.TryGetValue(targetType, out var targetInfo) is false)
            targetInfo = _typePropertyReflectionInfos[targetType] = GetReflectionObjectInfo(
                targetType
            );

        var sourceAccessor = ObjectAccessor.Create(source);
        var targetAccessor = ObjectAccessor.Create(target);

        foreach (var property in sourceType.GetProperties())
        {
            // 获取源属性名
            var sourcePropertyName = sourceInfo.PropertyInfos.TryGetValue(
                property.Name,
                out var sourceReflectionInfo
            )
                ? sourceReflectionInfo.PropertyName
                : property.Name;
            if (targetInfo.PropertyNames.Contains(sourcePropertyName) is false)
                continue;
            // 获取目标属性名
            var targetPropertyName = targetInfo.PropertyInfos.TryGetValue(
                sourcePropertyName,
                out var targetReflectionInfo
            )
                ? targetReflectionInfo.PropertyName
                : property.Name;
            // 获取源值
            var sourceValue = sourceAccessor[sourcePropertyName];
            // 转换源值
            if (sourceReflectionInfo?.Converter is IReflectionConverter sourceConverter)
                sourceValue = sourceConverter.Convert(sourceValue);
            else if (targetReflectionInfo?.Converter is IReflectionConverter targetConverter)
                sourceValue = targetConverter.ConvertBack(sourceValue);
            // 比较源值和目标值
            if (options.CheckValueEquals)
            {
                var targetValue = targetAccessor[targetPropertyName];
                if (sourceValue.Equals(targetValue))
                    continue;
            }
            targetAccessor[targetPropertyName] = sourceValue;
        }
    }

    private static ReflectionObjectInfo GetReflectionObjectInfo(Type type)
    {
        var objectInfo = new ReflectionObjectInfo(type);
        foreach (var property in type.GetProperties())
        {
            if (property.IsDefined(typeof(ReflectionPropertyInfoAttribute)))
            {
                var reflectionInfo = property.GetCustomAttribute<ReflectionPropertyInfoAttribute>();
                if (string.IsNullOrWhiteSpace(reflectionInfo.PropertyName))
                    reflectionInfo.PropertyName = property.Name;
                objectInfo.PropertyInfos[property.Name] = reflectionInfo;
            }
        }
        return objectInfo;
    }
}

/// <summary>
/// 反射对象信息
/// </summary>
public class ReflectionObjectInfo
{
    public HashSet<string> PropertyNames { get; }

    public Dictionary<string, ReflectionPropertyInfoAttribute> PropertyInfos { get; } = new();

    public ReflectionObjectInfo(Type type)
    {
        PropertyNames = new(type.GetProperties().Select(p => p.Name));
    }
}

/// <summary>
/// 反射属性信息
/// </summary>
public class ReflectionPropertyInfoAttribute : Attribute
{
    /// <summary>
    /// 目标属性名称
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// 反射转换器
    /// </summary>
    public IReflectionConverter? Converter { get; } = null;

    public ReflectionPropertyInfoAttribute(Type converterType)
        : this(string.Empty, converterType) { }

    public ReflectionPropertyInfoAttribute(string name, Type? converterType = null)
    {
        PropertyName = name;
        if (converterType is null)
            return;
        Converter = (IReflectionConverter)TypeAccessor.Create(converterType).CreateNew();
    }
}

/// <summary>
/// 反射设置
/// </summary>
public class ReflectionOptions
{
    /// <summary>
    /// 检查值是否相等, 若相等则跳过赋值
    /// </summary>
    [DefaultValue(false)]
    public bool CheckValueEquals { get; set; } = false;
}

/// <summary>
/// 反射转换器
/// </summary>
public interface IReflectionConverter
{
    public object Convert(object sourceValue);
    public object ConvertBack(object targetValue);
}

/// <summary>
/// 反射转换器
/// </summary>
/// <typeparam name="TSource">源值类型</typeparam>
/// <typeparam name="TTarget">目标值类型</typeparam>
public abstract class ReflectionConverterBase<TSource, TTarget> : IReflectionConverter
{
    public abstract TTarget Convert(TSource sourceValue);

    public abstract TSource ConvertBack(TTarget targetValue);

    object IReflectionConverter.Convert(object sourceValue)
    {
        return Convert((TSource)sourceValue);
    }

    object IReflectionConverter.ConvertBack(object targetValue)
    {
        return ConvertBack((TTarget)targetValue);
    }
}
