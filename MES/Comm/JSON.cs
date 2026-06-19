using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.IO;

using Formatting = Newtonsoft.Json.Formatting;

namespace MES.Comm;

/// <summary>
/// 
/// </summary>
public static class JSON
{
    /// <summary>
    /// 将 <typeparamref name="Target"/> 对象进行json序列化同时进行格式化
    /// </summary>
    /// <typeparam name="Target"></typeparam>
    /// <param name="object"></param>
    /// <returns></returns>
    public static string ToJsonFormat<Target>(this Target @object)
    {
        if (@object == null)
        {
            return "{}";
        }

        JsonSerializer jsonSerializer = new();
        using (StringWriter stringWriter = new())
        {
            using (JsonTextWriter jsonText = new(stringWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            })
            {
                jsonSerializer.Serialize(jsonText, @object);

                return stringWriter.ToString();
            }
        }
    }

    /// <summary>
    /// Converts to json.
    /// </summary>
    /// <typeparam name="Target">The type of the arget.</typeparam>
    /// <param name="object">The object.</param>
    /// <returns></returns>
    public static string ToJson<Target>(this Target @object)
    {
        return JsonConvert.SerializeObject(@object);
    }


    /// <summary>
    /// Froms the json.
    /// </summary>
    /// <typeparam name="Target">The type of the arget.</typeparam>
    /// <param name="object">The object.</param>
    /// <returns></returns>
    public static Target FromJson<Target>(this string @object)
    {
        return JsonConvert.DeserializeObject<Target>(@object);
    }


    /// <summary>
    /// Froms the json.
    /// </summary>
    /// <param name="object">The object.</param>
    /// <param name="targetType">Type of the target.</param>
    /// <returns></returns>
    public static object FromJson(this string @object, Type targetType)
    {
        return JsonConvert.DeserializeObject(@object, targetType);
    }
    /// <summary>
    /// Parses the specified target.
    /// </summary>
    /// <typeparam name="Target">The type of the arget.</typeparam>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    public static Target Parse<Target>(object target)
    {
        return target is null
           ? default
           : target is Target t
           ? t
           : JToken.FromObject(target).ToObject<Target>();
    }

    /// <summary>
    /// Parses the specified target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="targetType">Type of the target.</param>
    /// <returns></returns>
    public static object Parse(object target, Type targetType)
    {
        return target is null
            ? default
            : target.GetType().IsSubclassOf(targetType)
            ? target
            : JToken.FromObject(target).ToObject(targetType);
    }

    /// <summary>
    /// Tries the parse.
    /// </summary>
    /// <typeparam name="Target">The type of the arget.</typeparam>
    /// <param name="jsonString">The json string.</param>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    /// 2023/11/25 13:46
    public static bool TryParse<Target>(string jsonString, out Target? target)
    {
        try
        {
            target = JsonConvert.DeserializeObject<Target>(jsonString);

            if (target is not Target _)
            {
                return false;
            }

            return true;
        }
        catch
        {
            target = default;
            return false;
        }
    }

    /// <summary>
    /// Copies the specified target.
    /// </summary>
    /// <typeparam name="Target">The type of the arget.</typeparam>
    /// <param name="target">The target.</param>
    /// <param name="copyConfig">The copy configuration.</param>
    /// <returns></returns>
    public static Target Copy<Target>(Target target, Action<Target> copyConfig = null)
    {
        if (target is null)
        {
            return default;
        }
        Target targetValue = (Target)FromJson(ToJson(target), target.GetType());

        copyConfig?.Invoke(targetValue);

        return targetValue;
    }

    /// <summary>
    /// Copies the specified target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    public static object Copy(object target, Type type)
    {
        if (target is null || type is null)
        {
            return default;
        }

        string jsonString = ToJson(target);

        return FromJson(jsonString, type);

    }
}