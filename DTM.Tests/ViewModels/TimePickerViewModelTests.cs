using DTM.ViewModels;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels;

public class TimePickerViewModelTests
{
    [Fact]
    public void ComposeDateTime_SpecificDateAndTime()
    {
        var vm = new TimePickerViewModel
        {
            SelectedDate = new DateTime(2026, 6, 15),
            SelectedTime = new TimeSpan(14, 30, 0)
        };
        vm.ComposeDateTime().Should().Be(new DateTime(2026, 6, 15, 14, 30, 0));
    }

    [Fact]
    public void ComposeDateTime_NullDate_UsesToday()
    {
        var vm = new TimePickerViewModel
        {
            SelectedDate = null,
            SelectedTime = new TimeSpan(8, 0, 0)
        };
        vm.ComposeDateTime().Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public void ComposeDateTime_NullTime_UsesZero()
    {
        var date = new DateTime(2026, 1, 1);
        var vm = new TimePickerViewModel
        {
            SelectedDate = date,
            SelectedTime = null
        };
        vm.ComposeDateTime().Should().Be(date);
    }

    [Fact]
    public void ComposeDateTime_BothNull_ReturnsTodayMidnight()
    {
        var vm = new TimePickerViewModel
        {
            SelectedDate = null,
            SelectedTime = null
        };
        vm.ComposeDateTime().Should().Be(DateTime.Today);
    }
}
