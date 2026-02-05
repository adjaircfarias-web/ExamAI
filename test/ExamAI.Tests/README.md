# ExamAI.Tests

Unit tests for the ExamAI project using **xUnit**, **Moq**, and **FluentAssertions**.

## 📊 Current Status

### ✅ Implemented Tests

#### 1. DocumentHashService (17 tests) - ✅ 100% Passing
- **Coverage:** ~95% of DocumentHashService
- **Location:** `Infrastructure/Services/DocumentHashServiceTests.cs`

**Test Categories:**
- Constructor validation (1 test)
- Stream validation (2 tests)
- Hash computation (4 tests)
- Hash consistency (2 tests)
- Known hash values (3 tests - Theory)
- Hash format validation (3 tests)
- Edge cases (2 tests - large content, UTF-8, cancellation)

---

## 🎯 Test Plan

### Phase 1: High Priority ✅
- [x] **DocumentHashService** (17 tests) - DONE

### Phase 2: High Priority
- [ ] **NormalizationAgent** (~12-15 tests)
- [ ] **ValidationAgent** (~10-12 tests)

### Phase 3: Medium Priority
- [ ] **ExamRepository** (~10-12 tests)

**Total Estimated:** ~45-55 tests  
**Coverage Goal:** 80%+ minimum

---

## 🚀 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Tests
```bash
# Run only DocumentHashService tests
dotnet test --filter "FullyQualifiedName~DocumentHashService"
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Generate Coverage Report (HTML)
```bash
# Install reportgenerator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate report
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

# Open report
start coverage-report/index.html
```

---

## 📁 Project Structure

```
ExamAI.Tests/
├── Application/
│   └── Agents/
│       ├── ValidationAgentTests.cs       (TODO)
│       └── NormalizationAgentTests.cs    (TODO)
│
├── Infrastructure/
│   ├── Services/
│   │   └── DocumentHashServiceTests.cs   ✅ DONE (17 tests)
│   └── Repositories/
│       └── ExamRepositoryTests.cs        (TODO)
│
├── ExamAI.Tests.csproj
└── README.md
```

---

## 🛠️ Technologies

- **xUnit 2.9.3** - Test framework
- **Moq 4.20.72** - Mocking framework
- **FluentAssertions 6.12.2** - Fluent assertion library
- **Microsoft.EntityFrameworkCore.InMemory 10.0.2** - In-memory database for repository tests
- **coverlet.collector 6.0.4** - Code coverage collector
- **coverlet.msbuild 6.0.4** - Code coverage MSBuild integration

---

## 📊 Test Results

### DocumentHashService Tests

```
✅ All 17 tests passing
⏱️ Execution time: ~134ms
📈 Coverage: ~95% of DocumentHashService
```

**Test Summary:**
- Constructor validation: ✅
- Null/invalid stream handling: ✅
- Valid hash generation: ✅
- Hash consistency (idempotency): ✅
- Different content → different hashes: ✅
- Empty stream handling: ✅
- Large content (10MB): ✅
- Stream position reset: ✅
- UTF-8 encoding: ✅
- Cancellation token: ✅
- Known hash values validation: ✅
- Hash format (64 char, lowercase, hex): ✅

---

## 🎯 Next Steps

1. **Implement NormalizationAgent tests** (~2h)
2. **Implement ValidationAgent tests** (~2h)
3. **Implement ExamRepository tests** (~2-3h)
4. **Achieve 80%+ code coverage**
5. **Integrate with CI/CD pipeline**

---

## 📝 Notes

- **No CPF validation tests** - As per requirements
- **No patient name validation tests** - As per requirements
- **No integration tests** - Focus on unit tests only
- **No pipeline tests** - Testing individual components only
- **Single test project** - All tests in ExamAI.Tests

---

*Last update: February 4th, 2026*
