namespace SingleFileCSharp;

using System.Reflection;

public class SfcsSolutionAttribute : SolutionAttribute
{
    public SfcsSolutionAttribute()
    {
        Build? parser = Build.Current;

        MemberInfo[] memberInfos = typeof(SolutionAttribute)
            .GetMember("_relativePath", BindingFlags.NonPublic | BindingFlags.Instance);

        if (memberInfos is not
            {
                Length: > 0,
            })
        {
            return;
        }

        MemberInfo? mi = memberInfos.FirstOrDefault(static m
            => m.MemberType is MemberTypes.Field or MemberTypes.Property
        );

        if (mi is null)
        {
            return;
        }

        var sln = (string)parser?.GetSolution();

        if (sln is null)
        {
            return;
        }

        switch (mi)
        {
            case FieldInfo fi:
                fi.SetValue(this, sln);

                break;

            case PropertyInfo pi:
                pi.SetValue(this, sln);

                break;
        }
    }
}
