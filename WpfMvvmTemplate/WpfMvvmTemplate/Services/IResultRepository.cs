using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// 시험 이력 저장 및 조회를 담당하는 저장소 인터페이스입니다.
/// </summary>
public interface IResultRepository
{
    Task<IReadOnlyList<ResultRecord>> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? keyword,
        string? judgment);

    Task<long> SaveAsync(ResultRecord record);

    Task DeleteAsync(long id);
}

