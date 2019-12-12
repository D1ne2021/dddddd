using System;

namespace fnbot.shop.ItemShop
{

    // pixel counts are based off of 1080p (1920x1080), which can be scaled with SKCanvas matricies
    // only the drawer should modify matricies, and only ONCE should it be modified
    static class Config
    {
        static int category;
        public static int SelectedCategory
        {
            get => category;
            set
            {
                var config = ScaledConfig.GetConfig(category = value);
                SIDE_PADDING = config.SIDE_PADDING;
                ITEM_PADDING = config.ITEM_PADDING;
                SECTION_PADDING = config.SECTION_PADDING;
                DAILY_WIDTH = config.DAILY_WIDTH;
                FEATURED_WIDTH = config.FEATURED_WIDTH;
                TOTAL_HEIGHT = config.TOTAL_HEIGHT;

                BORDER_WIDTH = config.BORDER_WIDTH;
            }
        }

        public static int SCREEN_WIDTH => 1920;
        public static int SCREEN_HEIGHT => 1080;

        // from the side of the screen
        public static int SIDE_PADDING { get; private set; }

        // between items, not including the separation between featured and daily
        public static int ITEM_PADDING { get; private set; }

        // between featured and daily sections
        public static int SECTION_PADDING { get; private set; }

        public static int DAILY_WIDTH { get; private set; }

        public static int FEATURED_WIDTH { get; private set; }

        public static int TOTAL_HEIGHT { get; private set; }

        // width of the borders for the items
        public static int BORDER_WIDTH { get; private set; }
    }

    readonly struct ScaledConfig
    {
        public readonly int TOTAL_HEIGHT;
        public readonly int SIDE_PADDING;
        public readonly int ITEM_PADDING;
        public readonly int SECTION_PADDING;
        public readonly int DAILY_WIDTH;
        public readonly int FEATURED_WIDTH;

        // make the parts that are for the item box itself a constant float based on the width or height or something
        public readonly int BORDER_WIDTH;

        static readonly ScaledConfig[] Cache = new ScaledConfig[10]; // let's hope we don't get any more than that

        static ScaledConfig()
        {
            // used for 4+ categories, might make this more readable haha
            Cache[2] = new ScaledConfig(3);
        }

        public static ScaledConfig GetConfig(int category) =>
            Cache[category - 1].TOTAL_HEIGHT == 0 ? (Cache[category - 1] = new ScaledConfig(category)) : Cache[category - 1];

        ScaledConfig(int category)
        {
            switch (category)
            {
                case 1:
                    TOTAL_HEIGHT = 608;
                    DAILY_WIDTH = 256;
                    FEATURED_WIDTH = 384;
                    SECTION_PADDING = 217;
                    ITEM_PADDING = 24;
                    break;
                case 2:
                    TOTAL_HEIGHT = 608;
                    DAILY_WIDTH = 256;
                    FEATURED_WIDTH = 384;
                    SECTION_PADDING = 48;
                    ITEM_PADDING = 24;
                    break;
                case 3:
                    TOTAL_HEIGHT = 530;
                    DAILY_WIDTH = 224;
                    FEATURED_WIDTH = 335;
                    SECTION_PADDING = 42;
                    ITEM_PADDING = 21;
                    break;
                default:
                    double height = Cache[2].TOTAL_HEIGHT;
                    double dwidth = Cache[2].DAILY_WIDTH;
                    double fwidth = Cache[2].FEATURED_WIDTH;
                    double section = Cache[2].SECTION_PADDING;
                    double item = Cache[2].ITEM_PADDING;
                    for (int i = category - 3; i > 0; i--)
                    {
                        height -= 83.5 - 34.0 * Math.Log(i);
                        dwidth -= 35.6 - 14.7 * Math.Log(i);
                        fwidth -= 52.2 - 20.8 * Math.Log(i);
                        section -= 6.68 - 2.74 * Math.Log(i);
                        item -= 3.02 - 1.05 * Math.Log(i);
                    }
                    TOTAL_HEIGHT = (int)Math.Round(height);
                    DAILY_WIDTH = (int)Math.Round(dwidth);
                    FEATURED_WIDTH = (int)Math.Round(fwidth);
                    SECTION_PADDING = (int)Math.Round(section);
                    ITEM_PADDING = (int)Math.Round(item);
                    break;
            }
            SIDE_PADDING = (int)Math.Round((1920 - (SECTION_PADDING + DAILY_WIDTH * 3 + FEATURED_WIDTH * category + ITEM_PADDING * (2 + category - 1))) / 2f);

            BORDER_WIDTH = (int)Math.Round(4.45 - 1.3 * Math.Log(category));
        }
    }
}
