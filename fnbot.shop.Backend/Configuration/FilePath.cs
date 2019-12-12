namespace fnbot.shop.Backend.Configuration
{
    public readonly struct FilePath
    {
        public string Path { get; }
        public string Filter { get; }
        public FilePath(string filter, string path)
        {
            Filter = filter;
            Path = path;
        }

        public FilePath ChangePath(string path) => new FilePath(Filter, path);

        public override string ToString() => Path;
    }
}
