using System;

namespace fnbot.shop.Backend.Configuration
{
    public abstract class ConfigProperty
    {
    }
    public class ConfigProperty<T> : ConfigProperty
    {
        string name_;
        bool enabled_;
        bool visible_;
        T value_;
        public string Name { get => name_; set { name_ = value; SetNameCallback?.Invoke(value); } }
        public bool Enabled { get => enabled_; set { enabled_ = value; SetEnabledCallback?.Invoke(value); } }
        public bool Visible { get => visible_; set { visible_ = value; SetVisibleCallback?.Invoke(value); } }
        public T Value { get => value_; set { value_ = value; SetValueCallback?.Invoke(value); } }


        public Action<string> SetNameCallback { get; set; }
        public Action<bool> SetEnabledCallback { get; set; }
        public Action<bool> SetVisibleCallback { get; set; }
        public Action<T> SetValueCallback { get; set; }
        public void UISetValue(T value)
        {
            value_ = value;
        }

        public ConfigProperty(T defaultValue, string name, bool enabled, bool visible)
        {
            value_ = defaultValue;
            name_ = name;
            enabled_ = enabled;
            visible_ = visible;
        }

        public override string ToString() => value_.ToString();
    }
}
