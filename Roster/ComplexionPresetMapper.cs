using System.Text.Json;

namespace Madden26Plugin.Roster;

public enum SkinToneGroup
{
    Unknown,
    Light,
    Medium,
    Dark,
    Mannequin,
}

public class ComplexionPresetMapper
{
    private const string ItemPrefix = "content/FootballCharacter/Items/GenericHeads/Player/";

    private static readonly int[] ToneThresholds = { 1, 2 };

    private Dictionary<string, SkinToneGroup> _genericToGroup = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, SkinToneGroup> GenericToGroup => _genericToGroup;

    public SkinToneGroup GetGroup(string genericId)
    {
        return _genericToGroup.TryGetValue(genericId, out var group) ? group : SkinToneGroup.Unknown;
    }

    public int MappedCount => _genericToGroup.Count;

    public bool LoadDefaultToneMap(string filePath = null)
    {
        filePath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "face_tone_map.json");
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            DeserializeMapping(json);
            return _genericToGroup.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public string SerializeMapping()
    {
        var dict = _genericToGroup.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }

    public void DeserializeMapping(string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict == null) return;

        _genericToGroup.Clear();
        foreach (var kv in dict)
        {
            if (Enum.TryParse<SkinToneGroup>(kv.Value, true, out var group))
                _genericToGroup[kv.Key] = group;
        }
    }

    internal static SkinToneGroup MapToneValue(int? value)
    {
        if (!value.HasValue)
            return SkinToneGroup.Unknown;

        return value.Value switch
        {
            0 => SkinToneGroup.Light,
            1 => SkinToneGroup.Medium,
            >= 2 => SkinToneGroup.Dark,
            _ => SkinToneGroup.Unknown,
        };
    }

    internal static object FindField(object obj, string fieldName)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        var prop = type.GetProperty(fieldName);
        if (prop != null)
        {
            var val = prop.GetValue(obj);
            if (val != null) return val;
        }

        var field = type.GetField(fieldName);
        if (field != null)
        {
            var val = field.GetValue(obj);
            if (val != null) return val;
        }

        var indexer = type.GetProperty("Item", new[] { typeof(string) });
        if (indexer != null)
        {
            try { return indexer.GetValue(obj, new object[] { fieldName }); }
            catch { }
        }

        var getValue = type.GetMethod("GetValue", new[] { typeof(string) });
        if (getValue != null)
        {
            try { return getValue.Invoke(obj, new object[] { fieldName }); }
            catch { }
        }

        var getFieldValue = type.GetMethod("GetFieldValue", new[] { typeof(string) });
        if (getFieldValue != null)
        {
            try { return getFieldValue.Invoke(obj, new object[] { fieldName }); }
            catch { }
        }

        return null;
    }

    internal static void SetFieldValue(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var type = obj.GetType();
        var prop = type.GetProperty(fieldName);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(obj, value);
            return;
        }
        var field = type.GetField(fieldName);
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }
    }

    internal static object CloneObject(object original)
    {
        if (original == null) return null;
        var t = original.GetType();
        try
        {
            var clone = Activator.CreateInstance(t);
            foreach (var prop in t.GetProperties())
            {
                if (prop.CanRead && prop.CanWrite && prop.GetIndexParameters().Length == 0)
                    prop.SetValue(clone, prop.GetValue(original));
            }
            foreach (var field in t.GetFields())
                field.SetValue(clone, field.GetValue(original));
            return clone;
        }
        catch
        {
            return original;
        }
    }

    public static int? ExtractSkinToneBaseValue(string genericId)
    {
        if (string.IsNullOrEmpty(genericId))
            return null;

        var parts = genericId.Split('_');
        if (parts.Length < 2)
            return null;

        if (int.TryParse(parts[^1], out var val))
            return val;

        return null;
    }

    public static string ToneFromTemplateId(string genericId)
    {
        var val = ExtractSkinToneBaseValue(genericId);
        return val.HasValue ? MapToneValue(val).ToString() : "";
    }
}
