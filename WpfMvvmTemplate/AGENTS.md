# Project Guidelines (WPF MVVM Template)

이 프로젝트는 **장비 시험기/제어 프로그램** 개발을 위한 WPF (.NET 10) MVVM 스타터 템플릿을 기반으로 합니다.

## 1. 아키텍처 및 기술 스택
- **MVVM**: `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` 소스 제너레이터 사용)
- **DI / 수명 주기**: `Microsoft.Extensions.Hosting` (`App.xaml.cs`에 서비스·ViewModel·View 등록)
- **XAML Behaviors**: `Microsoft.Xaml.Behaviors.Wpf` (코드비하인드 최소화, `*.xaml.cs`에 비즈니스 로직 작성 금지)
- **Database**: `Microsoft.Data.Sqlite` + `Dapper` (`IDbConnectionFactory` / `SqliteDbContext`가 실행 폴더의 `app.db` 초기화, `IResultRepository`로 이력 저장·조회)

## 2. 핵심 서비스 및 화면 전환 규칙
- **화면 및 모달 전환**: 반드시 `INavigationService` 사용
  - 전체 화면 전환: `NavigateTo<TViewModel>()`
  - 모달 오버레이: `OpenModal<TViewModel>()` / `CloseModal()`
  - 새 View 추가 시 `App.xaml`에 `<DataTemplate DataType="{x:Type vm:...}">` 매핑과 `App.xaml.cs` DI 등록 필수
- **알림 / 확인 창**: 기본 `MessageBox.Show()` 대신 `IDialogService` (`ShowAlertAsync`, `ShowConfirmAsync`) 사용
- **비밀번호 입력 바인딩**: `PasswordBox`에는 `Helpers/PasswordBoxHelper.BoundPassword` 첨부 속성 사용
- **설정 및 보안 관리**: `ISettingsService` (`AppSettings` 테이블 연동, 초기 비밀번호 `0000` SHA-256 해시). `PasswordPromptView` 등 인증 UI에는 초기 비밀번호 힌트를 노출하지 않음

## 3. PLC 통신 및 개발 전용 화면 규칙
- **2계층 구조 분리**:
  - 전송 계층(`Services/Communication/ICommunicationTransport`): `TcpTransport`(이더넷 TCP), `SerialTransport`(RS-232 / RS-485)
  - 프로토콜 계층(`Services/Plc/IPlcService`): 기본 구현체로 LS산전 XGT(`LsXgtPlcService`, 이더넷 FEnet / 시리얼 Cnet) 사용, 필요 시 Modbus 등 다른 프로토콜 구현체로 DI 교체
- **개발 전용 사전 테스트 화면(`PlcTestView`)**:
  - `PlcTestViewModel` DI 등록과 `SettingsView`의 진입 버튼은 반드시 `#if DEBUG`로 제한해 `Release` 배포본에서 노출되지 않도록 유지

## 4. 시험 및 결과 조회 필수 공통 항목
모든 시험기 프로그램의 시험 입력(`TestViewModel`) 및 결과 조회(`ResultRecord`, `SearchResultView`)에는 다음 공통 필드를 필수로 유지합니다:
- `TestNumber` (시험 번호)
- `TestedAt` (시험 일자)
- `PassedCount` / `ItemCount` (합격 항목)
- `OverallResult` (종합 판정: `OK` / `NG`)
- `TesterName` (시험자)
- `CarNumber` (차량번호)
- `FormationNumber` (편성번호)
- `SerialNumber` (시리얼넘버)

## 5. UI, 해상도 및 리소스 규칙
- **타겟 해상도**: 제어기 PC(`1280×1024`, 5:4)와 일반 모니터(`1920×1080`, 16:9) 모두에서 비율이 깨지지 않도록 적응형 레이아웃(비율 `Grid`, `Viewbox`) 유지
- **전체 톤**: 라이트 톤(`WindowBackgroundBrush` `#F4F6F9`, 풀폭 `58px` 상단 헤더 바, `1px` 보더 중심의 절제된 그림자)
- **아이콘**: `Resources/Icons.xaml`의 `24×24` 규격 `1.75px` 선형(Thin-Stroke) 벡터 아이콘 사용 (`Viewbox`로 크게 확대할 때는 `StrokeThickness`를 비례 축소)
- **색상/스타일/컨버터**: `Resources/Colors.xaml`, `Resources/Styles.xaml`, `App.xaml` 공용 컨버터(`BoolToVis`, `InverseBool`) 재사용
- **이미지 리소스**: `Resources/Images/` 폴더에 배치 (`.csproj`의 `<Resource Include="Resources\Images\*" />`로 자동 포함)

## 6. AI 개발 품질 및 안전 규칙
- **비동기 및 장비 안전**: UI 스레드에서 `Thread.Sleep`, `.Result`, `.Wait()` 사용 금지. 모든 대기·통신은 `async/await` + `CancellationToken`으로 처리하며, 시험 중단·예외 발생 시 `finally` 블록에서 장비 출력을 안전 상태(OFF/초기화)로 복구
- **양면 빌드 검증**: 코드나 XAML 수정 후에는 반드시 `Debug`와 `Release` 빌드를 모두 실행해 경고·오류 `0`개를 확인 (`#if DEBUG` 분기로 인한 Release 빌드 오류 방지)
- **설계 우선 원칙**: 새 화면 추가, DB 스키마 변경, PLC 프로토콜 구현 등 구조 변경이 큰 작업은 코드 작성 전 설계안을 먼저 제안하고 확인받은 뒤 구현
- **기존 리소스 우선 재사용**: 새 UI 작성 시 인라인 스타일·색상을 중복 정의하지 말고 `Resources/Colors.xaml`, `Styles.xaml`, `Icons.xaml`을 우선 재사용

## 7. 폴더 및 아키텍처 관리 규칙
- **단방향 의존성 원칙**: `Views` → `ViewModels` → `Services` → `Models` 방향만 허용 (역방향 참조 및 `Views`에서 `Services` 직접 호출 금지)
- **폴더별 책임 및 네임스페이스 일치**:
  - `Models/`: 순수 데이터·설정 모델 (UI/서비스 참조 금지)
  - `Services/`: DB·PLC(`Plc/`)·통신(`Communication/`)·내비게이션·다이얼로그 구현
  - `ViewModels/`: `~ViewModel.cs` (화면 상태 및 커맨드, `Views` 직접 참조 금지)
  - `Views/`: `~View.xaml(.cs)` (UI 전용, 재사용 커스텀 컨트롤은 `Views/Controls/` 하위 폴더 사용)
  - `Helpers/`, `Converters/`, `Resources/`: 각각 첨부 속성, 값 변환기, 공용 XAML/이미지 전용
- **새 화면/폴더 추가 체크**: 새 화면(`View` + `ViewModel`) 추가 시 `App.xaml`의 `DataTemplate` 매핑과 `App.xaml.cs` DI 등록을 함께 처리하고, 빈 폴더 생성 시 `.csproj`의 `<Folder Include="..." />` 반영

