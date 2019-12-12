using System;

namespace fnbot.shop.Backend
{
    public static class UIUtility
    {
        static bool Initialized = false;
        public static Func<string, string, ButtonType, ImageType, ResultType, ResultType> ShowDialog { get; private set; }

        public static void Initialize(Func<string, string, ButtonType, ImageType, ResultType, ResultType> showDialog)
        {
            if (Initialized)
                return;
            ShowDialog = showDialog;
            Initialized = true;
        }
    }

    public enum ButtonType
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    public enum ImageType
    {
        None = 0,
        Hand = 0x10,
        Question = 0x20,
        Exclamation = 48,
        Asterisk = 0x40,
        Stop = 0x10,
        Error = 0x10,
        Warning = 48,
        Information = 0x40
    }

    public enum ResultType
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }
}
