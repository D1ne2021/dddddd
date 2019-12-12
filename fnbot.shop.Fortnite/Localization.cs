using System;
using System.IO;
using PakReader;
using PakReader.Parsers.Objects;

namespace fnbot.shop.Fortnite
{
    public static class Localization
    {
        const string BASE_PATH = "/FortniteGame/Content/Localization/Game_BR/";
        static LocResReader LocRes;

        public static string DefaultLanguage { get; private set; }
        public static string SelectedLanguage { get; private set; }

        public static void Initialize()
        {
            DefaultLanguage = SelectedLanguage = new LocMetaReader(PakSupplier.Index.GetFile(BASE_PATH + "Game_BR.locmeta").AsStream()).NativeCulture;
        }

        public static void LoadLanguage(string lang)
        {
            if (lang == SelectedLanguage)
                return;
            if (lang != DefaultLanguage) // TODO: cache all the readers (maybe)
            {
                LocRes = new LocResReader(PakSupplier.Index.GetFile($"{BASE_PATH}/{lang}/Game_BR.locres").AsStream());
            }
            else
            {
                LocRes = null;
            }
            SelectedLanguage = lang;
        }

        static MemoryStream AsStream(this ArraySegment<byte> me) => new MemoryStream(me.Array, me.Offset, me.Count, false, true);

        public static string GetString(string Namespace, string Key) => // careful, this might throw a key not found exception if you use the default language
            LocRes[Namespace, Key];

        public static string GetString(this FText txt) =>
            DefaultLanguage == SelectedLanguage ? txt.SourceString : LocRes[txt.Namespace, txt.Key];
    }
}
