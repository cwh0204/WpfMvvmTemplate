using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfMvvmTemplate.Services;
using WpfMvvmTemplate.Services.Plc;

namespace WpfMvvmTemplate.ViewModels;

/// <summary>
/// 개발/검증 단계에서 PLC 연결·비트·워드 읽기/쓰기 및 통신 로그를 사전 테스트하는 ViewModel입니다.
/// (Release 배포 시 진입 버튼 및 DI 등록이 비활성화됩니다.)
/// </summary>
public sealed partial class PlcTestViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IPlcService _plcService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadBitCommand))]
    [NotifyCanExecuteChangedFor(nameof(WriteBitOnCommand))]
    [NotifyCanExecuteChangedFor(nameof(WriteBitOffCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadWordCommand))]
    [NotifyCanExecuteChangedFor(nameof(WriteWordCommand))]
    private bool _isConnected;

    [ObservableProperty]
    private string _bitAddress = "M0000";

    [ObservableProperty]
    private string _bitResultText = "-";

    [ObservableProperty]
    private string _wordAddress = "D0100";

    [ObservableProperty]
    private string _wordWriteValue = "0";

    [ObservableProperty]
    private string _wordResultText = "-";

    public string ConnectionSummary => _plcService.ConnectionSummary;
    public ObservableCollection<string> Logs { get; } = [];

    public PlcTestViewModel(INavigationService navigation, IPlcService plcService)
    {
        _navigation = navigation;
        _plcService = plcService;
        _isConnected = _plcService.IsConnected;

        _plcService.ConnectionStateChanged += OnConnectionStateChanged;
        _plcService.CommunicationLogged += OnCommunicationLogged;
    }

    private bool CanConnect => !IsConnected;
    private bool CanOperate => IsConnected;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        try
        {
            await _plcService.ConnectAsync();
            IsConnected = _plcService.IsConnected;
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] 연결 실패: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task DisconnectAsync()
    {
        await _plcService.DisconnectAsync();
        IsConnected = _plcService.IsConnected;
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task ReadBitAsync()
    {
        try
        {
            var value = await _plcService.ReadBitAsync(BitAddress);
            BitResultText = value ? "ON (1)" : "OFF (0)";
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Bit 읽기 실패: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task WriteBitOnAsync() => await WriteBitInternalAsync(true);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task WriteBitOffAsync() => await WriteBitInternalAsync(false);

    private async Task WriteBitInternalAsync(bool value)
    {
        try
        {
            await _plcService.WriteBitAsync(BitAddress, value);
            BitResultText = value ? "ON (1)" : "OFF (0)";
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Bit 쓰기 실패: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task ReadWordAsync()
    {
        try
        {
            var value = await _plcService.ReadWordAsync(WordAddress);
            WordResultText = $"{value} (0x{value:X4})";
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Word 읽기 실패: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task WriteWordAsync()
    {
        if (!short.TryParse(WordWriteValue, out var parsed))
        {
            AddLog($"[ERR] 올바른 정수(short) 값을 입력하세요: '{WordWriteValue}'");
            return;
        }

        try
        {
            await _plcService.WriteWordAsync(WordAddress, parsed);
            WordResultText = $"{parsed} (0x{parsed:X4})";
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Word 쓰기 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLogs() => Logs.Clear();

    [RelayCommand]
    private void BackToSettings()
    {
        _plcService.ConnectionStateChanged -= OnConnectionStateChanged;
        _plcService.CommunicationLogged -= OnCommunicationLogged;
        _navigation.NavigateTo<SettingsViewModel>();
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        Application.Current?.Dispatcher.Invoke(() => IsConnected = connected);
    }

    private void OnCommunicationLogged(object? sender, string log)
    {
        Application.Current?.Dispatcher.Invoke(() => AddLog(log));
    }

    private void AddLog(string line)
    {
        Logs.Insert(0, line);
        if (Logs.Count > 200)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }
}

