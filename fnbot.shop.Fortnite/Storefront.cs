using System;
using Name = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace fnbot.shop.Fortnite
{
    public class Storefront
    {
        [Name("refreshIntervalHrs")]
        public int RefreshIntervalHrs { get; set; }
        [Name("dailyPurchaseHrs")]
        public int DailyPurchaseHrs { get; set; }
        [Name("expiration")]
        public DateTimeOffset Expiration { get; set; }
        [Name("storefronts")]
        public Store[] Storefronts { get; set; }

        public Entry[] Weekly
        {
            get
            {
                for (int i = 0; i < Storefronts.Length; i++)
                {
                    if (Storefronts[i].Name == "BRWeeklyStorefront")
                        return Storefronts[i].Entries;
                }
                return null;
            }
        }
        public Entry[] Daily
        {
            get
            {
                for (int i = 0; i < Storefronts.Length; i++)
                {
                    if (Storefronts[i].Name == "BRDailyStorefront")
                        return Storefronts[i].Entries;
                }
                return null;
            }
        }

        public class Store
        {
            [Name("name")]
            public string Name { get; set; }
            [Name("catalogEntries")]
            public Entry[] Entries { get; set; }
        }

        public class Entry
        {
            [Name("offerId")]
            public string OfferID { get; set; }
            [Name("devName")]
            public string DevName { get; set; }
            // public string[] FulfillmentIDs { get; set; } // empty from what i can see
            [Name("categories")]
            public string[] Categories { get; set; }
            [Name("offerType")]
            public string OfferType { get; set; }
            [Name("prices")]
            public Price[] Prices { get; set; }
            [Name("dynamicBundleInfo")]
            public DynamicBundleInfo BundleInfo { get; set; }
            // public string MatchFilter; always empty
            // public int FilterWeight; always 0
            [Name("dailyLimit")]
            public int DailyLimit { get; set; }
            [Name("weeklyLimit")]
            public int WeeklyLimit { get; set; }
            [Name("monthlyLimit")]
            public int MonthlyLimit { get; set; }
            [Name("refundable")]
            public bool Refundable { get; set; }
            [Name("appStoreId")]
            public string[] AppStoreID { get; set; }
            [Name("requirements")]
            public Requirement[] Requirements { get; set; }
            [Name("metaInfo")]
            public MetaEntry[] MetaInfo { get; set; }
            [Name("giftInfo")]
            public GiftInfo GiftInfo { get; set; }
            [Name("catalogGroup")]
            public string CatalogGroup { get; set; }
            [Name("catalogGroupPriority")]
            public int CatalogGroupPriority { get; set; }
            [Name("sortPriority")]
            public int SortPriority { get; set; }
            [Name("title")]
            public string Title { get; set; }
            [Name("shortDescription")]
            public string ShortDescription { get; set; }
            [Name("description")]
            public string Description { get; set; }
            [Name("displayAssetPath")]
            public string DisplayAssetPath { get; set; }
            [Name("itemGrants")]
            public ItemGrant[] ItemGrants { get; set; }
            [Name("fulfillmentClass")]
            public string FulfillmentClass { get; set; }
        }

        public struct DynamicBundleInfo
        {
            [Name("discountedBasePrice")]
            public int DiscountedBasePrice { get; set; }
            [Name("regularBasePrice")]
            public int RegularBasePrice { get; set; }
            [Name("floorPrice")]
            public int FloorPrice { get; set; }
            [Name("currencyType")]
            public string CurrencyType { get; set; }
            [Name("currencySubType")]
            public string CurrencySubType { get; set; }
            [Name("basePrice")]
            public int BasePrice { get; set; }
            [Name("displayType")]
            public string DisplayType { get; set; }
            [Name("bundleItems")]
            public BundleItem[] BundleItems { get; set; }
        }

        public struct BundleItem
        {
            [Name("bCanOwnMultiple")]
            public bool CanOwnMultiple { get; set; }
            [Name("regularPrice")]
            public int RegularPrice { get; set; }
            [Name("discountedPrice")]
            public int DiscountedPrice { get; set; }
            [Name("alreadyOwnedPriceReduction")]
            public int AlreadyOwnedPriceReduction { get; set; }
            [Name("item")]
            public ItemGrant Item { get; set; }
        }

        public struct Price
        {
            [Name("currencyType")]
            public CurrencyType CurrencyType { get; set; }
            [Name("currencySubType")]
            public string CurrencySubType { get; set; }
            [Name("basePrice")]
            public int BasePrice { get; set; }
            [Name("regularPrice")]
            public int RegularPrice { get; set; }
            [Name("finalPrice")]
            public int FinalPrice { get; set; }
            [Name("saleType")]
            public string SaleType { get; set; }
            [Name("saleExpiration")]
            public DateTimeOffset SaleExpiration { get; set; }
        }

        public struct MetaEntry
        {
            [Name("key")]
            public string Key { get; set; }
            [Name("value")]
            public string Value { get; set; }
        }

        public struct Requirement
        {
            [Name("requirementType")]
            public string RequirementType { get; set; }
            [Name("requiredId")]
            public string RequirementID { get; set; }
            [Name("minQuantity")]
            public int MinQuantity { get; set; }
        }

        public struct GiftInfo
        {
            [Name("bIsEnabled")]
            public bool Enabled { get; set; }
            [Name("forcedGiftBoxTemplateId")]
            public string ForcedGiftBoxTemplateID { get; set; }
            /*
            [Name("purchaseRequirements")]
            public Requirement[] PurchaseRequirements { get; set; } // unknown
            [Name("giftRecordIds")]
            public string[] GiftRecordIDs { get; set; } // unknown
            always empty from what i can see*/
        }

        public struct ItemGrant
        {
            [Name("templateId")]
            public string TemplateID { get; set; }
            [Name("quantity")]
            public int Quantity { get; set; }

            /* Only used in STW, no real need
            [Name("attributes")]
            public Attribs Attributes { get; set; }
            public class Attribs
            {
                [Name("Alteration")]
                public Alteration Alteration { get; set; }
            }
            public class Alteration
            {
                [Name("LootTierGroup")]
                public string LootTierGroup { get; set; }
                [Name("Tier")]
                public int Tier { get; set; }
            }
            */
        }

        public enum CurrencyType
        {
            Other,
            RealMoney,
            GameItem,
            MtxCurrency
        }
    }
}
