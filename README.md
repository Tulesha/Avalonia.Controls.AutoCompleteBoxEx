# AutoCompleteBoxEx

## Description

A custom implementation of `AutoCompleteBox` from `Avalonia`

## Features (key changes)

- Remove properties with complex logic or redundant for my opinion:
    - `MinimumPrefixLengthProperty`
    - `IsTextCompletionEnabledProperty`
    - `ItemSelectorProperty`
    - `TextSelectorProperty`
    - `AsyncPopulatorProperty`
    - `InnerLeftContentProperty`
    - `InnerRightContentProperty`
- Rework logic of applying `SelectedItem` property to the control.

  **Enter cases for setting the property:**
    1. User selects an item from `Popup` using mouse or `Enter` key.
    2. User reaches the filtered item string and press `Enter` key.
    3. User reaches the filtered item string and changes focus from control. For this case
       `ResetSelectedItemOnLostFocus`
       property must be set to false, in other case property will be sets to the previous selected item.
    4. User removes filtered string and press `Enter` key. For this case `SelectedItem` will be set to null.

  If user doesn't reach this cases, `SelectedItem` property will be set's to the previous one.

  **Enter cases for resting the property:**
    1. User press `Ecs` key.
    2. User changes focus from control. See also iii case from _Enter cases for setting the property_ section

- Popup opens while user tries to focus the control
- Remove `F4` key handler
- Implement `ToggleButton` to open the drop-down
- Add `AddingInnerContent` property which is representing the content while user tries to reach the item, which isn't
  in the collection. To enable this feature you need to set `IsAddingInnerContentEnabled` property to `true`