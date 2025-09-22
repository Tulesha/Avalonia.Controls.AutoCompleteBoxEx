using System.ComponentModel;
using Avalonia.Interactivity;

namespace Avalonia.Controls.AutoCompleteBoxEx.Controls;

public partial class AutoCompleteBoxEx
{
    #region SelectionChanged Event

    /// <summary>
    /// Defines the <see cref="SelectionChanged"/> event.
    /// </summary>
    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<SelectionChangedEventArgs>(
            nameof(SelectionChanged),
            RoutingStrategies.Bubble,
            typeof(AutoCompleteBoxEx));

    /// <summary>
    /// Occurs when the selected item in the drop-down portion of the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> has
    /// changed.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.SelectionChanged" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:Avalonia.Controls.SelectionChangedEventArgs" />
    /// that contains the event data.</param>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        RaiseEvent(e);
    }

    #endregion

    #region TextChanged Event

    /// <summary>
    /// Defines the <see cref="TextChanged"/> event.
    /// </summary>
    public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<AutoCompleteBoxEx, TextChangedEventArgs>(
            nameof(TextChanged),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Occurs asynchronously when the text in the <see cref="TextBox"/> portion of the
    /// <see cref="AutoCompleteBoxEx" /> changes.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged
    {
        add => AddHandler(TextChangedEvent, value);
        remove => RemoveHandler(TextChangedEvent, value);
    }

    /// <summary>
    /// Raises the <see cref="TextChanged" /> event.
    /// </summary>
    /// <param name="e">A <see cref="TextChangedEventArgs" /> that contains the event data.</param>
    protected virtual void OnTextChanged(TextChangedEventArgs e)
    {
        RaiseEvent(e);
    }

    #endregion

    #region Populating event

    /// <summary>
    /// Occurs when the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> is
    /// populating the drop-down with possible matches based on the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.Text" />
    /// property.
    /// </summary>
    /// <remarks>
    /// If the event is canceled, by setting the PopulatingEventArgs.Cancel
    /// property to true, the AutoCompleteBoxEx will not automatically
    /// populate the selection adapter contained in the drop-down.
    /// In this case, if you want possible matches to appear, you must
    /// provide the logic for populating the selection adapter.
    /// </remarks>
    public event EventHandler<PopulatingEventArgs>? Populating;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.Populating" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:Avalonia.Controls.PopulatingEventArgs" /> that
    /// contains the event data.</param>
    protected virtual void OnPopulating(PopulatingEventArgs e)
    {
        Populating?.Invoke(this, e);
    }

    #endregion

    #region Populated Event

    /// <summary>
    /// Occurs when the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> has
    /// populated the drop-down with possible matches based on the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.Text" />
    /// property.
    /// </summary>
    public event EventHandler<PopulatedEventArgs>? Populated;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.Populated" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:Avalonia.Controls.PopulatedEventArgs" />
    /// that contains the event data.</param>
    protected virtual void OnPopulated(PopulatedEventArgs e)
    {
        Populated?.Invoke(this, e);
    }

    #endregion

    #region DropDownOpening Event

    /// <summary>
    /// Occurs when the value of the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.IsDropDownOpen" />
    /// property is changing from false to true.
    /// </summary>
    public event EventHandler<CancelEventArgs>? DropDownOpening;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.DropDownOpening" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
    /// that contains the event data.</param>
    protected virtual void OnDropDownOpening(CancelEventArgs e)
    {
        DropDownOpening?.Invoke(this, e);
    }

    #endregion

    #region DropDownOpened Event

    /// <summary>
    /// Occurs when the value of the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.IsDropDownOpen" />
    /// property has changed from false to true and the drop-down is open.
    /// </summary>
    public event EventHandler? DropDownOpened;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.DropDownOpened" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:System.EventArgs" />
    /// that contains the event data.</param>
    protected virtual void OnDropDownOpened(EventArgs e)
    {
        DropDownOpened?.Invoke(this, e);
    }

    #endregion

    #region DropDownClosing Event

    /// <summary>
    /// Occurs when the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.IsDropDownOpen" />
    /// property is changing from true to false.
    /// </summary>
    public event EventHandler<CancelEventArgs>? DropDownClosing;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.DropDownClosing" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
    /// that contains the event data.</param>
    protected virtual void OnDropDownClosing(CancelEventArgs e)
    {
        DropDownClosing?.Invoke(this, e);
    }

    #endregion

    #region DropDownClosed Event

    /// <summary>
    /// Occurs when the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.IsDropDownOpen" />
    /// property was changed from true to false and the drop-down is open.
    /// </summary>
    public event EventHandler? DropDownClosed;

    /// <summary>
    /// Raises the
    /// <see cref="E:Avalonia.Controls.AutoCompleteBoxEx.DropDownClosed" />
    /// event.
    /// </summary>
    /// <param name="e">A
    /// <see cref="T:System.EventArgs" />
    /// which contains the event data.</param>
    protected virtual void OnDropDownClosed(EventArgs e)
    {
        DropDownClosed?.Invoke(this, e);
    }

    #endregion
}