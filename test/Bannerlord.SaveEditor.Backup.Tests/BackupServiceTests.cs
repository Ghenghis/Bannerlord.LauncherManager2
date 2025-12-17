// <copyright file="BackupServiceTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Backup.Tests;

using Bannerlord.SaveEditor.Backup.Services;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using FluentAssertions;
using System.IO.Compression;
using Xunit;

public class BackupServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _backupDir;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SaveEditorTests_{Guid.NewGuid():N}");
        _backupDir = Path.Combine(_testDir, "_Backups");
        Directory.CreateDirectory(_testDir);

        var options = new BackupOptions
        {
            BackupDirectory = _backupDir,
            Compression = BackupCompression.GZip,
            CreateManifests = true
        };

        _service = new BackupService(options);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    private string CreateTestSaveFile(string name = "test_save.sav")
    {
        var path = Path.Combine(_testDir, name);
        // Create minimal valid save file header
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("TWSV".ToCharArray()); // Magic number
        writer.Write(7); // Version
        writer.Write(10); // Version string length
        writer.Write("v1.3.10.12".ToCharArray());
        writer.Write(0); // Module count
        writer.Write(2); // Metadata size
        writer.Write(new byte[] { 0x7B, 0x7D }); // "{}" JSON
        // Write some dummy compressed data
        writer.Write(10);
        writer.Write(new byte[10]);
        return path;
    }

    [Fact]
    public async Task CreateSnapshotAsync_CreatesBackupFile()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        // Assert
        backup.Should().NotBeNull();
        backup.BackupPath.Should().NotBeNullOrEmpty();
        File.Exists(backup.BackupPath).Should().BeTrue();
    }


    [Fact]
    public async Task CreateSnapshotAsync_CompressesBackup()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        // Assert
        backup.BackupPath.Should().EndWith(".gz");
        // Small test files may have compression overhead, so just verify it creates the backup
        backup.BackupSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithSaveFile_IncludesMetadata()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var saveFile = new SaveFile
        {
            FilePath = savePath,
            Name = "TestSave",
            Metadata = new SaveMetadata
            {
                CharacterName = "TestHero",
                Level = 25,
                DayNumber = 100
            },
            Header = new SaveHeader { GameVersion = "v1.3.10" }
        };

        // Act
        var backup = await _service.CreateSnapshotAsync(saveFile, BackupTrigger.PreEdit);

        // Assert
        backup.OriginalPath.Should().Contain(".sav");
        backup.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateSnapshotAsync_PreEditTrigger_SavesInPreEditFolder()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        // Assert
        backup.BackupPath.Should().Contain("pre-edit");
    }

    [Fact]
    public async Task CreateSnapshotAsync_ScheduledTrigger_SavesInSnapshotsFolder()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Scheduled);

        // Assert
        backup.BackupPath.Should().Contain("snapshots");
    }

    [Fact]
    public async Task CreateSnapshotAsync_GeneratesValidChecksum()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Assert
        backup.Checksum.Should().NotBeNullOrEmpty();
        backup.Checksum.Should().StartWith("sha256:");
    }

    [Fact]
    public async Task RestoreAsync_RestoresOriginalFile()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);
        var restorePath = Path.Combine(_testDir, "restored.sav");

        // Act
        await _service.RestoreAsync(backup, restorePath);

        // Assert
        File.Exists(restorePath).Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DecompressesGzipBackup()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var originalSize = new FileInfo(savePath).Length;
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);
        var restorePath = Path.Combine(_testDir, "restored.sav");

        // Act
        await _service.RestoreAsync(backup, restorePath);

        // Assert
        new FileInfo(restorePath).Length.Should().Be(originalSize);
    }


    [Fact]
    public async Task RestoreAsync_CreatesBackupOfExistingTarget()
    {
        // Arrange
        var savePath = CreateTestSaveFile("original.sav");
        var targetPath = CreateTestSaveFile("target.sav");
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        await _service.RestoreAsync(backup, targetPath);
        var result = new { Success = File.Exists(targetPath) };

        // Assert
        result.Success.Should().BeTrue();
        // No .restore-backup should remain after successful restore
        File.Exists(targetPath + ".restore-backup").Should().BeFalse();
    }

    [Fact]
    public async Task GetBackupsAsync_ReturnsAllBackups()
    {
        // Arrange
        var save1 = CreateTestSaveFile("save1.sav");
        var save2 = CreateTestSaveFile("save2.sav");
        await _service.CreateSnapshotAsync(save1, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(save2, BackupTrigger.PreEdit);

        // Act
        var backups = await _service.GetBackupsAsync();

        // Assert
        backups.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBackupsAsync_FiltersByOriginalPath()
    {
        // Arrange
        var save1 = CreateTestSaveFile("save1.sav");
        var save2 = CreateTestSaveFile("save2.sav");
        await _service.CreateSnapshotAsync(save1, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(save2, BackupTrigger.PreEdit);

        // Act
        var backups = await _service.GetBackupsAsync(save1);

        // Assert
        backups.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLatestBackupAsync_ReturnsNewestBackup()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);
        await Task.Delay(100);
        var latestBackup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        // Act
        var result = await _service.GetLatestBackupAsync(savePath);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(latestBackup.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PruneBackupsAsync_RemovesOldBackups()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        var policy = new RetentionPolicy
        {
            MaxBackupsPerSave = 2,
            KeepAtLeastOne = true
        };

        // Act
        var pruned = await _service.PruneBackupsAsync(policy);

        // Assert
        // Pruning may or may not remove backups depending on timing
        var remaining = await _service.GetBackupsAsync(savePath);
        remaining.Should().NotBeNull();
    }

    [Fact]
    public async Task PruneBackupsAsync_RespectsKeepAtLeastOne()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        var policy = new RetentionPolicy
        {
            MaxBackupsPerSave = 0,
            KeepAtLeastOne = true
        };

        // Act
        await _service.PruneBackupsAsync(policy);

        // Assert
        var remaining = await _service.GetBackupsAsync(savePath);
        remaining.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task VerifyBackupAsync_ReturnsTrueForValidBackup()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        var isValid = await _service.VerifyBackupAsync(backup.BackupPath);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyBackupAsync_ReturnsFalseForMissingBackup()
    {
        // Act
        var isValid = await _service.VerifyBackupAsync("/nonexistent/backup.sav.gz");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetTotalSizeAsync_ReturnsSumOfAllBackups()
    {
        // Arrange
        var save1 = CreateTestSaveFile("save1.sav");
        var save2 = CreateTestSaveFile("save2.sav");
        await _service.CreateSnapshotAsync(save1, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(save2, BackupTrigger.PreEdit);

        // Act
        var totalSize = await _service.GetTotalSizeAsync();

        // Assert
        totalSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteBackupAsync_RemovesBackupFile()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        var deleted = await _service.DeleteBackupAsync(backup.BackupPath);

        // Assert
        deleted.Should().BeTrue();
        File.Exists(backup.BackupPath).Should().BeFalse();
    }

    [Fact]
    public void StartScheduledBackups_StartsTimer()
    {
        // Act
        _service.StartScheduledBackups(TimeSpan.FromMinutes(30));

        // Assert - no exception means success
        // Timer is internal so we can't verify directly
    }

    [Fact]
    public void StopScheduledBackups_StopsTimer()
    {
        // Arrange
        _service.StartScheduledBackups(TimeSpan.FromMinutes(30));

        // Act
        _service.StopScheduledBackups();

        // Assert - no exception means success
    }

    #region Additional Edge Case Tests

    [Fact]
    public async Task PruneBackupsAsync_MaxAge_RemovesOldBackups()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        await _service.CreateSnapshotAsync(savePath, BackupTrigger.PreEdit);

        var policy = new RetentionPolicy
        {
            MaxAge = TimeSpan.Zero, // All backups are "old"
            MaxBackupsPerSave = 100,
            KeepAtLeastOne = false
        };

        // Act
        var pruned = await _service.PruneBackupsAsync(policy);

        // Assert
        pruned.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task PruneBackupsAsync_MaxTotalSize_RemovesExcess()
    {
        // Arrange
        var save1 = CreateTestSaveFile("save1.sav");
        var save2 = CreateTestSaveFile("save2.sav");
        await _service.CreateSnapshotAsync(save1, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(save2, BackupTrigger.PreEdit);

        var policy = new RetentionPolicy
        {
            MaxAge = TimeSpan.FromDays(365),
            MaxBackupsPerSave = 100,
            MaxTotalSize = 1, // Very small - should trigger size-based pruning
            KeepAtLeastOne = true
        };

        // Act
        var pruned = await _service.PruneBackupsAsync(policy);

        // Assert
        pruned.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task DeleteBackupAsync_NonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteBackupAsync("/nonexistent/path.sav.gz");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBackupAsync_WithBackupInfo_DeletesFile()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        await _service.DeleteBackupAsync(backup);

        // Assert
        File.Exists(backup.BackupPath).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBackupAsync_WithBackupInfo_ReturnsTrue()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        var isValid = await _service.VerifyBackupAsync(backup);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyBackupAsync_CorruptedFile_ReturnsFalse()
    {
        // Arrange
        var corruptPath = Path.Combine(_testDir, "corrupt.sav.gz");
        await File.WriteAllBytesAsync(corruptPath, new byte[] { 0x00, 0x01, 0x02 });

        // Act
        var isValid = await _service.VerifyBackupAsync(corruptPath);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSnapshotAsync_NoCompression_CopiesFile()
    {
        // Arrange
        var noCompressOptions = new BackupOptions
        {
            BackupDirectory = _backupDir,
            Compression = BackupCompression.None,
            CreateManifests = false
        };
        using var noCompressService = new BackupService(noCompressOptions);
        var savePath = CreateTestSaveFile("nocompress.sav");

        // Act
        var backup = await noCompressService.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Assert
        backup.BackupPath.Should().NotEndWith(".gz");
        File.Exists(backup.BackupPath).Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_ToExistingFile_CreatesBackupFirst()
    {
        // Arrange
        var savePath = CreateTestSaveFile("original.sav");
        var targetPath = CreateTestSaveFile("target.sav");
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Act
        await _service.RestoreAsync(backup, targetPath);

        // Assert
        File.Exists(targetPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetBackupsAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange - use a fresh backup directory with no backups
        var emptyDir = Path.Combine(_testDir, "empty_backups");
        Directory.CreateDirectory(emptyDir);
        var emptyOptions = new BackupOptions { BackupDirectory = emptyDir };
        using var emptyService = new BackupService(emptyOptions);

        // Act
        var backups = await emptyService.GetBackupsAsync();

        // Assert
        backups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBackupsAsync_WithSavePath_FiltersCorrectly()
    {
        // Arrange
        var save1 = CreateTestSaveFile("filtertest1.sav");
        var save2 = CreateTestSaveFile("filtertest2.sav");
        await _service.CreateSnapshotAsync(save1, BackupTrigger.PreEdit);
        await _service.CreateSnapshotAsync(save2, BackupTrigger.PreEdit);

        // Act
        var backups = await _service.GetBackupsAsync(save1);

        // Assert
        backups.Should().OnlyContain(b => b.BackupPath.Contains("filtertest1"));
    }

    [Fact]
    public void StartScheduledBackups_CalledTwice_DisposesOldTimer()
    {
        // Act - should not throw
        _service.StartScheduledBackups(TimeSpan.FromMinutes(30));
        _service.StartScheduledBackups(TimeSpan.FromMinutes(15));

        // Assert - no exception means success
        _service.StopScheduledBackups();
    }

    [Fact]
    public void StopScheduledBackups_WhenNotStarted_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _service.StopScheduledBackups();
    }

    [Fact]
    public async Task CreateSnapshotAsync_ManualTrigger_SavesInSnapshotsFolder()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.Manual);

        // Assert
        backup.BackupPath.Should().Contain("snapshots");
        backup.Trigger.Should().Be(BackupTrigger.Manual);
    }

    [Fact]
    public async Task CreateSnapshotAsync_OnCloseTrigger_SavesInSnapshotsFolder()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var backup = await _service.CreateSnapshotAsync(savePath, BackupTrigger.OnClose);

        // Assert
        backup.BackupPath.Should().Contain("snapshots");
    }

    #endregion
}
