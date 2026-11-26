using System.Collections.ObjectModel;
using Dapper;
using WpfMvvmTemplate.Models;

namespace WpfMvvmTemplate.Services;

/// <summary>
/// SQLite + Dapper 기반 시험 결과 저장소입니다.
/// </summary>
public sealed class SqliteResultRepository : IResultRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public SqliteResultRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<ResultRecord>> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? keyword,
        string? judgment)
    {
        using var connection = _dbFactory.CreateConnection();

        var records = (await connection.QueryAsync<RecordRow>("""
            SELECT Id, TestNumber, TestedAt, OverallResult, TesterName, CarNumber, FormationNumber, SerialNumber
            FROM TestRecords
            ORDER BY TestedAt DESC, Id DESC;
            """)).ToList();

        if (records.Count == 0)
            return [];

        var detailsLookup = (await connection.QueryAsync<DetailRow>("""
            SELECT RecordId, StepNo, ItemName, Category, StandardSpec, MeasuredValue, Result
            FROM TestResultDetails
            ORDER BY StepNo ASC;
            """))
            .GroupBy(d => d.RecordId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var trimmedKeyword = keyword?.Trim() ?? string.Empty;

        var result = new List<ResultRecord>();
        foreach (var row in records)
        {
            if (!DateTime.TryParse(row.TestedAt, out var testedAt))
                testedAt = DateTime.MinValue;

            if (startDate.HasValue && testedAt.Date < startDate.Value.Date)
                continue;

            if (endDate.HasValue && testedAt.Date > endDate.Value.Date)
                continue;

            if (!string.IsNullOrEmpty(judgment) &&
                judgment != "전체" &&
                !string.Equals(row.OverallResult, judgment, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(trimmedKeyword))
            {
                bool matches = row.TestNumber.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase)
                            || row.TesterName.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase)
                            || row.CarNumber.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase)
                            || row.FormationNumber.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase)
                            || row.SerialNumber.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase);
                if (!matches)
                    continue;
            }

            var detailItems = detailsLookup.TryGetValue(row.Id, out var detailRows)
                ? detailRows.Select(d => new ResultDetailItem
                {
                    StepNo = d.StepNo,
                    ItemName = d.ItemName,
                    Category = d.Category,
                    StandardSpec = d.StandardSpec,
                    MeasuredValue = d.MeasuredValue,
                    Result = d.Result
                })
                : [];

            result.Add(new ResultRecord
            {
                Id = row.Id,
                TestNumber = row.TestNumber,
                TestedAt = testedAt,
                OverallResult = row.OverallResult,
                TesterName = row.TesterName,
                CarNumber = row.CarNumber,
                FormationNumber = row.FormationNumber,
                SerialNumber = row.SerialNumber,
                Details = new ObservableCollection<ResultDetailItem>(detailItems)
            });
        }

        return result;
    }

    public async Task<long> SaveAsync(ResultRecord record)
    {
        using var connection = _dbFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var recordId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO TestRecords (TestNumber, TestedAt, OverallResult, TesterName, CarNumber, FormationNumber, SerialNumber)
            VALUES (@TestNumber, @TestedAt, @OverallResult, @TesterName, @CarNumber, @FormationNumber, @SerialNumber);
            SELECT last_insert_rowid();
            """,
            new
            {
                record.TestNumber,
                TestedAt = record.TestedAt.ToString("O"),
                record.OverallResult,
                record.TesterName,
                record.CarNumber,
                record.FormationNumber,
                record.SerialNumber
            },
            transaction);

        foreach (var detail in record.Details)
        {
            await connection.ExecuteAsync("""
                INSERT INTO TestResultDetails (RecordId, StepNo, ItemName, Category, StandardSpec, MeasuredValue, Result)
                VALUES (@RecordId, @StepNo, @ItemName, @Category, @StandardSpec, @MeasuredValue, @Result);
                """,
                new
                {
                    RecordId = recordId,
                    detail.StepNo,
                    detail.ItemName,
                    detail.Category,
                    detail.StandardSpec,
                    detail.MeasuredValue,
                    detail.Result
                },
                transaction);
        }

        transaction.Commit();
        record.Id = recordId;
        return recordId;
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = _dbFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM TestRecords WHERE Id = @Id;", new { Id = id });
    }

    private sealed class RecordRow
    {
        public long Id { get; init; }
        public string TestNumber { get; init; } = string.Empty;
        public string TestedAt { get; init; } = string.Empty;
        public string OverallResult { get; init; } = string.Empty;
        public string TesterName { get; init; } = string.Empty;
        public string CarNumber { get; init; } = string.Empty;
        public string FormationNumber { get; init; } = string.Empty;
        public string SerialNumber { get; init; } = string.Empty;
    }

    private sealed class DetailRow
    {
        public long RecordId { get; init; }
        public int StepNo { get; init; }
        public string ItemName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string StandardSpec { get; init; } = string.Empty;
        public string MeasuredValue { get; init; } = string.Empty;
        public string Result { get; init; } = string.Empty;
    }
}

