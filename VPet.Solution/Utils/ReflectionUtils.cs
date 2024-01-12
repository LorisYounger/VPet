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
    private static readonly BindingFlags _propertyBindingFlags =
        BindingFlags.Instance | BindingFlags.Public;

    /// <summary>
    /// 类型信息
    /// <para>
    /// (TargetType, (PropertyName, TargetPropertyName))
    /// </para>
    /// </summary>
    private static readonly Dictionary<Type, ReflectionObjectInfo> _typePropertyReflectionInfos =
        new();

    public static void SetValue(object source, object target, ReflectionOptions options = null!)
    {
        options ??= new();
        var sourceType = source.GetType();
        var targetType = target.GetType();
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

        foreach (var property in targetType.GetProperties(_propertyBindingFlags))
        {
            // 尝试获取目标属性信息
            targetInfo.PropertyInfos.TryGetValue(property.Name, out var targetReflectionInfo);
            // 获取源属性名
            var sourcePropertyName = targetReflectionInfo is null
                ? property.Name
                : targetReflectionInfo.TargetName;
            // 获取源属性信息
            sourceInfo.PropertyInfos.TryGetValue(sourcePropertyName, out var sourceReflectionInfo);
            if (sourceInfo.PropertyNames.Contains(sourcePropertyName) is false)
            {
                if (targetReflectionInfo?.IsRequired is true)
                    options.UnassignedRequiredProperties.Add(property.Name);
                continue;
            }

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
                var targetValue = targetAccessor[property.Name];
                if (sourceValue.Equals(targetValue))
                    continue;
            }
            targetAccessor[property.Name] = sourceValue;
        }
    }

    private static ReflectionObjectInfo GetReflectionObjectInfo(Type type)
    {
        var objectInfo = new ReflectionObjectInfo(type);
        foreach (var property in type.GetProperties(_propertyBindingFlags))
        {
            if (
                property.IsDefined(typeof(ReflectionPropertyAttribute)) is false
                && property.IsDefined(typeof(ReflectionPropertyConverterAttribute)) is false
            )
                continue;
            var propertyInfo = new ReflectionPropertyInfo(property.Name);
            // 获取属性信息
            if (
                property.GetCustomAttribute<ReflectionPropertyAttribute>()
                is ReflectionPropertyAttribute propertyInfoAttribute
            )
            {
                if (string.IsNullOrWhiteSpace(propertyInfoAttribute.TargetName) is false)
                    propertyInfo.TargetName = propertyInfoAttribute.TargetName;
                propertyInfo.IsRequired = propertyInfoAttribute.IsRequired;
            }
            // 获取属性转换器
            if (
                property.GetCustomAttribute<ReflectionPropertyConverterAttribute>()
                is ReflectionPropertyConverterAttribute propertyConverterAttribute
            )
            {
                propertyInfo.Converter = propertyConverterAttribute.Converter;
            }
            objectInfo.PropertyInfos[property.Name] = propertyInfo;
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

    public Dictionary<string, ReflectionPropertyInfo> PropertyInfos { get; } = new();

    public ReflectionObjectInfo(Type type)
    {
        PropertyNames = new(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name)
        );
    }
}

public class ReflectionPropertyInfo
{
    /// <summary>
    /// 目标属性名称
    /// </summary>
    public string TargetName { get; set; }

    /// <summary>
    /// 是必要的
    /// </summary>
    [DefaultValue(true)]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 反射值转换器
    /// </summary>
    public IReflectionConverter? Converter { get; set; } = null;

    public ReflectionPropertyInfo(string propertyName)
    {
        TargetName = propertyName;
    }
}

/// <summary>
/// 反射属性信息
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReflectionPropertyAttribute : Attribute
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    /// 是必要的
    /// </summary>
    [DefaultValue(true)]
    public bool IsRequired { get; } = true;

    public ReflectionPropertyAttribute(bool isRequired = true)
    {
        IsRequired = isRequired;
    }

    public ReflectionPropertyAttribute(string name, bool isRequired = true)
    {
        TargetName = name;
        IsRequired = isRequired;
    }
}

/// <summary>
/// 反射属性转换器
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReflectionPropertyConverterAttribute : Attribute
{
    /// <summary>
    /// 反射转换器
    /// </summary>
    public IReflectionConverter Converter { get; }

    public ReflectionPropertyConverterAttribute(Type converterType)
    {
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

    /// <summary>
    /// 未赋值的必要属性
    /// </summary>
    public List<string> UnassignedRequiredProperties { get; set; } = new();
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
