using System.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.Controls.AutoCompleteBoxEx.Controls;

public partial class AutoCompleteBoxEx
{
    #region CaterIndex Property

    /// <summary>
    /// Defines see <see cref="Avalonia.Controls.TextBox.CaretIndex"/> property.
    /// </summary>
    public static readonly StyledProperty<int> CaretIndexProperty =
        TextBox.CaretIndexProperty.AddOwner<AutoCompleteBoxEx>(new(
            defaultValue: 0,
            defaultBindingMode: BindingMode.TwoWay));

    /// <summary>
    /// Gets or sets the caret index
    /// </summary>
    public int CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, value);
    }

    #endregion

    #region Watermark Property

    /// <summary>
    /// Defines see <see cref="TextBox.Watermark"/> property.>
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<AutoCompleteBoxEx>();

    /// <summary>
    /// Gets or set the watermark
    /// </summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    #endregion

    #region MinimumPopulateDelay Property

    /// <summary>
    /// Identifies the <see cref="MinimumPopulateDelay" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="MinimumPopulateDelay" /> property.</value>
    public static readonly StyledProperty<TimeSpan> MinimumPopulateDelayProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, TimeSpan>(
            nameof(MinimumPopulateDelay),
            TimeSpan.Zero,
            validate: IsValidMinimumPopulateDelay);

    /// <summary>
    /// Gets or sets the minimum delay, after text is typed
    /// in the text box before the
    /// <see cref="AutoCompleteBoxEx" /> control
    /// populates the list of possible matches in the drop-down.
    /// </summary>
    /// <value>The minimum delay, after text is typed in
    /// the text box, but before the
    /// <see cref="AutoCompleteBoxEx" /> populates
    /// the list of possible matches in the drop-down. The default is 0.</value>
    public TimeSpan MinimumPopulateDelay
    {
        get => GetValue(MinimumPopulateDelayProperty);
        set => SetValue(MinimumPopulateDelayProperty, value);
    }

    private static bool IsValidMinimumPopulateDelay(TimeSpan value) => value.TotalMilliseconds >= 0.0;

    /// <summary>
    /// MinimumPopulateDelayProperty property changed handler. Any current
    /// dispatcher timer will be stopped. The timer will not be restarted
    /// until the next TextUpdate call by the user.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnMinimumPopulateDelayChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newValue = (TimeSpan)e.NewValue!;

        // Stop any existing timer
        if (_delayTimer != null)
        {
            _delayTimer.Stop();

            if (newValue == TimeSpan.Zero)
            {
                _delayTimer.Tick -= PopulateDropDown;
                _delayTimer = null;
            }
        }

        if (newValue > TimeSpan.Zero)
        {
            // Create or clear a dispatcher timer instance
            if (_delayTimer == null)
            {
                _delayTimer = new DispatcherTimer();
                _delayTimer.Tick += PopulateDropDown;
            }

            // Set the new tick interval
            _delayTimer.Interval = newValue;
        }
    }

    #endregion

    #region MaxDropDownHeight Property

    /// <summary>
    /// Identifies the <see cref="MaxDropDownHeight" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="MaxDropDownHeight" /> property.</value>
    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, double>(
            nameof(MaxDropDownHeight),
            double.PositiveInfinity,
            validate: IsValidMaxDropDownHeight);

    /// <summary>
    /// Gets or sets the maximum height of the drop-down portion of the
    /// <see cref="AutoCompleteBoxEx" /> control.
    /// </summary>
    /// <value>The maximum height of the drop-down portion of the
    /// <see cref="AutoCompleteBoxEx" /> control.
    /// The default is <see cref="F:System.Double.PositiveInfinity" />.</value>
    /// <exception cref="T:System.ArgumentException">The specified value is less than 0.</exception>
    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    private static bool IsValidMaxDropDownHeight(double value) => value >= 0.0;

    #endregion

    #region ItemTemplate Property

    /// <summary>
    /// Identifies the <see cref="ItemTemplate" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="ItemTemplate" /> property.</value>
    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, IDataTemplate>(
            nameof(ItemTemplate));

    /// <summary>
    /// Gets or sets the <see cref="T:Avalonia.DataTemplate" /> used
    /// to display each item in the drop-down portion of the control.
    /// </summary>
    /// <value>The <see cref="T:Avalonia.DataTemplate" /> used to
    /// display each item in the drop-down. The default is null.</value>
    /// <remarks>
    /// You use the ItemTemplate property to specify the visualization
    /// of the data objects in the drop-down portion of the AutoCompleteBoxEx
    /// control. If your AutoCompleteBoxEx is bound to a collection and you
    /// do not provide specific display instructions by using a
    /// DataTemplate, the resulting UI of each item is a string
    /// representation of each object in the underlying collection.
    /// </remarks>
    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    #endregion

    #region IsDropDownOpen Property

    /// <summary>
    /// Identifies the <see cref="IsDropDownOpen" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="IsDropDownOpen" /> property.</value>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, bool>(
            nameof(IsDropDownOpen));

    /// <summary>
    /// Gets or sets a value indicating whether the drop-down portion of
    /// the control is open.
    /// </summary>
    /// <value>
    /// True if the drop-down is open; otherwise, false. The default is
    /// false.
    /// </value>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>
    /// IsDropDownOpenProperty property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnIsDropDownOpenChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var oldValue = (bool)e.OldValue!;
        var newValue = (bool)e.NewValue!;

        if (ToggleButton != null)
            ToggleButton.IsChecked = newValue;

        // Ignore the change if requested
        if (_ignorePropertyChange)
        {
            _ignorePropertyChange = false;
            return;
        }

        if (newValue)
        {
            TextUpdated(Text, true);
        }
        else
        {
            ClosingDropDown(oldValue);
        }

        UpdatePseudoClasses();
    }

    #endregion

    #region SelectedItem Property

    /// <summary>
    /// Identifies the <see cref="SelectedItem" /> property.
    /// </summary>
    /// <value>The identifier the <see cref="SelectedItem" /> property.</value>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, object?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    /// <summary>
    /// Gets or sets the selected item in the drop-down.
    /// </summary>
    /// <value>The selected item in the drop-down.</value>
    /// <remarks>
    /// If the IsTextCompletionEnabled property is true and text typed by
    /// the user matches an item in the ItemsSource collection, which is
    /// then displayed in the text box, the SelectedItem property will be
    /// a null reference.
    /// </remarks>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// SelectedItem property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnSelectedItemPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_ignorePropertyChange)
        {
            _ignorePropertyChange = false;
            return;
        }

        // Update the text display
        if (_skipSelectedItemTextUpdate)
        {
            _skipSelectedItemTextUpdate = false;
        }
        else
        {
            OnSelectedItemChanged(e.NewValue);
        }

        // Fire the SelectionChanged event
        var removed = new List<object>();
        if (e.OldValue != null)
        {
            removed.Add(e.OldValue);
        }

        var added = new List<object>();
        if (e.NewValue != null)
        {
            added.Add(e.NewValue);
        }

        OnSelectionChanged(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
    }

    #endregion

    #region Text Property

    /// <summary>
    /// Identifies the <see cref="Text" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="Text" /> property.</value>
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<AutoCompleteBoxEx>(new(string.Empty,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true));

    /// <summary>
    /// Gets or sets the text in the text box portion of the
    /// <see cref="AutoCompleteBoxEx" /> control.
    /// </summary>
    /// <value>The text in the text box portion of the
    /// <see cref="AutoCompleteBoxEx" /> control.</value>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// TextProperty property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        TextUpdated((string?)e.NewValue, false);
    }

    #endregion

    #region SearchText Property

    private string? _searchText = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a read-only dependency
    /// property change handler should allow the value to be set.  This is
    /// used to ensure that read-only properties cannot be changed via
    /// SetValue, etc.
    /// </summary>
    private bool _allowWrite;

    /// <summary>
    /// Identifies the <see cref="SearchText" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="SearchText" /> property.</value>
    public static readonly DirectProperty<AutoCompleteBoxEx, string?> SearchTextProperty =
        AvaloniaProperty.RegisterDirect<AutoCompleteBoxEx, string?>(
            nameof(SearchText),
            o => o.SearchText,
            unsetValue: string.Empty);

    /// <summary>
    /// Gets the text that is used to filter items in the
    /// <see cref="ItemsSource" /> item collection.
    /// </summary>
    /// <value>The text that is used to filter items in the
    /// <see cref="ItemsSource" /> item collection.</value>
    /// <remarks>
    /// The SearchText value is typically the same as the
    /// Text property, but is set after the TextChanged event occurs
    /// and before the Populating event.
    /// </remarks>
    public string? SearchText
    {
        get => _searchText;
        private set
        {
            try
            {
                _allowWrite = true;
                SetAndRaise(SearchTextProperty, ref _searchText, value);
            }
            finally
            {
                _allowWrite = false;
            }
        }
    }

    /// <summary>
    /// Clears the search text property.
    /// </summary>
    private void ClearSearchTextProperty()
    {
        SearchText = string.Empty;
    }

    /// <summary>
    /// SearchText property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void OnSearchTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_ignorePropertyChange)
        {
            _ignorePropertyChange = false;
            return;
        }

        // Ensure the property is only written when expected
        if (!_allowWrite)
        {
            // Reset the old value before it was incorrectly written
            _ignorePropertyChange = true;
            SetCurrentValue(e.Property, e.OldValue);

            throw new InvalidOperationException("Cannot set read-only property SearchText.");
        }
    }

    #endregion

    #region FilterMode Property

    /// <summary>
    /// Gets the identifier for the <see cref="FilterMode" /> property.
    /// </summary>
    public static readonly StyledProperty<AutoCompleteFilterMode> FilterModeProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, AutoCompleteFilterMode>(
            nameof(FilterMode),
            defaultValue: AutoCompleteFilterMode.StartsWith,
            validate: IsValidFilterMode);

    /// <summary>
    /// Gets or sets how the text in the text box is used to filter items
    /// specified by the <see cref="ItemsSource" />
    /// property for display in the drop-down.
    /// </summary>
    /// <value>One of the <see cref="AutoCompleteFilterMode" />
    /// values The default is <see cref="AutoCompleteFilterMode.StartsWith" />.</value>
    /// <exception cref="T:System.ArgumentException">The specified value is not a valid
    /// <see cref="AutoCompleteFilterMode" />.</exception>
    /// <remarks>
    /// Use the FilterMode property to specify how possible matches are
    /// filtered. For example, possible matches can be filtered in a
    /// predefined or custom way.
    /// </remarks>
    public AutoCompleteFilterMode FilterMode
    {
        get => GetValue(FilterModeProperty);
        set => SetValue(FilterModeProperty, value);
    }

    private static bool IsValidFilterMode(AutoCompleteFilterMode mode)
    {
        switch (mode)
        {
            case AutoCompleteFilterMode.None:
            case AutoCompleteFilterMode.StartsWith:
            case AutoCompleteFilterMode.StartsWithCaseSensitive:
            case AutoCompleteFilterMode.StartsWithOrdinal:
            case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
            case AutoCompleteFilterMode.Contains:
            case AutoCompleteFilterMode.ContainsCaseSensitive:
            case AutoCompleteFilterMode.ContainsOrdinal:
            case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
            case AutoCompleteFilterMode.Equals:
            case AutoCompleteFilterMode.EqualsCaseSensitive:
            case AutoCompleteFilterMode.EqualsOrdinal:
            case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
            case AutoCompleteFilterMode.Custom:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// FilterModeProperty property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnFilterModePropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var mode = (AutoCompleteFilterMode)e.NewValue!;

        // Sets the filter predicate for the new value
        SetCurrentValue(TextFilterProperty, AutoCompleteSearch.GetFilter(mode));
    }

    #endregion

    #region TextFilter Property

    /// <summary>
    /// Identifies the <see cref="TextFilter" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="TextFilter" /> property.</value>
    public static readonly StyledProperty<AutoCompleteFilterPredicate<string?>?> TextFilterProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, AutoCompleteFilterPredicate<string?>?>(
            nameof(TextFilter),
            defaultValue: AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

    /// <summary>
    /// Gets or sets the custom method that uses the user-entered text to
    /// filter items specified by the <see cref="ItemsSource" />
    /// property in a text-based way for display in the drop-down.
    /// </summary>
    /// <value>The custom method that uses the user-entered text to filter
    /// items specified by the <see cref="ItemsSource" />
    /// property in a text-based way for display in the drop-down.</value>
    /// <remarks>
    /// The search mode is automatically set to Custom if you set the
    /// TextFilter property.
    /// </remarks>
    public AutoCompleteFilterPredicate<string?>? TextFilter
    {
        get => GetValue(TextFilterProperty);
        set => SetValue(TextFilterProperty, value);
    }

    #endregion

    #region ItemsSource Property

    /// <summary>
    /// Identifies the <see cref="ItemsSource" /> property.
    /// </summary>
    /// <value>The identifier for the <see cref="ItemsSource" /> property.</value>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, IEnumerable?>(
            nameof(ItemsSource));

    /// <summary>
    /// Gets or sets a collection that is used to generate the items for the
    /// drop-down portion of the <see cref="AutoCompleteBoxEx" /> control.
    /// </summary>
    /// <value>The collection that is used to generate the items of the
    /// drop-down portion of the <see cref="AutoCompleteBoxEx" /> control.</value>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// ItemsSourceProperty property changed handler.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    private void OnItemsSourcePropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        OnItemsSourceChanged((IEnumerable?)e.NewValue);
    }

    #endregion

    #region MaxLength Property

    /// <summary>
    /// Defines the <see cref="MaxLength"/> property
    /// </summary>
    public static readonly StyledProperty<int> MaxLengthProperty =
        TextBox.MaxLengthProperty.AddOwner<AutoCompleteBoxEx>();

    /// <summary>
    /// Gets or sets the maximum number of characters that the <see cref="AutoCompleteBoxEx"/> can accept.
    /// This constraint only applies for manually entered (user-inputted) text.
    /// </summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    #endregion

    #region IsEnabled Property

    /// <summary>
    /// Handle the change of the IsEnabled property.
    /// </summary>
    /// <param name="e">The event data.</param>
    private void OnControlIsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var isEnabled = (bool)e.NewValue!;
        if (!isEnabled)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }
    }

    #endregion

    #region AddingInnerContent Property

    /// <summary>
    /// Defines the <see cref="AddingInnerContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> AddingInnerContentProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, object?>(
            nameof(AddingInnerContent));

    /// <summary>
    /// Gets or sets the content which display while user tries to write not existed item from a collection.
    /// </summary>
    public object? AddingInnerContent
    {
        get => GetValue(AddingInnerContentProperty);
        set => SetValue(AddingInnerContentProperty, value);
    }

    #endregion

    #region IsAddingInnerContentEnabled Property

    /// <summary>
    /// Defines the <see cref="IsAddingInnerContentEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsAddingInnerContentEnabledProperty =
        AvaloniaProperty.Register<AutoCompleteBoxEx, bool>(
            nameof(IsAddingInnerContentEnabled));

    /// <summary>
    /// Gets or sets if <see cref="AddingInnerContent"/> is enabled.
    /// </summary>
    public bool IsAddingInnerContentEnabled
    {
        get => GetValue(IsAddingInnerContentEnabledProperty);
        set => SetValue(IsAddingInnerContentEnabledProperty, value);
    }

    #endregion

    #region IsAddingInnerContentVisible Property

    private bool _isAddingInnerContentVisible;

    /// <summary>
    /// Defines the <see cref="IsAddingInnerContentVisible"/> property.
    /// </summary>
    public static readonly DirectProperty<AutoCompleteBoxEx, bool> IsAddingInnerContentVisibleProperty =
        AvaloniaProperty.RegisterDirect<AutoCompleteBoxEx, bool>(
            nameof(IsAddingInnerContentVisible),
            o => o.IsAddingInnerContentVisible,
            (o, v) => o.IsAddingInnerContentVisible = v,
            unsetValue: false);

    /// <summary>
    /// Gets or sets if <see cref="AddingInnerContent"/> is visible.
    /// </summary>
    public bool IsAddingInnerContentVisible
    {
        get => _isAddingInnerContentVisible;
        private set => SetAndRaise(IsAddingInnerContentVisibleProperty, ref _isAddingInnerContentVisible, value);
    }

    #endregion
}