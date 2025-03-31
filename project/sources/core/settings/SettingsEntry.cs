using System;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace Core.Settings;

public interface ISettingsEntry
{
    string Path { get; }
    bool IsDefault { get; }
    void Load(ConfigFile configFile);
    void Save(ConfigFile configFile);
}

public class SettingsEntry<[MustBeVariant] T> : ISettingsEntry
{
    public readonly string Section;
    public readonly string Key;
    private readonly Action<T>? applyDelegate;

    private Variant value;

    public SettingsEntry(string section, string key, in T defaultValue, Action<T>? applyDelegate = null)
    {
        this.Section = section;
        this.Key = key;
        this.value = Variant.From(defaultValue);
        this.applyDelegate = applyDelegate;
        this.IsDefault = true;
    }

    public virtual T Value
    {
        get => this.value.As<T>();
        set
        {
            T? lastValue = this.value.As<T>();
            if ((value == null && lastValue != null) || (value != null && !value.Equals(lastValue)))
            {
                this.IsDefault = false;
            }

            this.value = Variant.From(value);
            this.applyDelegate?.Invoke(value);
        }
    }

    public string Path => $"{this.Section}/{this.Key}";

    public bool IsDefault
    {
        get;
        private set;
    }

    public void Load(ConfigFile configFile)
    {
        this.Value = configFile.GetValue(this.Section, this.Key, this.value).As<T>();
        if (configFile.HasSectionKey(this.Section, this.Key))
        {
            this.IsDefault = false;
        }
    }

    public void Save(ConfigFile configFile)
    {
        configFile.SetValue(this.Section, this.Key, this.value);
        this.IsDefault = false;
    }
}

public class EnumEntry<[MustBeVariant] T> : SettingsEntry<T>
    where T : Enum
{
    public string[] PossibleValueNames;
    public T[] PossibleValue;

    public EnumEntry(string category, string name, in T defaultValue, Action<T> applyDelegate, params T[] possibleValues) : base(category, name, defaultValue, applyDelegate)
    {
        Debug.Assert(possibleValues.Contains(defaultValue));
        this.PossibleValue = possibleValues;
        this.PossibleValueNames = possibleValues.Select(value => value.ToString()).ToArray();
    }

    public override T Value
    {
        get => base.Value;
        set
        {
            Debug.Assert(this.PossibleValue.Contains(value));
            base.Value = value;
        }
    }

    public void FillControl(OptionButton optionButton)
    {
        optionButton.Clear();
        for (int index = 0; index < this.PossibleValue.Length; index++)
        {
            optionButton.AddItem(this.PossibleValueNames[index], id: Convert.ToInt32(this.PossibleValue[index]));
        }

        optionButton.Selected = optionButton.GetItemIndex(Convert.ToInt32(this.Value));
    }
}
