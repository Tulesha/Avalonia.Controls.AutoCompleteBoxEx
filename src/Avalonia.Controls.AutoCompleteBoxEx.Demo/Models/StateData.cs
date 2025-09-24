namespace Avalonia.Controls.AutoCompleteBoxEx.Demo.Models;

public class StateData
{
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }
    public string Capital { get; private set; }

    public StateData(string name, string abbreviatoin, string capital)
    {
        Name = name;
        Abbreviation = abbreviatoin;
        Capital = capital;
    }
}