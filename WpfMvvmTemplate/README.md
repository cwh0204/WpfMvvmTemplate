# WpfMvvmTemplate

![.NET](https://img.shields.io/badge/.NET-10.0--windows-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF%20MVVM-0284C7?style=flat-square)
![SQLite](https://img.shields.io/badge/DB-SQLite%20%2B%20Dapper-003B57?style=flat-square&logo=sqlite)
![PLC](https://img.shields.io/badge/PLC-LS%20XGT%20(FEnet%20%2F%20Cnet)-0B3A78?style=flat-square)

**장비 시험기 및 산업용 제어 프로그램** 개발을 위해 구성된 **WPF (.NET 10) MVVM 스타터 템플릿**입니다.  
제어기 PC(`1280×1024`)와 일반 모니터(`1920×1080`) 모두에 대응하는 적응형 UI, SQLite + Dapper 로컬 DB, 그리고 LS산전 XGT PLC 2계층 통신 구조를 기본 제공합니다.

---

## 주요 특징

| 영역 | 구성 내용 |
| :--- | :--- |
| **아키텍처** | `CommunityToolkit.Mvvm` 소스 제너레이터 + `Microsoft.Extensions.Hosting` DI |
| **내비게이션 / 모달** | `INavigationService` 기반 전체 화면 전환 및 모달 오버레이 레이어 |
| **다이얼로그** | `IDialogService` 기반 커스텀 알림/확인 모달 (`ConfirmDialogView`) |
| **데이터베이스** | `Microsoft.Data.Sqlite` + `Dapper` (`app.db` 자동 초기화 및 스키마 마이그레이션) |
| **PLC 통신** | 전송 계층(`TcpTransport` / `SerialTransport`) + 프로토콜(`LsXgtPlcService` FEnet/Cnet) |
| **개발자 전용 도구** | `#if DEBUG` 조건부 컴파일 기반 `PlcTestView` (`Release` 배포본에서 자동 제외) |
| **디자인 시스템** | 라이트 톤 팔레트(`Colors.xaml`), 통일된 `1.75px` 선형 벡터 아이콘(`Icons.xaml`), 공용 스타일(`Styles.xaml`) |

---

## 프로젝트 구조

```text
WpfMvvmTemplate/
├── Converters/                  # 공용 XAML 값 변환기 (InverseBooleanConverter 등)
├── Helpers/                     # 첨부 속성 헬퍼 (PasswordBoxHelper)
├── Models/                      # 데이터·설정 모델 (ResultRecord, TestModels, PlcConnectionSettings 등)
├── Resources/
│   ├── Images/                  # 배경·로고 이미지 (Build Action: Resource 자동 포함)
│   ├── Colors.xaml              # 브랜드·시맨틱 색상 토큰
│   ├── Icons.xaml               # 24x24 규격 1.75px 선형 벡터 아이콘
│   └── Styles.xaml              # 공용 컨트롤 스타일 및 애니메이션
├── Services/
│   ├── Communication/           # 물리 전송 계층 (ICommunicationTransport, TcpTransport, SerialTransport)
│   ├── Plc/                     # PLC 프로토콜 계층 (IPlcService, LsXgtPlcService)
│   ├── SqliteDbContext.cs       # SQLite 연결 및 테이블 초기화
│   ├── SqliteResultRepository.cs# 시험 이력 저장·조회·삭제 (Dapper)
│   ├── SettingsService.cs       # 설정·비밀번호(SHA-256)·PLC 설정 관리
│   ├── NavigationService.cs     # 화면 및 모달 전환
│   └── DialogService.cs         # 확인/알림 모달 서비스
├── ViewModels/                  # 화면별 ViewModel
├── Views/                       # 화면별 XAML UserControl 및 MainWindow 셸
├── App.xaml(.cs)                # DI 등록, 리소스 병합, 전역 예외 처리
└── AGENTS.md                    # AI 개발 어시스턴트용 아키텍처·품질 규칙
```

---

## 기본 포함 화면

- **홈 화면 (`HomeView`)**: 해상도 적응형 5카드 런처 (`시험`, `결과 검색`, `자가 진단`, `설정`, `종료`)
- **장비 시험 (`TestView`)**: 자동/매뉴얼 모드 탭, 필수 공통 5종 입력(시험 중 자동 잠금), 3-State 시험 항목 선택 트리, 실시간 결과 테이블
- **결과 조회 (`SearchResultView`)**: 기간·키워드·판정(`전체`/`OK`/`NG`) 필터, 요약 카드 8종, 상세 계측 테이블, 이력 삭제
- **자가 진단 (`SelfDiagnosisView`)**: 모달 오버레이 기반 PLC 연결 점검
- **시스템 설정 (`SettingsView` + `PasswordPromptView`)**:
  - 진입 비밀번호 잠금/해제 및 변경 (`app.db` 저장, 초기 비밀번호 `0000`)
  - LS산전 PLC 통신 설정 (이더넷 TCP / 시리얼 RS-232·485 전환, 연결 테스트)
  - 시스템 정보 (앱 버전 / .NET 런타임)
- **PLC 사전 테스트 (`PlcTestView`, `Debug` 전용)**: Bit/Word 읽기·쓰기 및 실시간 TX/RX 로그 모니터

---

## 새 프로젝트 적용 체크리스트

1. **브랜딩 변경**
   - `ViewModels/MainViewModel.cs` → `WindowTitle` 수정
   - `ViewModels/HomeViewModel.cs` → `Title`, `Subtitle` 수정
   - `Resources/Images/` → `background-img.jpg`, `Logo-img.png` 교체
2. **시험 시퀀스 및 장비 UI 구현**
   - `ViewModels/TestViewModel.cs` → `InitializeSampleTestGroups()` 및 `StartAutoTestAsync()`에 실제 장비 제어 로직 작성
   - `Views/TestView.xaml` → 상단 실시간 센서 카드 및 매뉴얼 시험 슬롯(`IsManualMode`)에 장비 전용 UI 배치
3. **자가 진단 항목 확장**
   - `ViewModels/SelfDiagnosisViewModel.cs` → `Items` 컬렉션에 점검 항목(`DiagnosisItem`) 추가
4. **배포 빌드 확인**
   - `Release` 빌드 시 `PlcTestView` 진입 경로와 DI 등록이 자동으로 제외되는지 확인

---

## 시작하기

```bash
dotnet restore
dotnet build -c Debug
dotnet build -c Release
```

> **참고:** GitHub 저장소 메인 페이지에 README가 바로 표시되려면 `README.md` 파일이 `.git` 폴더와 같은 최상위 루트에 위치해야 합니다.
