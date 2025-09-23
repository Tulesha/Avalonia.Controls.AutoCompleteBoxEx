using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls.AutoCompleteBoxEx.Helpers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.AutoCompleteBoxEx.Controls;

[TemplatePart(ElementPopup, typeof(Popup))]
[TemplatePart(ElementSelector, typeof(SelectingItemsControl))]
[TemplatePart(ElementSelectionAdapter, typeof(ISelectionAdapter))]
[TemplatePart(ElementTextBox, typeof(TextBox))]
[TemplatePart(ElementInnerPanel, typeof(Panel))]
[TemplatePart(ElementToggleButton, typeof(ToggleButton))]
[PseudoClasses(":dropdownopen")]
public partial class AutoCompleteBoxEx : TemplatedControl
{
    #region Control names

    /// <summary>
    /// Specifies the name of the selection adapter TemplatePart.
    /// </summary>
    private const string ElementSelectionAdapter = "PART_SelectionAdapter";

    /// <summary>
    /// Specifies the name of the Selector TemplatePart.
    /// </summary>
    private const string ElementSelector = "PART_SelectingItemsControl";

    /// <summary>
    /// Specifies the name of the Popup TemplatePart.
    /// </summary>
    private const string ElementPopup = "PART_Popup";

    /// <summary>
    /// The name for the text box part.
    /// </summary>
    private const string ElementTextBox = "PART_TextBox";

    /// <summary>
    /// The name for the inner content panel part;
    /// </summary>
    private const string ElementInnerPanel = "PART_InnerContentPanel";

    /// <summary>
    /// The name for the toggle button part
    /// </summary>
    private const string ElementToggleButton = "PART_ToggleButton";

    #endregion

    /// <summary>
    /// Gets or sets a local cached copy of the items data.
    /// </summary>
    private List<object>? _items;

    /// <summary>
    /// Gets or sets the observable collection that contains references to
    /// all of the items in the generated view of data that is provided to
    /// the selection-style control adapter.
    /// </summary>
    private AvaloniaList<object>? _view;

    /// <summary>
    /// Gets or sets a value to ignore a number of pending change handlers.
    /// The value is decremented after each use. This is used to reset the
    /// value of properties without performing any of the actions in their
    /// change handlers.
    /// </summary>
    /// <remarks>The int is important as a value because the TextBox
    /// TextChanged event does not immediately fire, and this will allow for
    /// nested property changes to be ignored.</remarks>
    private int _ignoreTextPropertyChange;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore calling a pending
    /// change handlers.
    /// </summary>
    private bool _ignorePropertyChange;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore the selection
    /// changed event.
    /// </summary>
    private bool _ignoreTextSelectionChange;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore the focus changing, while use select an item from ListBox
    /// </summary>
    private bool _ignoreFocusChange;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore commiting value on AdapterSelection.Commit
    /// </summary>
    private bool _ignoreAdapterSelectionCommiting;

    /// <summary>
    /// Gets or sets a value indicating whether to skip the text update
    /// processing when the selected item is updated.
    /// </summary>
    private bool _skipSelectedItemTextUpdate;

    /// <summary>
    /// Gets or sets a value indicating whether the user initiated the
    /// current populate call.
    /// </summary>
    private bool _userCalledPopulate;

    /// <summary>
    /// A value indicating whether the popup has been opened at least once.
    /// </summary>
    private bool _popupHasOpened;

    /// <summary>
    /// Gets or sets the DispatcherTimer used for the MinimumPopulateDelay
    /// condition for auto completion.
    /// </summary>
    private DispatcherTimer? _delayTimer;

    /// <summary>
    /// A boolean indicating if a cancellation was requested
    /// </summary>
    private bool _cancelRequested;

    /// <summary>
    /// A boolean indicating if filtering is in action
    /// </summary>
    private bool _filterInAction;

    /// <summary>
    /// The TextBox template part.
    /// </summary>
    private TextBox? _textBox;

    private IDisposable? _textBoxSubscriptions;

    /// <summary>
    /// The SelectionAdapter.
    /// </summary>
    private ISelectionAdapter? _adapter;

    /// <summary>
    /// A weak subscription for the collection changed event.
    /// </summary>
    private IDisposable? _collectionChangeSubscription;

    private bool _isFocused;

    static AutoCompleteBoxEx()
    {
        FocusableProperty.OverrideDefaultValue<AutoCompleteBoxEx>(true);
        IsTabStopProperty.OverrideDefaultValue<AutoCompleteBoxEx>(false);

        MinimumPopulateDelayProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) =>
            x.OnMinimumPopulateDelayChanged(e));
        IsDropDownOpenProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnIsDropDownOpenChanged(e));
        SelectedItemProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnSelectedItemPropertyChanged(e));
        TextProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnTextPropertyChanged(e));
        SearchTextProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnSearchTextPropertyChanged(e));
        FilterModeProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnFilterModePropertyChanged(e));
        ItemsSourceProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnItemsSourcePropertyChanged(e));
        IsEnabledProperty.Changed.AddClassHandler<AutoCompleteBoxEx>((x, e) => x.OnControlIsEnabledChanged(e));
    }

    public AutoCompleteBoxEx()
    {
        ClearView();
    }

    /// <summary>
    /// Gets or sets the drop down popup control.
    /// </summary>
    private Popup? DropDownPopup { get; set; }

    /// <summary>
    /// Gets or sets the inner content panel control.
    /// </summary>
    private Panel? InnerContentPanel { get; set; }

    /// <summary>
    /// Gets or sets the toggle button popup control.
    /// </summary>
    private ToggleButton? ToggleButton { get; set; }

    /// <summary>
    /// Gets or sets the Text template part.
    /// </summary>
    private TextBox? TextBox
    {
        get => _textBox;
        set
        {
            _textBoxSubscriptions?.Dispose();
            _textBox = value;

            // Attach handlers
            if (_textBox != null)
            {
                _textBoxSubscriptions =
                    _textBox.GetObservable(TextBox.TextProperty)
                        .Skip(1)
                        .Subscribe(_ => OnTextBoxTextChanged());

                if (Text != null)
                {
                    UpdateTextValue(Text);
                }
            }
        }
    }

    private int TextBoxSelectionLength
    {
        get
        {
            if (TextBox != null)
            {
                return Math.Abs(TextBox.SelectionEnd - TextBox.SelectionStart);
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Gets or sets the selection adapter used to populate the drop-down
    /// with a list of selectable items.
    /// </summary>
    /// <value>The selection adapter used to populate the drop-down with a
    /// list of selectable items.</value>
    /// <remarks>
    /// You can use this property when you create an automation peer to
    /// use with AutoCompleteBoxEx or deriving from AutoCompleteBoxEx to
    /// create a custom control.
    /// </remarks>
    protected ISelectionAdapter? SelectionAdapter
    {
        get => _adapter;
        set
        {
            if (_adapter != null)
            {
                _adapter.Commit -= OnAdapterSelectionCommitComplete;
                _adapter.Cancel -= OnAdapterSelectionCanceled;
                _adapter.Cancel -= OnAdapterSelectionCancelComplete;
                _adapter.ItemsSource = null;
            }

            _adapter = value;

            if (_adapter != null)
            {
                _adapter.Commit += OnAdapterSelectionCommitComplete;
                _adapter.Cancel += OnAdapterSelectionCanceled;
                _adapter.Cancel += OnAdapterSelectionCancelComplete;
                _adapter.ItemsSource = _view;
            }
        }
    }

    /// <summary>
    /// Returns the
    /// <see cref="T:Avalonia.Controls.ISelectionAdapter" /> part, if
    /// possible.
    /// </summary>
    /// <returns>
    /// A <see cref="T:Avalonia.Controls.ISelectionAdapter" /> object,
    /// if possible. Otherwise, null.
    /// </returns>
    protected virtual ISelectionAdapter? GetSelectionAdapterPart(INameScope nameScope)
    {
        ISelectionAdapter? adapter = null;
        var selector = nameScope.Find<SelectingItemsControl>(ElementSelector);
        if (selector != null)
        {
            // Check if it is already an IItemsSelector
            adapter = selector as ISelectionAdapter;
            if (adapter == null)
            {
                // Built in support for wrapping a Selector control
                adapter = new SelectingItemsControlSelectionAdapter(selector);
            }
        }

        if (adapter == null)
        {
            adapter = nameScope.Find<ISelectionAdapter>(ElementSelectionAdapter);
        }

        return adapter;
    }

    /// <summary>
    /// Builds the visual tree for the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> control
    /// when a new template is applied.
    /// </summary>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (DropDownPopup != null)
        {
            DropDownPopup.Closed -= DropDownPopup_Closed;
            DropDownPopup = null;
        }

        if (ToggleButton != null)
        {
            ToggleButton.IsCheckedChanged -= ToggleButton_IsCheckedChanged;
            ToggleButton = null;
        }

        // Set the template parts. Individual part setters remove and add
        // any event handlers.
        var popup = e.NameScope.Find<Popup>(ElementPopup);
        if (popup != null)
        {
            DropDownPopup = popup;
            DropDownPopup.Closed += DropDownPopup_Closed;
        }

        SelectionAdapter = GetSelectionAdapterPart(e.NameScope);
        TextBox = e.NameScope.Find<TextBox>(ElementTextBox);
        InnerContentPanel = e.NameScope.Find<Panel>(ElementInnerPanel);

        var toggleButton = e.NameScope.Find<ToggleButton>(ElementToggleButton);
        if (toggleButton != null)
        {
            ToggleButton = toggleButton;
            ToggleButton.IsCheckedChanged += ToggleButton_IsCheckedChanged;
        }

        // If the drop down property indicates that the popup is open,
        // flip its value to invoke the changed handler.
        if (IsDropDownOpen && DropDownPopup != null && !DropDownPopup.IsOpen)
        {
            OpeningDropDown(false);
        }

        base.OnApplyTemplate(e);
    }

    /// <summary>
    /// Called to update the validation state for properties for which data validation is
    /// enabled.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="state">The current data binding state.</param>
    /// <param name="error">The current data binding error, if any.</param>
    protected override void UpdateDataValidation(
        AvaloniaProperty property,
        BindingValueType state,
        Exception? error)
    {
        if (property == TextProperty || property == SelectedItemProperty)
        {
            DataValidationErrors.SetError(this, error);
        }
    }

    /// <summary>
    /// Provides handling for the
    /// <see cref="E:Avalonia.InputElement.KeyDown" /> event.
    /// </summary>
    /// <param name="e">A <see cref="T:Avalonia.Input.KeyEventArgs" />
    /// that contains the event data.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _ = e ?? throw new ArgumentNullException(nameof(e));

        base.OnKeyDown(e);

        if (e.Handled || !IsEnabled)
        {
            return;
        }

        // The drop down is open, pass along the key event arguments to the
        // selection adapter. If it isn't handled by the adapter's logic,
        // then we handle some simple navigation scenarios for controlling
        // the drop down.
        if (!IsDropDownOpen)
        {
            // The drop down is not open, the Down key will toggle it open.
            // Ignore key buttons, if they are used for XY focus.
            if (e.Key == Key.Down
                && !XYFocusHelpersEx.IsAllowedXYNavigationMode(this, e.KeyDeviceType))
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
                e.Handled = true;
            }
        }

        if (SelectionAdapter != null)
        {
            SelectionAdapter.HandleKeyDown(e);
            if (e.Handled)
            {
                return;
            }
        }

        if (e.Key == Key.Escape)
        {
            OnAdapterSelectionCanceled(this, new RoutedEventArgs());
            e.Handled = true;
        }

        // Standard drop down navigation
        switch (e.Key)
        {
            case Key.Enter:
                if (IsDropDownOpen)
                {
                    OnAdapterSelectionCommitComplete(this, new RoutedEventArgs());
                    e.Handled = true;
                }

                break;
        }
    }

    /// <summary>
    /// Provides handling for the
    /// <see cref="E:Avalonia.UIElement.GotFocus" /> event.
    /// </summary>
    /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
    /// that contains the event data.</param>
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        FocusChanged(HasFocus());
    }

    /// <summary>
    /// Provides handling for the
    /// <see cref="E:Avalonia.UIElement.LostFocus" /> event.
    /// </summary>
    /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
    /// that contains the event data.</param>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        FocusChanged(HasFocus());
    }

    /// <summary>
    /// Determines whether the text box or drop-down portion of the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> control has
    /// focus.
    /// </summary>
    /// <returns>true to indicate the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> has focus;
    /// otherwise, false.</returns>
    protected bool HasFocus() => IsKeyboardFocusWithin;

    /// <summary>
    /// Handles the FocusChanged event.
    /// </summary>
    /// <param name="hasFocus">A value indicating whether the control
    /// currently has the focus.</param>
    private void FocusChanged(bool hasFocus)
    {
        // The OnGotFocus & OnLostFocus are asynchronously and cannot
        // reliably tell you that have the focus.  All they do is let you
        // know that the focus changed sometime in the past.  To determine
        // if you currently have the focus you need to do consult the
        // FocusManager (see HasFocus()).

        var wasFocused = _isFocused;
        _isFocused = hasFocus;

        if (_ignoreFocusChange)
            return;

        if (hasFocus)
        {
            if (!wasFocused && TextBoxSelectionLength <= 0)
            {
                TextBox?.Focus();
                TextBox?.SelectAll();
                if (InnerContentPanel != null)
                    InnerContentPanel.IsHitTestVisible = true;

                SetCurrentValue(IsDropDownOpenProperty, true);
            }
        }
        else
        {
            var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
            var getFocusElementMethod =
                typeof(FocusManager).GetMethod("GetFocusedElement",
                    BindingFlags.Instance | BindingFlags.Public,
                    [typeof(IFocusScope)]);
            var scope = GetFocusScope();

            // Check if we still have focus in the parent's focus scope
            if (scope != null &&
                (getFocusElementMethod?.Invoke(focusManager, [scope]) is not InputElement focused ||
                 (focused != this &&
                  focused is Visual v && !this.IsVisualAncestorOf(v))))
            {
                if (InnerContentPanel != null)
                    InnerContentPanel.IsHitTestVisible = false;

                OnAdapterSelectionComplete(false);
            }

            _userCalledPopulate = false;

            if (ContextMenu is not { IsOpen: true })
            {
                ClearTextBoxSelection();
            }
        }

        _isFocused = hasFocus;
        return;

        IFocusScope? GetFocusScope()
        {
            IInputElement? c = this;

            while (c != null)
            {
                if (c is IFocusScope scope &&
                    c is Visual v &&
                    v.GetVisualRoot() is Visual root &&
                    root.IsVisible)
                {
                    return scope;
                }

                c = (c as Visual)?.GetVisualParent<IInputElement>() ??
                    ((c as IHostedVisualTreeRoot)?.Host as IInputElement);
            }

            return null;
        }
    }

    /// <summary>
    /// Begin closing the drop-down.
    /// </summary>
    /// <param name="oldValue">The original value.</param>
    private void ClosingDropDown(bool oldValue)
    {
        var args = new CancelEventArgs();
        OnDropDownClosing(args);

        if (args.Cancel)
        {
            _ignorePropertyChange = true;
            SetCurrentValue(IsDropDownOpenProperty, oldValue);
        }
        else
        {
            CloseDropDown();
        }

        UpdatePseudoClasses();
    }

    /// <summary>
    /// Begin opening the drop down by firing cancelable events, opening the
    /// drop-down or reverting, depending on the event argument values.
    /// </summary>
    /// <param name="oldValue">The original value, if needed for a revert.</param>
    private void OpeningDropDown(bool oldValue)
    {
        var args = new CancelEventArgs();

        // Opening
        OnDropDownOpening(args);

        if (args.Cancel)
        {
            _ignorePropertyChange = true;
            SetCurrentValue(IsDropDownOpenProperty, oldValue);
        }
        else
        {
            OpenDropDown();
        }

        UpdatePseudoClasses();
    }


    /// <summary>
    /// Connects to the DropDownPopup Closed event.
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event data.</param>
    private void DropDownPopup_Closed(object? sender, EventArgs e)
    {
        // Force the drop down dependency property to be false.
        if (IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }

        // Fire the DropDownClosed event
        if (_popupHasOpened)
        {
            OnDropDownClosed(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Connects to the ToggleButton IsCheckedChanged event.
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event data.</param>
    private void ToggleButton_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ToggleButton?.IsChecked == null)
            return;

        SetValue(IsDropDownOpenProperty, ToggleButton.IsChecked.Value);
    }

    /// <summary>
    /// Handles the timer tick when using a populate delay.
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event arguments.</param>
    private void PopulateDropDown(object? sender, EventArgs e)
    {
        _delayTimer?.Stop();

        // Update the prefix/search text.
        SearchText = Text;

        // The Populated event enables advanced, custom filtering. The
        // client needs to directly update the ItemsSource collection or
        // call the Populate method on the control to continue the
        // display process if Cancel is set to true.
        var populating = new PopulatingEventArgs(SearchText);
        OnPopulating(populating);
        if (!populating.Cancel)
        {
            PopulateComplete();
        }
    }

    /// <summary>
    /// Private method that directly opens the popup, checks the expander
    /// button, and then fires the Opened event.
    /// </summary>
    private void OpenDropDown()
    {
        if (DropDownPopup != null)
        {
            DropDownPopup.IsOpen = true;
        }

        _popupHasOpened = true;
        OnDropDownOpened(EventArgs.Empty);
    }

    /// <summary>
    /// Private method that directly closes the popup, flips the Checked
    /// value, and then fires the Closed event.
    /// </summary>
    private void CloseDropDown()
    {
        if (_popupHasOpened)
        {
            if (SelectionAdapter != null)
            {
                SelectionAdapter.SelectedItem = null;
            }

            if (DropDownPopup != null)
            {
                DropDownPopup.IsOpen = false;
            }

            OnDropDownClosed(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Converts the specified object to a string by using the
    /// <see cref="P:Avalonia.Data.Binding.Converter" /> and
    /// <see cref="P:Avalonia.Data.Binding.ConverterCulture" /> values
    /// of the binding object specified by the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.ValueMemberBinding" />
    /// property.
    /// </summary>
    /// <param name="value">The object to format as a string.</param>
    /// <returns>The string representation of the specified object.</returns>
    /// <remarks>
    /// Override this method to provide a custom string conversion.
    /// </remarks>
    protected virtual string? FormatValue(object? value)
    {
        return value == null ? String.Empty : value.ToString();
    }

    /// <summary>
    /// Handle the TextChanged event that is directly attached to the
    /// TextBox part. This ensures that only user initiated actions will
    /// result in an AutoCompleteBoxEx suggestion and operation.
    /// </summary>
    private void OnTextBoxTextChanged()
    {
        //Uses Dispatcher.Post to allow the TextBox selection to update before processing
        Dispatcher.UIThread.Post(() =>
        {
            // Call the central updated text method as a user-initiated action
            TextUpdated(_textBox!.Text, true);
        });
    }

    /// <summary>
    /// Updates both the text box value and underlying text dependency
    /// property value if and when they change. Automatically fires the
    /// text changed events when there is a change.
    /// </summary>
    /// <param name="value">The new string value.</param>
    private void UpdateTextValue(string? value)
    {
        UpdateTextValue(value, null);
    }

    /// <summary>
    /// Updates both the text box value and underlying text dependency
    /// property value if and when they change. Automatically fires the
    /// text changed events when there is a change.
    /// </summary>
    /// <param name="value">The new string value.</param>
    /// <param name="userInitiated">A nullable bool value indicating whether
    /// the action was user initiated. In a user initiated mode, the
    /// underlying text dependency property is updated. In a non-user
    /// interaction, the text box value is updated. When user initiated is
    /// null, all values are updated.</param>
    private void UpdateTextValue(string? value, bool? userInitiated)
    {
        var callTextChanged = false;
        // Update the Text dependency property
        if ((userInitiated ?? true) && Text != value)
        {
            _ignoreTextPropertyChange++;
            SetCurrentValue(TextProperty, value);
            callTextChanged = true;
        }

        // Update the TextBox's Text dependency property
        if ((userInitiated == null || userInitiated == false) && TextBox != null && TextBox.Text != value)
        {
            _ignoreTextPropertyChange++;
            TextBox.Text = value ?? string.Empty;

            // Text dependency property value was set, fire event
            if (!callTextChanged && (Text == value || Text == null))
            {
                callTextChanged = true;
            }
        }

        if (callTextChanged)
        {
            OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
        }
    }

    /// <summary>
    /// Handle the update of the text for the control from any source,
    /// including the TextBox part and the Text dependency property.
    /// </summary>
    /// <param name="newText">The new text.</param>
    /// <param name="userInitiated">A value indicating whether the update
    /// is a user-initiated action. This should be a True value when the
    /// TextUpdated method is called from a TextBox event handler.</param>
    private void TextUpdated(string? newText, bool userInitiated)
    {
        // Only process this event if it is coming from someone outside
        // setting the Text dependency property directly.
        if (_ignoreTextPropertyChange > 0)
        {
            _ignoreTextPropertyChange--;
            return;
        }

        if (newText == null)
        {
            newText = string.Empty;
        }

        _userCalledPopulate = userInitiated;

        // Update the interface and values only as necessary
        UpdateTextValue(newText, userInitiated);

        _ignoreTextSelectionChange = true;

        if (_delayTimer != null)
        {
            _delayTimer.Start();
        }
        else
        {
            PopulateDropDown(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// A simple helper method to clear the view and ensure that a view
    /// object is always present and not null.
    /// </summary>
    private void ClearView()
    {
        if (_view == null)
        {
            _view = new AvaloniaList<object>();
        }
        else
        {
            _view.Clear();
        }
    }

    /// <summary>
    /// Walks through the items enumeration. Performance is not going to be
    /// perfect with the current implementation.
    /// </summary>
    private void RefreshView()
    {
        // If we have a running filter, trigger a request first
        if (_filterInAction)
        {
            _cancelRequested = true;
        }

        // Indicate that filtering is ongoing
        _filterInAction = true;

        try
        {
            if (_items == null)
            {
                ClearView();
                return;
            }

            // Cache the current text value
            var text = Text ?? string.Empty;

            // Determine if any filtering mode is on
            var stringFiltering = TextFilter != null;
            var objectFiltering = FilterMode == AutoCompleteFilterMode.Custom && TextFilter == null;

            var items = _items;

            // cache properties
            var textFilter = TextFilter;
            var newViewItems = new Collection<object>();

            // if the mode is objectFiltering, we throw an exception
            if (objectFiltering)
            {
                throw new Exception(
                    "AutoCompleteFilterMode.Custom is not supported.");
            }

            foreach (var item in items)
            {
                // Exit the fitter when requested if cancellation is requested
                if (_cancelRequested)
                {
                    return;
                }

                var inResults = !(stringFiltering || objectFiltering);

                if (!inResults)
                {
                    if (stringFiltering)
                    {
                        inResults = textFilter!(text, FormatValue(item));
                    }
                }

                if (inResults)
                {
                    newViewItems.Add(item);
                }
            }

            _view?.Clear();
            _view?.AddRange(newViewItems);
        }
        finally
        {
            // indicate that filtering is not ongoing anymore
            _filterInAction = false;
            _cancelRequested = false;
        }
    }

    /// <summary>
    /// Handle any change to the ItemsSource dependency property, update
    /// the underlying ObservableCollection view, and set the selection
    /// adapter's ItemsSource to the view if appropriate.
    /// </summary>
    /// <param name="newValue">The new enumerable reference.</param>
    private void OnItemsSourceChanged(IEnumerable? newValue)
    {
        // Remove handler for oldValue.CollectionChanged (if present)
        _collectionChangeSubscription?.Dispose();
        _collectionChangeSubscription = null;

        // Add handler for newValue.CollectionChanged (if possible)
        if (newValue is INotifyCollectionChanged newValueINotifyCollectionChanged)
        {
            _collectionChangeSubscription = newValueINotifyCollectionChanged.WeakSubscribe(ItemsCollectionChanged);
        }

        // Store a local cached copy of the data
        _items = newValue == null ? null : new List<object>(newValue.Cast<object>());

        // Clear and set the view on the selection adapter
        ClearView();
        if (SelectionAdapter != null && !Equals(SelectionAdapter.ItemsSource, _view))
        {
            SelectionAdapter.ItemsSource = _view;
        }

        if (IsDropDownOpen)
        {
            RefreshView();
        }
    }

    /// <summary>
    /// Method that handles the ObservableCollection.CollectionChanged event for the ItemsSource property.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event data.</param>
    private void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update the cache
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            for (var index = 0; index < e.OldItems.Count; index++)
            {
                _items!.RemoveAt(e.OldStartingIndex);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && _items!.Count >= e.NewStartingIndex)
        {
            for (var index = 0; index < e.NewItems.Count; index++)
            {
                _items.Insert(e.NewStartingIndex + index, e.NewItems[index]!);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null && e.OldItems != null)
        {
            for (var index = 0; index < e.NewItems.Count; index++)
            {
                _items![e.NewStartingIndex] = e.NewItems[index]!;
            }
        }

        // Update the view
        if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace) &&
            e.OldItems != null)
        {
            for (var index = 0; index < e.OldItems.Count; index++)
            {
                _view!.Remove(e.OldItems[index]!);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Significant changes to the underlying data.
            ClearView();
            if (ItemsSource != null)
            {
                _items = new List<object>(ItemsSource.Cast<object>());
            }
        }

        // Refresh the observable collection used in the selection adapter.
        RefreshView();
    }

    /// <summary>
    /// Notifies the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBoxEx" /> that the
    /// <see cref="P:Avalonia.Controls.AutoCompleteBoxEx.Items" />
    /// property has been set and the data can be filtered to provide
    /// possible matches in the drop-down.
    /// </summary>
    /// <remarks>
    /// Call this method when you are providing custom population of
    /// the drop-down portion of the AutoCompleteBoxEx, to signal the control
    /// that you are done with the population process.
    /// Typically, you use PopulateComplete when the population process
    /// is a long-running process and you want to cancel built-in filtering
    ///  of the ItemsSource items. In this case, you can handle the
    /// Populated event and set PopulatingEventArgs.Cancel to true.
    /// When the long-running process has completed you call
    /// PopulateComplete to indicate the drop-down is populated.
    /// </remarks>
    public void PopulateComplete()
    {
        // Apply the search filter
        RefreshView();

        // Fire the Populated event containing the read-only view data.
        var populated = new PopulatedEventArgs(new ReadOnlyCollection<object>(_view!));
        OnPopulated(populated);

        if (SelectionAdapter != null && !Equals(SelectionAdapter.ItemsSource, _view))
        {
            SelectionAdapter.ItemsSource = _view;
        }

        var isDropDownOpen = _userCalledPopulate && (_view!.Count > 0);
        if (isDropDownOpen != IsDropDownOpen)
        {
            _ignorePropertyChange = true;
            SetCurrentValue(IsDropDownOpenProperty, isDropDownOpen);
        }

        if (IsDropDownOpen)
        {
            OpeningDropDown(false);
        }
        else
        {
            ClosingDropDown(true);
        }

        UpdateTextCompletion();
    }

    /// <summary>
    /// Performs text completion, if enabled, and a lookup on the underlying
    /// item values for an exact match. Will update the SelectedItem value.
    /// </summary>
    private void UpdateTextCompletion()
    {
        // By default this method will clear the selected value
        object? newSelectedItem = null;
        var text = Text;

        // Text search is StartsWith explicit and only when enabled, in
        // line with WPF's ComboBox lookup. When in use it will associate
        // a Value with the Text if it is found in ItemsSource. This is
        // only valid when there is data and the user initiated the action.
        if (_view!.Count > 0)
        {
            // Perform an exact string lookup for the text. This is a
            // design change from the original Toolkit release when the
            // IsTextCompletionEnabled property behaved just like the
            // WPF ComboBox's IsTextSearchEnabled property.
            //
            // This change provides the behavior that most people expect
            // to find: a lookup for the value is always performed.
            newSelectedItem = TryGetMatch(text, _view,
                AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive));
        }

        var ignoreUpdatingAddingInnerContent = false;
        if (newSelectedItem == null)
        {
            SetIsAddingInnerContentVisible(!string.IsNullOrEmpty(text));
            ignoreUpdatingAddingInnerContent = true;
        }

        // Update the selected item property
        if (SelectedItem != newSelectedItem)
        {
            if (newSelectedItem != null)
            {
                _skipSelectedItemTextUpdate = true;
                _ignoreAdapterSelectionCommiting = true;

                SetIsAddingInnerContentVisible(false);
                SetCurrentValue(SelectedItemProperty, newSelectedItem);
            }
            else
            {
                _ignoreAdapterSelectionCommiting = false;
            }
        }
        else
        {
            if (!ignoreUpdatingAddingInnerContent)
                SetIsAddingInnerContentVisible(false);
        }

        // Restore updates for TextSelection
        if (_ignoreTextSelectionChange)
        {
            _ignoreTextSelectionChange = false;
        }
    }

    /// <summary>
    /// Sets <see cref="IsAddingInnerContentVisible"/> property
    /// </summary>
    /// <param name="value"></param>
    private void SetIsAddingInnerContentVisible(bool value)
    {
        if (IsAddingInnerContentEnabled)
        {
            IsAddingInnerContentVisible = value;
        }
    }

    /// <summary>
    /// Attempts to look through the view and locate the specific exact
    /// text match.
    /// </summary>
    /// <param name="searchText">The search text.</param>
    /// <param name="view">The view reference.</param>
    /// <param name="predicate">The predicate to use for the partial or
    /// exact match.</param>
    /// <returns>Returns the object or null.</returns>
    private object? TryGetMatch(string? searchText, AvaloniaList<object>? view,
        AutoCompleteFilterPredicate<string?>? predicate)
    {
        if (predicate is null)
            return null;

        if (view != null && view.Count > 0)
        {
            foreach (var o in view)
            {
                if (predicate(searchText, FormatValue(o)))
                {
                    return o;
                }
            }
        }

        return null;
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":dropdownopen", IsDropDownOpen);
    }

    private void ClearTextBoxSelection()
    {
        if (TextBox != null)
        {
            var length = TextBox.Text?.Length ?? 0;
            TextBox.SelectionStart = length;
            TextBox.SelectionEnd = length;
        }
    }

    /// <summary>
    /// Called when the selected item is changed, updates the text value
    /// that is displayed in the text box part.
    /// </summary>
    /// <param name="newItem">The new item.</param>
    private void OnSelectedItemChanged(object? newItem)
    {
        SetIsAddingInnerContentVisible(false);
        string? text;

        if (newItem == null)
        {
            text = SearchText;
        }
        else
        {
            text = FormatValue(newItem);
        }

        // Update the Text property and the TextBox values
        UpdateTextValue(text);

        // Move the caret to the end of the text box
        ClearTextBoxSelection();
    }

    /// <summary>
    /// Handles the Commit event on the selection adapter.
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event data.</param>
    private void OnAdapterSelectionCommitComplete(object? sender, RoutedEventArgs e)
    {
        _ignoreFocusChange = true;
        try
        {
            OnAdapterSelectionComplete(true);
        }
        finally
        {
            _ignoreFocusChange = false;
        }
    }

    /// <summary>
    /// Handles the Cancel event on the selection adapter
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event data.</param>
    private void OnAdapterSelectionCancelComplete(object? sender, RoutedEventArgs e)
    {
        OnAdapterSelectionComplete(false);
    }

    private void OnAdapterSelectionComplete(bool fromCommit)
    {
        ClearSearchTextProperty();

        if (fromCommit)
        {
            if (_adapter!.SelectedItem == null)
            {
                if (string.IsNullOrEmpty(Text))
                    SetCurrentValue(SelectedItemProperty, null);
                else
                {
                    if (_ignoreAdapterSelectionCommiting)
                    {
                        _ignoreAdapterSelectionCommiting = false;
                    }
                    else
                    {
                        OnSelectedItemChanged(SelectedItem);
                    }
                }
            }
            else
            {
                if (SelectedItem != _adapter!.SelectedItem)
                    SetCurrentValue(SelectedItemProperty, _adapter!.SelectedItem);
                else
                    OnSelectedItemChanged(SelectedItem);
            }
        }
        else
        {
            OnSelectedItemChanged(SelectedItem);
        }

        SetCurrentValue(IsDropDownOpenProperty, false);

        // Text should not be selected
        ClearTextBoxSelection();
    }

    /// <summary>
    /// Handles the Cancel event on the selection adapter.
    /// </summary>
    /// <param name="sender">The source object.</param>
    /// <param name="e">The event data.</param>
    private void OnAdapterSelectionCanceled(object? sender, RoutedEventArgs e)
    {
        UpdateTextValue(SearchText);

        // Completion will update the selected value
        UpdateTextCompletion();
    }

    #region AutoCompleteSearch

    /// <summary>
    /// A predefined set of filter functions for the known, built-in
    /// AutoCompleteFilterMode enumeration values.
    /// </summary>
    private static class AutoCompleteSearch
    {
        /// <summary>
        /// Index function that retrieves the filter for the provided
        /// AutoCompleteFilterMode.
        /// </summary>
        /// <param name="filterMode">The built-in search mode.</param>
        /// <returns>Returns the string-based comparison function.</returns>
        public static AutoCompleteFilterPredicate<string?>? GetFilter(AutoCompleteFilterMode filterMode)
        {
            switch (filterMode)
            {
                case AutoCompleteFilterMode.Contains:
                    return Contains;

                case AutoCompleteFilterMode.ContainsCaseSensitive:
                    return ContainsCaseSensitive;

                case AutoCompleteFilterMode.ContainsOrdinal:
                    return ContainsOrdinal;

                case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
                    return ContainsOrdinalCaseSensitive;

                case AutoCompleteFilterMode.Equals:
                    return Equals;

                case AutoCompleteFilterMode.EqualsCaseSensitive:
                    return EqualsCaseSensitive;

                case AutoCompleteFilterMode.EqualsOrdinal:
                    return EqualsOrdinal;

                case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
                    return EqualsOrdinalCaseSensitive;

                case AutoCompleteFilterMode.StartsWith:
                    return StartsWith;

                case AutoCompleteFilterMode.StartsWithCaseSensitive:
                    return StartsWithCaseSensitive;

                case AutoCompleteFilterMode.StartsWithOrdinal:
                    return StartsWithOrdinal;

                case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
                    return StartsWithOrdinalCaseSensitive;

                case AutoCompleteFilterMode.None:
                case AutoCompleteFilterMode.Custom:
                default:
                    return null;
            }
        }

        /// <summary>
        /// An implementation of the Contains member of string that takes in a
        /// string comparison. The traditional .NET string Contains member uses
        /// StringComparison.Ordinal.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The string value to search for.</param>
        /// <param name="comparison">The string comparison type.</param>
        /// <returns>Returns true when the substring is found.</returns>
        private static bool Contains(string? s, string? value, StringComparison comparison)
        {
            if (s is not null && value is not null)
                return s.IndexOf(value, comparison) >= 0;
            return false;
        }

        /// <summary>
        /// Check if the string value begins with the text.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool StartsWith(string? text, string? value)
        {
            if (value is not null && text is not null)
                return value.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
            return false;
        }

        /// <summary>
        /// Check if the string value begins with the text.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool StartsWithCaseSensitive(string? text, string? value)
        {
            if (value is not null && text is not null)
                return value.StartsWith(text, StringComparison.CurrentCulture);
            return false;
        }

        /// <summary>
        /// Check if the string value begins with the text.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool StartsWithOrdinal(string? text, string? value)
        {
            if (value is not null && text is not null)
                return value.StartsWith(text, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        /// <summary>
        /// Check if the string value begins with the text.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool StartsWithOrdinalCaseSensitive(string? text, string? value)
        {
            if (value is not null && text is not null)
                return value.StartsWith(text, StringComparison.Ordinal);
            return false;
        }

        /// <summary>
        /// Check if the prefix is contained in the string value. The current
        /// culture's case insensitive string comparison operator is used.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool Contains(string? text, string? value)
        {
            return Contains(value, text, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Check if the prefix is contained in the string value.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool ContainsCaseSensitive(string? text, string? value)
        {
            return Contains(value, text, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Check if the prefix is contained in the string value.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool ContainsOrdinal(string? text, string? value)
        {
            return Contains(value, text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the prefix is contained in the string value.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool ContainsOrdinalCaseSensitive(string? text, string? value)
        {
            return Contains(value, text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Check if the string values are equal.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool Equals(string? text, string? value)
        {
            return string.Equals(value, text, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Check if the string values are equal.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool EqualsCaseSensitive(string? text, string? value)
        {
            return string.Equals(value, text, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Check if the string values are equal.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool EqualsOrdinal(string? text, string? value)
        {
            return string.Equals(value, text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if the string values are equal.
        /// </summary>
        /// <param name="text">The AutoCompleteBoxEx prefix text.</param>
        /// <param name="value">The item's string value.</param>
        /// <returns>Returns true if the condition is met.</returns>
        public static bool EqualsOrdinalCaseSensitive(string? text, string? value)
        {
            return string.Equals(value, text, StringComparison.Ordinal);
        }
    }

    #endregion
}