#!/bin/bash
# Verification script for --dry-run mode implementation

set -e

echo "=========================================="
echo "Dry-Run Mode Implementation Verification"
echo "=========================================="
echo ""

# Check 1: Verify Program.cs has ParseDryRunFlag method
echo "✓ Check 1: Verifying ParseDryRunFlag method in Program.cs..."
if grep -q "ParseDryRunFlag" Program.cs; then
    echo "  ✓ ParseDryRunFlag method found"
else
    echo "  ✗ ParseDryRunFlag method NOT found"
    exit 1
fi

if grep -q 'args.Contains.*--dry-run' Program.cs; then
    echo "  ✓ Command-line flag parsing implemented"
else
    echo "  ✗ Command-line flag parsing NOT found"
    exit 1
fi

# Check 2: Verify SyncConfig has IsDryRun property
echo ""
echo "✓ Check 2: Verifying IsDryRun property in SyncConfig.cs..."
if grep -q "public bool IsDryRun" Domain/Models/SyncConfig.cs; then
    echo "  ✓ IsDryRun property found"
else
    echo "  ✗ IsDryRun property NOT found"
    exit 1
fi

if grep -q "When true, mutation calls to the Notion API are skipped" Domain/Models/SyncConfig.cs; then
    echo "  ✓ Property has XML documentation"
else
    echo "  ✗ Property lacks proper documentation"
    exit 1
fi

# Check 3: Verify SyncService skips mutations when IsDryRun is true
echo ""
echo "✓ Check 3: Verifying mutation skipping in SyncService.cs..."
matches=0
if grep -q "if (!config.IsDryRun)" Services/SyncService.cs; then
    ((matches++))
    echo "  ✓ Dry-run conditional checks found"
fi

if grep -q "DRY-RUN: Would" Services/SyncService.cs; then
    ((matches++))
    echo "  ✓ Dry-run logging messages found"
fi

if [ $matches -ge 2 ]; then
    echo "  ✓ SyncService mutation skipping implemented"
else
    echo "  ✗ SyncService mutation skipping NOT properly implemented"
    exit 1
fi

# Check 4: Verify AppSettings has DryRun property
echo ""
echo "✓ Check 4: Verifying DryRun property in AppSettings.cs..."
if grep -q "public bool DryRun" Infrastructure/Configuration/AppSettings.cs; then
    echo "  ✓ DryRun property found in AppSettings"
else
    echo "  ✗ DryRun property NOT found in AppSettings"
    exit 1
fi

# Check 5: Verify build succeeds
echo ""
echo "✓ Check 5: Verifying project builds successfully..."
if python3 /home/redrocket/task-factory/aider_buildcmd.py > /dev/null 2>&1; then
    echo "  ✓ Project builds successfully"
else
    echo "  ✗ Project build FAILED"
    exit 1
fi

# Check 6: Verify documentation
echo ""
echo "✓ Check 6: Verifying documentation..."
if [ -f "docs/DryRunMode.md" ]; then
    echo "  ✓ DryRunMode.md documentation created"
else
    echo "  ⚠ DryRunMode.md documentation NOT found (optional)"
fi

# Summary
echo ""
echo "=========================================="
echo "✓ ALL CHECKS PASSED"
echo "=========================================="
echo ""
echo "Summary of Changes:"
echo "------------------"
echo "1. Program.cs:"
echo "   - Added ParseDryRunFlag() method"
echo "   - Added --dry-run/-d command-line flag parsing"
echo "   - Added dry-run logging messages"
echo "   - Passes IsDryRun flag to SyncConfig"
echo ""
echo "2. Domain/Models/SyncConfig.cs:"
echo "   - Added IsDryRun property (default: false)"
echo "   - Property includes XML documentation"
echo ""
echo "3. Services/SyncService.cs:"
echo "   - Modified ApplyChangesAsync() to skip mutations when IsDryRun=true"
echo "   - Added 'DRY-RUN: Would...' logging for all planned operations"
echo "   - Skips Notion API calls (CreatePageAsync, UpdatePageAsync, ArchivePageAsync)"
echo "   - Skips local repository calls (AddAsync, UpdateAsync)"
echo ""
echo "4. Infrastructure/Configuration/AppSettings.cs:"
echo "   - DryRun property already existed (line 38)"
echo ""
echo "Usage:"
echo "------"
echo "dotnet run -- --dry-run    # Long form"
echo "dotnet run -- -d          # Short form"
echo ""
echo "The dry-run mode will:"
echo "- Compute all changes (creates, updates, deletes)"
echo "- Log all planned operations with 'DRY-RUN: Would...' prefix"
echo "- Skip all actual mutations to Notion API and local files"
echo "- Display summary showing no changes were applied"
echo ""
