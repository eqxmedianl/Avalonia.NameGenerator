namespace Avalonia.NameGenerator.VisualBasic.Domain;

public interface IGlobPattern
{
    bool Matches(string str);
}