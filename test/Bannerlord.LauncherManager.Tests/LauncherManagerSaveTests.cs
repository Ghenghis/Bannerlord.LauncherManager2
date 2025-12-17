using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.External.UI;
using Bannerlord.LauncherManager.Models;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager.Tests;

/// <summary>
/// Tests for LauncherManagerHandler save file operations
/// </summary>
public class LauncherManagerSaveTests
{
    private class TestLauncherManagerHandler : LauncherManagerHandler
    {
        private readonly IReadOnlyList<SaveMetadata>? _saveFiles;
        private readonly SaveMetadata? _saveMetadata;
        private readonly string? _saveFilePath;

        public TestLauncherManagerHandler(
            ILauncherStateProvider launcherStateProvider,
            IGameInfoProvider gameInfoProvider,
            IFileSystemProvider fileSystemProvider,
            IDialogProvider dialogProvider,
            INotificationProvider notificationProvider,
            ILoadOrderStateProvider loadOrderStateProvider,
            IReadOnlyList<SaveMetadata>? saveFiles = null,
            SaveMetadata? saveMetadata = null,
            string? saveFilePath = null) :
            base(launcherStateProvider, gameInfoProvider, fileSystemProvider, dialogProvider, notificationProvider, loadOrderStateProvider)
        {
            _saveFiles = saveFiles;
            _saveMetadata = saveMetadata;
            _saveFilePath = saveFilePath;
        }

        public override Task<IReadOnlyList<SaveMetadata>> GetSaveFilesAsync()
        {
            return Task.FromResult(_saveFiles ?? Array.Empty<SaveMetadata>() as IReadOnlyList<SaveMetadata>);
        }

        public override Task<SaveMetadata?> GetSaveMetadataAsync(string fileName, ReadOnlyMemory<byte> data)
        {
            return Task.FromResult(_saveMetadata);
        }

        public override Task<string?> GetSaveFilePathAsync(string saveFile)
        {
            return Task.FromResult(_saveFilePath);
        }
    }

    private static TestLauncherManagerHandler CreateHandler(
        IReadOnlyList<SaveMetadata>? saveFiles = null,
        SaveMetadata? saveMetadata = null,
        string? saveFilePath = null)
    {
        return new TestLauncherManagerHandler(
            launcherStateProvider: new CallbackLauncherStateProvider(
                setGameParameters: (executable, parameters, callback) => callback(),
                getOptions: callback => callback(new LauncherOptions(false)),
                getState: callback => callback(new LauncherState(true))
            ),
            gameInfoProvider: new CallbackGameInfoProvider(
                getInstallPath: callback => callback("/test/path")
            ),
            fileSystemProvider: new CallbackFileSystemProvider(
                readFileContent: (path, offset, length, callback) => callback(null),
                writeFileContent: (path, data, callback) => callback(),
                readDirectoryFileList: (directory, callback) => callback(null),
                readDirectoryList: (directory, callback) => callback(null)
            ),
            dialogProvider: new CallbackDialogProvider(
                sendDialog: (type, title, message, filters, callback) => callback("")
            ),
            notificationProvider: new CallbackNotificationProvider(
                sendNotification: (id, type, message, ms) => { }
            ),
            loadOrderStateProvider: new CallbackLoadOrderStateProvider(
                getAllModuleViewModels: callback => callback([]),
                getModuleViewModels: callback => callback([]),
                setModuleViewModels: (modules, callback) => callback()
            ),
            saveFiles: saveFiles,
            saveMetadata: saveMetadata,
            saveFilePath: saveFilePath
        );
    }

    [Test]
    public async Task GetSaveFilesAsync_ReturnsEmptyList_WhenNoSaveFiles()
    {
        var handler = CreateHandler(saveFiles: []);

        var saveFiles = await handler.GetSaveFilesAsync();

        Assert.That(saveFiles, Is.Empty);
    }

    [Test]
    public async Task GetSaveFilesAsync_ReturnsSaveFiles_WhenSaveFilesExist()
    {
        var expectedSaveFiles = new List<SaveMetadata>
        {
            new SaveMetadata("Save1"),
            new SaveMetadata("Save2")
        };
        var handler = CreateHandler(saveFiles: expectedSaveFiles);

        var saveFiles = await handler.GetSaveFilesAsync();

        Assert.That(saveFiles.Count, Is.EqualTo(2));
        Assert.That(saveFiles[0].Name, Is.EqualTo("Save1"));
        Assert.That(saveFiles[1].Name, Is.EqualTo("Save2"));
    }

    [Test]
    public async Task GetSaveMetadataAsync_ReturnsNull_WhenNoMetadata()
    {
        var handler = CreateHandler(saveMetadata: null);

        var metadata = await handler.GetSaveMetadataAsync("test.sav", ReadOnlyMemory<byte>.Empty);

        Assert.That(metadata, Is.Null);
    }

    [Test]
    public async Task GetSaveMetadataAsync_ReturnsMetadata_WhenMetadataExists()
    {
        var expectedMetadata = new SaveMetadata("TestSave")
        {
            ["Modules"] = "Native;SandBox",
            ["ApplicationVersion"] = "v1.2.3.456"
        };
        var handler = CreateHandler(saveMetadata: expectedMetadata);

        var metadata = await handler.GetSaveMetadataAsync("test.sav", ReadOnlyMemory<byte>.Empty);

        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.Name, Is.EqualTo("TestSave"));
        Assert.That(metadata["Modules"], Is.EqualTo("Native;SandBox"));
    }

    [Test]
    public async Task GetSaveFilePathAsync_ReturnsNull_WhenNoPath()
    {
        var handler = CreateHandler(saveFilePath: null);

        var path = await handler.GetSaveFilePathAsync("test.sav");

        Assert.That(path, Is.Null);
    }

    [Test]
    public async Task GetSaveFilePathAsync_ReturnsPath_WhenPathExists()
    {
        var expectedPath = "/saves/test.sav";
        var handler = CreateHandler(saveFilePath: expectedPath);

        var path = await handler.GetSaveFilePathAsync("test.sav");

        Assert.That(path, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task GetSaveFilesAsync_ReturnsSaveFilesWithMetadata()
    {
        var twMetadata = new TWSaveMetadata
        {
            List = new Dictionary<string, string>
            {
                { "Modules", "Native;SandBox;StoryMode" },
                { "Module_Native", "v1.2.3.456" },
                { "ApplicationVersion", "v1.2.3.456" }
            }
        };
        var saveMetadata = new SaveMetadata("MySave", twMetadata);
        var handler = CreateHandler(saveFiles: [saveMetadata]);

        var saveFiles = await handler.GetSaveFilesAsync();

        Assert.That(saveFiles.Count, Is.EqualTo(1));
        var save = saveFiles[0];
        Assert.That(save.Name, Is.EqualTo("MySave"));
        Assert.That(save.GetModules().Length, Is.EqualTo(3));
        Assert.That(save.GetChangeSet(), Is.EqualTo(456));
    }
}

/// <summary>
/// Tests for default LauncherManagerHandler save file behavior
/// </summary>
public class LauncherManagerHandlerDefaultSaveTests
{
    private class DefaultSaveHandler : LauncherManagerHandler
    {
        public DefaultSaveHandler(
            ILauncherStateProvider launcherStateProvider,
            IGameInfoProvider gameInfoProvider,
            IFileSystemProvider fileSystemProvider,
            IDialogProvider dialogProvider,
            INotificationProvider notificationProvider,
            ILoadOrderStateProvider loadOrderStateProvider) :
            base(launcherStateProvider, gameInfoProvider, fileSystemProvider, dialogProvider, notificationProvider, loadOrderStateProvider)
        {
        }
    }

    private static DefaultSaveHandler CreateDefaultHandler()
    {
        return new DefaultSaveHandler(
            launcherStateProvider: new CallbackLauncherStateProvider(
                setGameParameters: (executable, parameters, callback) => callback(),
                getOptions: callback => callback(new LauncherOptions(false)),
                getState: callback => callback(new LauncherState(true))
            ),
            gameInfoProvider: new CallbackGameInfoProvider(
                getInstallPath: callback => callback("/test/path")
            ),
            fileSystemProvider: new CallbackFileSystemProvider(
                readFileContent: (path, offset, length, callback) => callback(null),
                writeFileContent: (path, data, callback) => callback(),
                readDirectoryFileList: (directory, callback) => callback(null),
                readDirectoryList: (directory, callback) => callback(null)
            ),
            dialogProvider: new CallbackDialogProvider(
                sendDialog: (type, title, message, filters, callback) => callback("")
            ),
            notificationProvider: new CallbackNotificationProvider(
                sendNotification: (id, type, message, ms) => { }
            ),
            loadOrderStateProvider: new CallbackLoadOrderStateProvider(
                getAllModuleViewModels: callback => callback([]),
                getModuleViewModels: callback => callback([]),
                setModuleViewModels: (modules, callback) => callback()
            )
        );
    }

    [Test]
    public async Task GetSaveFilesAsync_Default_ReturnsEmptyList()
    {
        var handler = CreateDefaultHandler();

        var saveFiles = await handler.GetSaveFilesAsync();

        Assert.That(saveFiles, Is.Empty);
    }

    [Test]
    public async Task GetSaveMetadataAsync_Default_ReturnsNull()
    {
        var handler = CreateDefaultHandler();

        var metadata = await handler.GetSaveMetadataAsync("test.sav", ReadOnlyMemory<byte>.Empty);

        Assert.That(metadata, Is.Null);
    }

    [Test]
    public async Task GetSaveFilePathAsync_Default_ReturnsNull()
    {
        var handler = CreateDefaultHandler();

        var path = await handler.GetSaveFilePathAsync("test.sav");

        Assert.That(path, Is.Null);
    }
}
