# WpfMvvmTemplate

장비 시험기/제어 프로그램 개발 시 바로 재사용할 수 있도록 구성된 **WPF (.NET 10) + MVVM + DI + SQLite/Dapper** 스타터 템플릿입니다.

## 주요 기술 스택
- **MVVM**: `CommunityToolkit.Mvvm`
- **DI / Host**: `Microsoft.Extensions.Hosting`
- **XAML Behaviors**: `Microsoft.Xaml.Behaviors.Wpf`
- **Database**: `Microsoft.Data.Sqlite` + `Dapper` (실행 폴더의 `app.db` 자동 생성)

## 프로젝트 구조
- `Models/` — 공통 모델 (`MenuCardItem`, `DiagnosisItem`, `ResultRecord`, `TestModels`, `AppSettings`)
- `Services/` — `NavigationService`(화면/모달), `DialogService`, `SqliteDbContext`, `SqliteResultRepository`, `SettingsService`
- `ViewModels/` — `MainViewModel`, `HomeViewModel`, `TestViewModel`, `SearchResultViewModel`, `SelfDiagnosisViewModel`, `SettingsViewModel`, `PasswordPromptViewModel`, `ConfirmDialogViewModel`
- `Views/` — 각 ViewModel에 대응하는 XAML 화면 (`App.xaml`의 `DataTemplate`으로 매핑)
- `Resources/` — `Colors.xaml`, `Icons.xaml`, `Styles.xaml`, `Images/`

## 새 시험기 프로젝트 적용 체크리스트
1. **타이틀 / 브랜드 변경**
   - `ViewModels/MainViewModel.cs`: `WindowTitle` 변경
   - `ViewModels/HomeViewModel.cs`: `Title`, `Subtitle` 변경
   - `Resources/Images/`: `background-img.jpg`, `Logo-img.png` 교체
2. **시험 항목 및 계측 화면 커스터마이징**
   - `ViewModels/TestViewModel.cs`: `InitializeSampleTestGroups()` 및 `StartAutoTestAsync()` 내부 장비 제어 로직 교체
   - `Views/TestView.xaml`: 상단 실시간 센서 카드 영역과 매뉴얼 시험 슬롯(`IsManualMode`)에 장비 전용 UI 배치
3. **자가 진단 항목 추가**
   - `ViewModels/SelfDiagnosisViewModel.cs`: `Items` 컬렉션에 점검 항목(`DiagnosisItem`) 추가
4. **설정 화면 비밀번호**
   - 초기 비밀번호는 `0000`이며 `app.db`의 `AppSettings` 테이블에 SHA-256 해시로 저장됩니다.

