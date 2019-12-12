using System;
using System.Collections.Generic;
using System.Text;
using PakReader.Pak;
using PakReader.Parsers;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using static fnbot.shop.Fortnite.Storefront;

namespace fnbot.shop.ItemShop
{
    class Converter
    {
        public PakIndex Index;

        public Converter(PakIndex index)
        {
            Index = index;
        }

        public StoreItem ConvertItem(Entry entry)
        {
            var ret = new StoreItem();

            if (entry.MetaInfo != null)
            {
                foreach (var mi in entry.MetaInfo)
                {
                    switch (mi.Key)
                    {
                        case "EncryptionKey":
                            Index.UseKey(Convert.FromBase64String(mi.Value.Split(':', 3)[1]));
                            break;
                        case "BannerOverride":
                            ret.Banner = mi.Value.ToUpperInvariant();
                            break;
                    }
                }
            }

            var displayAssetPath = entry.DisplayAssetPath ?? "None";
            var _p = GetItemPath(entry.ItemGrants[0].TemplateID, out ret.Type);
            Console.WriteLine("Getting " + _p);
            var exp = Index.GetPackage(_p).GetExport<UObject>();
            foreach (var prop in exp)
            {
                switch (prop.Key)
                {
                    case "Rarity":
                        if (ret.Rarity == Rarity.UNCOMMON)
                            ret.Rarity = GetRarity((EnumProperty)prop.Value);
                        break;
                    case "DisplayName":
                        ret.Name = ((TextProperty)prop.Value).Value;
                        break;
                    case "ShortDescription":
                        ret.ShortDescription = ((TextProperty)prop.Value).Value;
                        break;
                    case "Description":
                        ret.Description = ((TextProperty)prop.Value).Value;
                        break;
                    case "Series":
                        ret.Series = ((ObjectProperty)prop.Value).Value.Resource.ObjectName.String.ToLowerInvariant();
                        break;
                    case "DisplayAssetPath":
                        if (displayAssetPath == "None")
                            displayAssetPath = ((FSoftObjectPath)((StructProperty)prop.Value).Value).AssetPathName.String;
                        break;
                    case "LargePreviewImage":
                        if (ret.StandardIcon == null)
                            ret.StandardIcon = ((SoftObjectProperty)prop.Value).Value.AssetPathName.String;
                        break;
                    case "HeroDefinition": // skin
                        foreach (var defProp in Index.GetPackage("/FortniteGame/Content/Athena/Heroes/" + ((ObjectProperty)prop.Value).Value.Resource.ObjectName.String).GetExport<UObject>())
                        {
                            switch (defProp.Key)
                            {
                                case "LargePreviewImage":
                                    ret.StandardIcon = ((SoftObjectProperty)defProp.Value).Value.AssetPathName.String;
                                    break;
                            }
                        }
                        break;
                    case "WeaponDefinition": // pick
                        var path = "/FortniteGame/Content/Athena/Items/Weapons/" + ((ObjectProperty)prop.Value).Value.Resource.ObjectName.String;
                        Console.WriteLine("getting pickaxe: " + path);
                        var uobj = Index.GetPackage("/FortniteGame/Content/Athena/Items/Weapons/" + ((ObjectProperty)prop.Value).Value.Resource.ObjectName.String).GetExport<UObject>();
                        foreach (var defProp in uobj)
                        {
                            switch (defProp.Key)
                            {
                                case "LargePreviewImage":
                                    ret.StandardIcon = ((SoftObjectProperty)defProp.Value).Value.AssetPathName.String;
                                    break;
                            }
                        }
                        break;
                }
            }

            if (displayAssetPath != "None")
            {
                exp = Index.GetPackage(ConvertObjectPath(displayAssetPath)).GetExport<UObject>();
                foreach (var prop in exp)
                {
                    switch (prop.Key)
                    {
                        case "DisplayName":
                            ret.Name = ((TextProperty)prop.Value).Value;
                            break;
                        case "ShortDescription":
                            ret.ShortDescription = ((TextProperty)prop.Value).Value;
                            break;
                        case "Description":
                            ret.Description = ((TextProperty)prop.Value).Value;
                            break;
                        case "TileImage":
                        case "DetailsImage":
                            var ind = ((ObjectProperty)((UObject)((StructProperty)prop.Value).Value)["ResourceObject"]).Value;
                            if (string.IsNullOrWhiteSpace(ind.Resource.ObjectName.String))
                            {
                                throw new NotImplementedException("WRAPS NOT ADDED YET");
                                /*/ most likely an item wrap
                                foreach (var mProp in (await Index.GetPackage("/FortniteGame/Content/UI/Foundation/Textures/Icons/Wraps/FeaturedMaterials/" + packageIndex.import).GetUObjectAsync()).properties)
                                {
                                    if (mProp.name == "TextureParameterValues")
                                    {
                                        ret.FeaturedIcon = ((FPackageIndex)((FStructFallback)((UScriptStruct)((UScriptArray)mProp.tag_data).data[0]).struct_type).properties[1].tag_data).outer_import;
                                    }
                                }*/
                            }
                            else
                            {
                                ret.FeaturedIcon = ind.Resource.OuterIndex.Resource.ObjectName.String;
                                if (ret.FeaturedIcon.Contains("MI_UI"))
                                {
                                    // yikes but whatever lol
                                    ret.FeaturedIcon = ((ObjectProperty)((UObject)((StructProperty)((ArrayProperty)Index.GetPackage(ConvertObjectPath(ret.FeaturedIcon)).GetExport<UObject>()["TextureParameterValues"]).Value[0]).Value)["ParameterValue"]).Value.Resource.OuterIndex.Resource.ObjectName.String;
                                }
                            }
                            break;
                    }
                }
                if (ret.Type == ItemType.ITEMWRAP)
                {
                    ret.StandardIcon = ret.FeaturedIcon;
                }
                ret.FeaturedIcon = ConvertObjectPath(ret.FeaturedIcon);
            }
            ret.StandardIcon = ConvertObjectPath(ret.StandardIcon);
            if (entry.Categories.Length != 0)
                ret.FeaturedCategory = int.Parse(entry.Categories[0].Split(' ', 2)[1]);
            ret.SortPriority = entry.SortPriority;

            switch (entry.OfferType)
            {
                case "DynamicBundle":
                    int basePrice = 0;
                    var items = entry.BundleInfo.BundleItems;
                    for (int i = 0; i < items.Length; i++)
                    {
                        basePrice += items[i].RegularPrice;
                    }
                    DiscountedPrice price;
                    ret.Price = price = new DiscountedPrice(basePrice + entry.BundleInfo.DiscountedBasePrice, basePrice);
                    switch (entry.BundleInfo.DisplayType)
                    {
                        case "PercentOff":
                            ret.Banner = $"{(1 - ((float)price.Price / price.OriginalPrice)) * 100}% OFF";
                            break;
                        case "AmountOff":
                            ret.Banner = $"{price.OriginalPrice - price.Price} V-BUCKS OFF";
                            break;
                    }
                    ret.Type = ItemType.BUNDLE;
                    //ret.ShortDescription.Text.SourceString = $"{items.Length} ITEM BUNDLE"; // TODO: fix this please lmao
                    break;
                case "StaticPrice":
                default:
                    ret.Price = new StaticPrice(entry.Prices[0].FinalPrice);
                    break;
            }
            return ret;
        }

        static void GetCategoryInfo(StoreItem[] entries, out int categoryCount, out int[] categorySizes, out int maxCategorySize)
        {
            categoryCount = 0;
            var sizes = new Dictionary<int, int>();
            for (int i = 0; i < entries.Length; i++)
            {
                var category = entries[i].FeaturedCategory - 1;
                sizes.TryGetValue(category, out var categSize);
                sizes[category] = ++categSize;
                if (categoryCount < category)
                    categoryCount = category + 1;
            }
            categorySizes = new int[sizes.Count];
            for (int i = 0; i < categorySizes.Length; i++)
            {
                categorySizes[i] = sizes[i];
            }
            maxCategorySize = 0;
            foreach(var size in categorySizes)
            {
                if (size > maxCategorySize)
                    maxCategorySize = size;
            }
        }

        public static StoreItem[][] GetCategorySlots(StoreItem[] entries, out int categoryCount, out int[] categorySizes, out int maxCategorySize)
        {
            Array.Sort(entries);
            GetCategoryInfo(entries, out categoryCount, out categorySizes, out maxCategorySize);
            var ret = new StoreItem[maxCategorySize][];
            for(int i = 0; i < maxCategorySize; i++)
            {
                ret[i] = new StoreItem[categoryCount];
            }
            var s = new int[categoryCount];
            for (int i = 0; i < entries.Length; i++)
            {
                var category = entries[i].FeaturedCategory - 1;
                ret[s[category]++][category] = entries[i];
            }
            for(int i = 0; i < maxCategorySize; i++)
            {
                for (int j = 0; j < categoryCount; j++)
                {
                    if (ret[i][j] == null)
                    {
                        ret[i][j] = ret[s[j] - 1][j];
                    }
                }
            }
            return ret;
        }

        static string GetItemPath(string templateId, out ItemType type)
        {
            var parts = templateId.Split(":");
            var builder = new StringBuilder("/FortniteGame/Content/Athena/Items/Cosmetics/");
            switch (parts[0])
            {
                case "AthenaCharacter":
                    builder.Append("Characters");
                    type = ItemType.CHARACTER;
                    break;
                case "AthenaBackpack":
                    builder.Append(parts[1].StartsWith("petcarrier") ? "PetCarriers" : "Backpacks");
                    type = parts[1].StartsWith("petcarrier") ? ItemType.PET : ItemType.BACKPACK;
                    break;
                case "AthenaItemWrap":
                    builder.Append("ItemWraps");
                    type = ItemType.ITEMWRAP;
                    break;
                case "AthenaGlider":
                    builder.Append("Gliders");
                    type = ItemType.GLIDER;
                    break;
                case "AthenaPickaxe":
                    builder.Append("Pickaxes");
                    type = ItemType.PICKAXE;
                    break;
                case "AthenaMusicPack":
                    builder.Append("MusicPacks");
                    type = ItemType.MUSICPACK;
                    break;
                case "AthenaDance":
                    builder.Append(parts[1].StartsWith("emoji") ? "Dances/Emoji" : "Dances");
                    type = parts[1].StartsWith("emoji") ? ItemType.EMOJI : ItemType.DANCE;
                    break;
                case "AthenaSkyDiveContrail":
                    builder.Append("Contrails");
                    type = ItemType.CONTRAIL;
                    break;
                case "AthenaLoadingScreen":
                    builder.Append("LoadingScreens");
                    type = ItemType.LOADING;
                    break;
                case "Token":
                    builder.Remove(builder.Length - 10, 10)
                           .Remove(builder.Length - 13, 7)
                           .Append("Tokens");
                    type = ItemType.TOKEN;
                    break;
                default:
                    builder.Append(parts[0].Substring(6) + "s");
                    type = ItemType.UNKNOWN;
                    break;
            }
            return builder.Append($"/{parts[1]}").ToString();
        }

        static Rarity GetRarity(EnumProperty rarityTag)
        {
            Enum.TryParse<Rarity>(rarityTag.Value.String.Substring(13), true, out var ret);
            return ret;
        }

        string ConvertObjectPath(string path) => $"/FortniteGame/Content/{path.Substring(6).Split('.', 2)[0]}";
    }
}
