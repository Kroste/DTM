using CommunityToolkit.Mvvm.ComponentModel;

namespace DTM.ViewModels;

public sealed partial class TimePickerViewModel : ViewModelBase
{
    [ObservableProperty] private DateTimeOffset? _selectedDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan? _selectedTime = DateTime.Now.TimeOfDay;

    public DateTime ComposeDateTime()
    {
        var date = SelectedDate?.LocalDateTime.Date ?? DateTime.Today;
        var time = SelectedTime ?? TimeSpan.Zero;
        return date.Add(time);
    }
}
