using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

public sealed record WipeMethodOption(WipeMethod Method, string Label, string Description)
{
    public override string ToString() => Label;
}
