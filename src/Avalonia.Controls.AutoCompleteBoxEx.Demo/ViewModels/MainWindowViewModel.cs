﻿using Avalonia.Controls.AutoCompleteBoxEx.Demo.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Controls.AutoCompleteBoxEx.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private StateData? _selectedState;
    [ObservableProperty] private StateData? _selectedStateEx;

    public MainWindowViewModel()
    {
        States = BuildAllStates();
    }

    public StateData[] States { get; private set; }

    private static StateData[] BuildAllStates()
    {
        return new StateData[]
        {
            new StateData("Alabama", "AL", "Montgomery"),
            new StateData("Alaska", "AK", "Juneau"),
            new StateData("Arizona", "AZ", "Phoenix"),
            new StateData("Arkansas", "AR", "Little Rock"),
            new StateData("California", "CA", "Sacramento"),
            new StateData("Colorado", "CO", "Denver"),
            new StateData("Connecticut", "CT", "Hartford"),
            new StateData("Delaware", "DE", "Dover"),
            new StateData("Florida", "FL", "Tallahassee"),
            new StateData("Georgia", "GA", "Atlanta"),
            new StateData("Hawaii", "HI", "Honolulu"),
            new StateData("Idaho", "ID", "Boise"),
            new StateData("Illinois", "IL", "Springfield"),
            new StateData("Indiana", "IN", "Indianapolis"),
            new StateData("Iowa", "IA", "Des Moines"),
            new StateData("Kansas", "KS", "Topeka"),
            new StateData("Kentucky", "KY", "Frankfort"),
            new StateData("Louisiana", "LA", "Baton Rouge"),
            new StateData("Maine", "ME", "Augusta"),
            new StateData("Maryland", "MD", "Annapolis"),
            new StateData("Massachusetts", "MA", "Boston"),
            new StateData("Michigan", "MI", "Lansing"),
            new StateData("Minnesota", "MN", "St. Paul"),
            new StateData("Mississippi", "MS", "Jackson"),
            new StateData("Missouri", "MO", "Jefferson City"),
            new StateData("Montana", "MT", "Helena"),
            new StateData("Nebraska", "NE", "Lincoln"),
            new StateData("Nevada", "NV", "Carson City"),
            new StateData("New Hampshire", "NH", "Concord"),
            new StateData("New Jersey", "NJ", "Trenton"),
            new StateData("New Mexico", "NM", "Santa Fe"),
            new StateData("New York", "NY", "Albany"),
            new StateData("North Carolina", "NC", "Raleigh"),
            new StateData("North Dakota", "ND", "Bismarck"),
            new StateData("Ohio", "OH", "Columbus"),
            new StateData("Oklahoma", "OK", "Oklahoma City"),
            new StateData("Oregon", "OR", "Salem"),
            new StateData("Pennsylvania", "PA", "Harrisburg"),
            new StateData("Rhode Island", "RI", "Providence"),
            new StateData("South Carolina", "SC", "Columbia"),
            new StateData("South Dakota", "SD", "Pierre"),
            new StateData("Tennessee", "TN", "Nashville"),
            new StateData("Texas", "TX", "Austin"),
            new StateData("Utah", "UT", "Salt Lake City"),
            new StateData("Vermont", "VT", "Montpelier"),
            new StateData("Virginia", "VA", "Richmond"),
            new StateData("Washington", "WA", "Olympia"),
            new StateData("West Virginia", "WV", "Charleston"),
            new StateData("Wisconsin", "WI", "Madison"),
            new StateData("Wyoming", "WY", "Cheyenne"),
        };
    }
}