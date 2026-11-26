using System.IO.Ports;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Models;
using WpfMvvmTemplate.Services;
using WpfMvvmTemplate.Services.Plc;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 시스템 설정 화면의 ViewModel입니다.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly ISettingsService _settingsService;
    private readonly IPlcService _plcService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int _selectedCategoryIndex;

    [ObservableProperty]
    private bool _isPasswordEnabled;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isError;

    // PLC 통신 설정
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSerialMode))]
    private bool _isEthernetMode = true;

    public bool IsSerialMode => !IsEthernetMode;

    [ObservableProperty]
    private string _plcIpAddress = "192.168.0.10";

    [ObservableProperty]
    private string _plcPort = "2004";

    [ObservableProperty]
    private string _plcTimeoutMs = "1000";

    [ObservableProperty]
    private string _serialPortName = "COM1";

    [ObservableProperty]
    private int _baudRate = 9600;

    [ObservableProperty]
    private string _stationNo = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlcStatusMessage))]
    private string _plcStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isPlcError;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public bool HasPlcStatusMessage => !string.IsNullOrEmpty(PlcStatusMessage);

    public IReadOnlyList<string> AvailableComPorts { get; }
    public IReadOnlyList<int> BaudRateOptions { get; } = [9600, 19200, 38400, 57600, 115200];

    public bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    public string RuntimeVersion => Environment.Version.ToString();

    public SettingsViewModel(
        INavigationService navigation,
        ISettingsService settingsService,
        IPlcService plcService,
        IDialogService dialogService)
    {
        _navigation = navigation;
        _settingsService = settingsService;
        _plcService = plcService;
        _dialogService = dialogService;

        _isPasswordEnabled = _settingsService.IsPasswordEnabled;

        var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
        if (ports.Count == 0)
            ports.Add("COM1");
        AvailableComPorts = ports;

        LoadPlcSettings();
    }

    partial void OnIsPasswordEnabledChanged(bool value)
    {
        _settingsService.SetPasswordEnabled(value);
        IsError = false;
        StatusMessage = value
            ? "설정 진입 시 비밀번호 확인이 활성화되었습니다."
            : "설정 진입 시 비밀번호 확인이 비활성화되었습니다.";
    }

    [RelayCommand]
    private void SelectCategory(string indexText)
    {
        if (int.TryParse(indexText, out var index))
        {
            SelectedCategoryIndex = index;
        }
    }

    [RelayCommand]
    private void SelectEthernetMode() => IsEthernetMode = true;

    [RelayCommand]
    private void SelectSerialMode() => IsEthernetMode = false;

    [RelayCommand]
    private void ChangePassword()
    {
        if (NewPassword != ConfirmPassword)
        {
            IsError = true;
            StatusMessage = "새 비밀번호와 확인 비밀번호가 일치하지 않습니다.";
            return;
        }

        if (_settingsService.TryChangePassword(CurrentPassword, NewPassword, out var errorMessage))
        {
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            IsError = false;
            StatusMessage = "비밀번호가 성공적으로 변경되었습니다.";
            return;
        }

        IsError = true;
        StatusMessage = errorMessage;
    }

    [RelayCommand]
    private void SavePlcSettings()
    {
        if (!TryBuildPlcSettings(out var settings, out var error))
        {
            IsPlcError = true;
            PlcStatusMessage = error;
            return;
        }

        _settingsService.SavePlcSettings(settings);
        IsPlcError = false;
        PlcStatusMessage = $"PLC 통신 설정이 저장되었습니다. ({settings.SummaryText})";
    }

    [RelayCommand]
    private async Task TestPlcConnectionAsync()
    {
        if (!TryBuildPlcSettings(out var settings, out var error))
        {
            IsPlcError = true;
            PlcStatusMessage = error;
            return;
        }

        _settingsService.SavePlcSettings(settings);
        var (isOk, message) = await _plcService.CheckConnectionAsync();
        IsPlcError = !isOk;
        PlcStatusMessage = message;
    }

    [RelayCommand]
    private void OpenPlcTest()
    {
#if DEBUG
        _navigation.NavigateTo<PlcTestViewModel>();
#endif
    }

    [RelayCommand]
    private void ReturnToMain()
    {
        _navigation.NavigateTo<HomeViewModel>();
    }

    private void LoadPlcSettings()
    {
        var plc = _settingsService.PlcSettings;
        IsEthernetMode = plc.ConnectionType == PlcConnectionType.Ethernet;
        PlcIpAddress = plc.IpAddress;
        PlcPort = plc.Port.ToString();
        PlcTimeoutMs = plc.TimeoutMs.ToString();
        SerialPortName = plc.SerialPortName;
        BaudRate = plc.BaudRate;
        StationNo = plc.StationNo.ToString();
    }

    private bool TryBuildPlcSettings(out PlcConnectionSettings settings, out string error)
    {
        settings = new PlcConnectionSettings();

        if (!int.TryParse(PlcTimeoutMs, out var timeout) || timeout < 100)
        {
            error = "타임아웃은 100ms 이상의 숫자로 입력하세요.";
            return false;
        }

        if (IsEthernetMode)
        {
            if (string.IsNullOrWhiteSpace(PlcIpAddress))
            {
                error = "PLC IP 주소를 입력하세요.";
                return false;
            }

            if (!int.TryParse(PlcPort, out var port) || port is < 1 or > 65535)
            {
                error = "포트 번호는 1 ~ 65535 범위로 입력하세요.";
                return false;
            }

            settings.ConnectionType = PlcConnectionType.Ethernet;
            settings.IpAddress = PlcIpAddress.Trim();
            settings.Port = port;
            settings.TimeoutMs = timeout;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(SerialPortName))
            {
                error = "COM 포트를 선택하거나 입력하세요.";
                return false;
            }

            if (!int.TryParse(StationNo, out var station) || station < 0)
            {
                error = "국번은 0 이상의 숫자로 입력하세요.";
                return false;
            }

            settings.ConnectionType = PlcConnectionType.Serial;
            settings.SerialPortName = SerialPortName.Trim();
            settings.BaudRate = BaudRate;
            settings.StationNo = station;
            settings.TimeoutMs = timeout;
        }

        error = string.Empty;
        return true;
    }
}
