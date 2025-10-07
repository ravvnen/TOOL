# Makefile for TOOL project
# Usage: make <target>

.PHONY: help build format format-check clean install-hooks

# Default target
help:
	@echo "Available targets:"
	@echo "  make build         - Build the solution"
	@echo "  make format        - Format code with CSharpier"
	@echo "  make format-check  - Check code formatting (no changes)"
	@echo "  make clean         - Clean build artifacts"
	@echo "  make install-hooks - Install git pre-commit hooks"

# Build the solution
build:
	@echo "🔨 Building solution..."
	@dotnet build --configuration Release

# Format code with CSharpier
format:
	@echo "✨ Formatting code with CSharpier..."
	@~/.dotnet/tools/csharpier format .

# Check code formatting (no changes)
format-check:
	@echo "🔍 Checking code format with CSharpier..."
	@~/.dotnet/tools/csharpier check .

# Clean build artifacts
clean:
	@echo "🧹 Cleaning build artifacts..."
	@dotnet clean
	@rm -rf **/bin **/obj
	@echo "✅ Clean complete"

# Install git pre-commit hooks
install-hooks:
	@echo "🪝 Installing git hooks..."
	@cp scripts/pre-commit .git/hooks/pre-commit
	@chmod +x .git/hooks/pre-commit
	@echo "✅ Git hooks installed!"
	@echo "Pre-commit hook will run: format check + build"
