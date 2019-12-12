using System;
using System.Collections.Generic;
using fnbot.shop.Fortnite;
using PakReader.Parsers;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;

namespace fnbot.shop.ItemShop
{
    static class Series
    {
        const string BASE_PATH = "/FortniteGame/Content/Athena/Items/Cosmetics/Series/";

        public static SortedList<string, RarityType> RarityTypes { get; private set; }

        public static void Initialize()
        {
            if (RarityTypes != null)
                return;
            RarityTypes = new SortedList<string, RarityType>();
            foreach (var s in PakSupplier.Index)
            {
                var str = s.Substring(0, s.IndexOf('.'));
                if (str.StartsWith(BASE_PATH, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (RarityTypes.ContainsKey(str.Substring(BASE_PATH.Length)))
                        continue;
                    var exp = PakSupplier.Index.GetPackage(str).GetExport<UObject>();
                    var type = new RarityType();
                    foreach (var prop in exp)
                    {
                        switch (prop.Key)
                        {
                            case "DisplayName":
                                type.SeriesName = ((TextProperty)prop.Value).Value;
                                break;
                            case "Colors":
                                var colorProps = (UObject)((StructProperty)prop.Value).Value;
                                var colors = new FLinearColor[colorProps.Count];
                                {
                                    int i = 0;
                                    foreach (var prp in colorProps)
                                    {
                                        colors[i] = (FLinearColor)((StructProperty)prp.Value).Value;
                                        i++;
                                    }
                                }
                                var hsl = Color.RGB2HSL((Color)colors[1]);
                                hsl.h += 5 / 360f;
                                hsl.l += 18f;
                                hsl.s -= .35f;
                                type.GradientColor1 = Color.HSL2RGB(hsl.h, hsl.s, hsl.l);
                                hsl = Color.RGB2HSL((Color)colors[2]);
                                hsl.h += 5 / 360f;
                                hsl.l += .07f;
                                hsl.s -= .32f;
                                type.GradientColor2 = Color.HSL2RGB(hsl.h, hsl.s, hsl.l);

                                hsl = Color.RGB2HSL((Color)colors[0]);
                                hsl.l = .93f;
                                hsl.s = 1;
                                type.TextColor1 = Color.HSL2RGB(hsl.h, hsl.s, hsl.l);
                                hsl = Color.RGB2HSL((Color)colors[1]);
                                hsl.l += .5f;
                                hsl.s = .95f;
                                type.TextColor2 = Color.HSL2RGB(hsl.h, hsl.s, hsl.l);

                                hsl = Color.RGB2HSL((Color)colors[0]);
                                hsl.l -= .15f;
                                hsl.s += .2f;
                                type.SeriesColor = Color.HSL2RGB(hsl.h, hsl.s, hsl.l);
                                break;
                        }
                    }
                    RarityTypes[str.Substring(str.LastIndexOf('/')+1)] = type;
                }
            }
            //RarityTypes.Sort(new Comparison<(string, RarityType)>((a, b) => a.Item1.CompareTo(b.Item1))); reverse the order (technically)
        }
    }
}
