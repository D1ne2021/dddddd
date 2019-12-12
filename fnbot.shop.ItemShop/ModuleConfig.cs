using System.IO;
using fnbot.shop.Backend.Configuration;

namespace fnbot.shop.ItemShop
{
    class ModuleConfig : IConfig
    {
        public readonly ConfigProperty<TextArea> Format;
        public readonly ConfigProperty<StringLabel> Description;
        public readonly ConfigProperty<FilePath> Background;
        public readonly ConfigProperty<TextArea> Example;

        public ModuleConfig()
        {
            Format = new ConfigProperty<TextArea>(null, "Format", true, true);
            Description = new ConfigProperty<StringLabel>("{date} is for the date", "Info", true, true);
            Background = new ConfigProperty<FilePath>(new FilePath("PNG files (*.png)|*.png|All files (*.*)|*.*", null), "Background", true, true);
            Example = new ConfigProperty<TextArea>("Item Shop for {date}!\nUse code \"furry\"!", "Example", false, true);
        }

        public void SaveConfig(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((Format.Value.Value ?? "").Trim());
                writer.Write(Background.Value.Path != null);
                if (Background.Value.Path != null)
                    writer.Write(Background.Value.Path);
            }
        }
        public void LoadConfig(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                Format.Value = reader.ReadString();
                if (reader.ReadBoolean())
                {
                    Background.Value = Background.Value.ChangePath(reader.ReadString());
                }
            }
        }
    }
}
