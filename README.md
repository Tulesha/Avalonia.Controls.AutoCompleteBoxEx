# AutoCompleteBoxEx

## Description

A custom implementation of AutoCompleteBox from avalonia

## Features (key changes)

- Remove properties with complex logic or redundant for my opinion:
    - `MinimumPrefixLengthProperty`
    - `IsTextCompletionEnabledProperty`
    - `ItemSelectorProperty`
    - `TextSelectorProperty`
    - `AsyncPopulatorProperty`
    - `InnerLeftContentProperty`
    - `InnerRightContentProperty`
    - `ValueMemberBindingProperty`
- Rework logic of applying `SelectedItem` property to the control. Know `SelectedItem` will be set only after hitting
  the `Enter` key, or reaching the filtered item string. If a user hits the `Escape` key or trying to hit `Enter` key,
  while `Text` property is not null or empty, `SelectedItem` will be set to the previous one. User can set
  `SelectedItem` to the null if he clears `TextBox` and hit an `Enter` key.
- Popup opens while user tries to focus the control
- Remove `F4` key handler
- Change styles to be closer to the `ComboBox`