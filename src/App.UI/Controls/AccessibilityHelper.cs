using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace App.UI.Controls;

public static class AccessibilityHelper
{
    public static void SetupAccessibility(UIElement element, string name, string helpText)
    {
        AutomationProperties.SetName(element, name);
        AutomationProperties.SetHelpText(element, helpText);
        KeyboardNavigation.SetIsTabStop(element, true);
    }

    public static void AnnounceScreenReaderMessage(string message)
    {
        var peer = new NotificationAutomationPeer(message);
        peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    public static void SetAccessibleLabel(DependencyObject obj, string label)
    {
        AutomationProperties.SetName(obj, label);
    }

    public static void SetLiveRegion(FrameworkElement element, bool isLive)
    {
        AutomationProperties.SetLiveSetting(element, isLive ? AutomationLiveSetting.Polite : AutomationLiveSetting.Off);
    }
}

public class NotificationAutomationPeer : AutomationPeer
{
    private readonly string _message;

    public NotificationAutomationPeer(string message) : base()
    {
        _message = message;
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Text;
    }

    protected override string GetClassNameCore()
    {
        return "Notification";
    }

    protected override string GetNameCore()
    {
        return _message;
    }

    protected override bool IsContentElementCore()
    {
        return true;
    }

    protected override bool IsControlElementCore()
    {
        return false;
    }

    protected override string GetAcceleratorKeyCore() => string.Empty;
    protected override string GetAccessKeyCore() => string.Empty;
    protected override string GetAutomationIdCore() => "NotificationPeer";
    protected override Rect GetBoundingRectangleCore() => new Rect();
    protected override List<AutomationPeer> GetChildrenCore() => new List<AutomationPeer>();
    protected override Point GetClickablePointCore() => new Point(0, 0);
    protected override string GetHelpTextCore() => string.Empty;
    protected override string GetItemStatusCore() => string.Empty;
    protected override string GetItemTypeCore() => string.Empty;
    protected override AutomationPeer? GetLabeledByCore() => null;
    protected override AutomationOrientation GetOrientationCore() => AutomationOrientation.None;
    protected override bool HasKeyboardFocusCore() => false;
    protected override bool IsKeyboardFocusableCore() => false;
    protected override bool IsOffscreenCore() => false;
    protected override bool IsPasswordCore() => false;
    protected override bool IsRequiredForFormCore() => false;
    protected override void SetFocusCore() { }
    protected override bool IsEnabledCore() => true;

    public override object? GetPattern(PatternInterface patternInterface)
    {
        return null;
    }
}