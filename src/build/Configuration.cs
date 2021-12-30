// ReSharper disable InconsistentNaming

// ReSharper disable AnnotateNotNullTypeMember
// ReSharper disable MemberCanBePrivate.Global

namespace SingleFileCSharp;

[ TypeConverter(typeof(TypeConverter<Configuration>)) ]
sealed class Configuration : Enumeration
{
    public static Configuration Instance
    {
        get;
    } = new();

    public static Configuration Debug
    {
        get
        {
            Configuration debug = Instance;
            debug.Value = nameof(Debug);

            return _debug ??= debug;
        }
    }

    public static Configuration Release
    {
        get
        {
            Configuration release = Instance;
            release.Value = nameof(Release);

            return _release ??= release;
        }
    }

    public override bool Equals(object? obj)
        => GetHashCode()
            .Equals(obj?.GetHashCode());

    public override int GetHashCode()
        => Value.GetHashCode(Ordinal);

    public static implicit operator string(Configuration configuration)
        => configuration.Value;

    public override string ToString()
        => Value;

    Configuration()
    {
    }

    static Configuration? _debug;
    static Configuration? _release;
}
