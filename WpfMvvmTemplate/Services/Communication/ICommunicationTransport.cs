namespace WpfMvvmTemplate.Services.Communication;

/// <summary>
/// 물리적 통신 계층(이더넷 TCP / 시리얼 RS-232·485) 공통 인터페이스입니다.
/// </summary>
public interface ICommunicationTransport : IDisposable
{
    bool IsOpen { get; }

    Task OpenAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default);
}

