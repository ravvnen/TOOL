# Development Guide

Quick reference for local development workflow.

## 🚀 Quick Commands

### Using Make (Recommended)

```bash
# Show all available commands
make help

# Before committing (fast - format + unit tests)
make pre-commit

# Run full validation (simulates CI)
make validate

# Run specific test suites
make test-unit          # Fast unit tests
make test-integration   # Integration tests
make test-benchmark     # Thesis metrics

# Code quality
make format             # Auto-format code
make format-check       # Check format without changes
make lint               # Check for TODOs/FIXMEs

# Coverage
make coverage           # Generate coverage report
```

### Using Scripts Directly

```bash
# Quick feedback during development
./scripts/quick-test.sh

# Full validation before push
./scripts/full-validation.sh
```

## 🪝 Git Hooks Setup

### Install Pre-Commit Hook

```bash
make install-hooks
```

This installs a pre-commit hook that runs automatically before every commit:
1. ✅ Code format check
2. ✅ TODO/FIXME check (warning)
3. ✅ Build verification
4. ✅ Unit tests

### Skip Hook (Emergency Only)

```bash
git commit --no-verify -m "emergency fix"
```

⚠️ **Not recommended!** Use only for urgent hotfixes.

## 🔄 Development Workflow

### 1. Start New Feature

```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes...
```

### 2. During Development

```bash
# Quick validation (every few minutes)
make test-unit

# Or use watch mode
dotnet watch test --project tests/TOOL.Tests/TOOL.Tests.csproj --filter Unit
```

### 3. Before Committing

```bash
# Fast pre-commit check
make pre-commit

# Fix format if needed
make format

# Commit
git add .
git commit -m "feat: add new feature"
```

The pre-commit hook will run automatically and block bad commits.

### 4. Before Pushing

```bash
# Run full validation (CI simulation)
./scripts/full-validation.sh

# Or using make
make validate

# If all passes, push
git push -u origin feature/my-feature
```

### 5. Create PR

GitHub Actions will automatically:
- Run all tests
- Generate coverage report
- Run benchmarks
- Comment results on your PR

## 🧪 Testing Strategy

### Test Pyramid

```
         ┌─────────────┐
         │  Benchmarks │  (Thesis metrics - slow)
         ├─────────────┤
         │ Integration │  (API tests - moderate)
         ├─────────────┤
         │    Unit     │  (Fast - run frequently)
         └─────────────┘
```

### When to Run Each

- **Unit Tests**: After every small change (< 10 seconds)
- **Integration Tests**: Before committing (< 30 seconds)
- **Benchmarks**: Before pushing / weekly (< 2 minutes)

### Test File Naming

```
tests/TOOL.Tests/
├── Unit/
│   └── MemoryCompilerTests.cs        # Fast isolated tests
├── Integration/
│   └── MemoryApiTests.cs              # Full HTTP stack tests
└── Benchmarks/
    └── MemoryCompilerBenchmark.cs     # Thesis metrics
```

## 📊 Coverage Reports

### Generate Coverage Locally

```bash
# Generate coverage
make coverage

# Install report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:'./coverage/**/coverage.cobertura.xml' \
  -targetdir:'./coverage-report' \
  -reporttypes:Html

# Open in browser
open ./coverage-report/index.html
```

### Coverage Threshold

- **Minimum**: 60% (enforced by CI)
- **Target**: 80%
- **Goal**: > 90% for core components

## 🔧 Troubleshooting

### Pre-commit Hook Failing

```bash
# Check what's failing
make pre-commit

# Format issues?
make format

# Test failures?
make test-unit

# Still failing? Skip hook temporarily
git commit --no-verify
```

### Tests Passing Locally but Failing in CI

```bash
# Run full CI simulation
./scripts/full-validation.sh

# Clean and rebuild
make clean
make build
make test
```

### Hook Not Running

```bash
# Reinstall hooks
make install-hooks

# Verify hook exists
ls -la .git/hooks/pre-commit

# Check permissions
chmod +x .git/hooks/pre-commit
```

## 🎯 Quality Checklist

Before pushing, ensure:

- [ ] `make pre-commit` passes
- [ ] All new code has tests
- [ ] Coverage > 60% (check with `make coverage`)
- [ ] No TODO/FIXME in committed code (or document in PR)
- [ ] Commit messages follow convention:
  - `feat:` new feature
  - `fix:` bug fix
  - `test:` test improvements
  - `docs:` documentation
  - `refactor:` code refactoring

## 🚨 Emergency Fixes

If you need to commit urgently without validation:

```bash
# Skip all hooks
git commit --no-verify -m "hotfix: critical issue"

# Push directly
git push

# Then IMMEDIATELY create a PR to fix properly
```

⚠️ This should be **extremely rare**. CI will still run on push.

## 📝 Editor Setup

### VS Code

Install extensions:
- C# Dev Kit
- .NET Core Test Explorer
- Coverage Gutters

Settings (`.vscode/settings.json`):
```json
{
  "dotnet.defaultSolution": "TOOL.sln",
  "editor.formatOnSave": true,
  "csharp.format.enable": true
}
```

### Rider / Visual Studio

- Enable "Format on Save"
- Configure to run tests on build
- Enable code coverage highlighting

## 🎓 Thesis Development Tips

### Benchmark Testing

```bash
# Run benchmarks and save output
make test-benchmark | tee benchmark-results.txt

# Compare with previous results
diff benchmark-results.txt benchmark-results-old.txt
```

### Metrics Tracking

Create a spreadsheet to track metrics over time:
- Date
- Precision@3
- Recall@5
- Avg Latency
- P99 Latency

Run benchmarks weekly and log results.

## 📚 Additional Resources

- [CLAUDE.md](CLAUDE.md) - Full architecture documentation
- [README.md](README.md) - Project overview
- [.github/BRANCH_PROTECTION.md](.github/BRANCH_PROTECTION.md) - CI/CD setup
- [Tests README](tests/TOOL.Tests/README.md) - Test suite documentation

---

**Remember**: Fast local feedback = faster development! Use `make pre-commit` often. 🚀
