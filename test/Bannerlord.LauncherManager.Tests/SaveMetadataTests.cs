using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Tests;

public class SaveMetadataTests
{
    [Test]
    public void Constructor_Default_CreatesEmptyDictionary()
    {
        var metadata = new SaveMetadata();
        
        Assert.That(metadata.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithName_SetsName()
    {
        var metadata = new SaveMetadata("TestSave");
        
        Assert.That(metadata.Name, Is.EqualTo("TestSave"));
        Assert.That(metadata.Count, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithNameAndTWSaveMetadata_CopiesMetadataAndSetsName()
    {
        var twMetadata = new TWSaveMetadata
        {
            List = new Dictionary<string, string>
            {
                { "Modules", "Native;SandBox" },
                { "Module_Native", "v1.0.0.0" },
                { "ApplicationVersion", "v1.2.3.456" }
            }
        };
        
        var metadata = new SaveMetadata("TestSave", twMetadata);
        
        Assert.That(metadata.Name, Is.EqualTo("TestSave"));
        Assert.That(metadata["Modules"], Is.EqualTo("Native;SandBox"));
        Assert.That(metadata["Module_Native"], Is.EqualTo("v1.0.0.0"));
        Assert.That(metadata["ApplicationVersion"], Is.EqualTo("v1.2.3.456"));
    }

    [Test]
    public void GetModules_WithValidModulesKey_ReturnsModuleArray()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["Modules"] = "Native;SandBox;StoryMode"
        };
        
        var modules = metadata.GetModules();
        
        Assert.That(modules.Length, Is.EqualTo(3));
        Assert.That(modules[0], Is.EqualTo("Native"));
        Assert.That(modules[1], Is.EqualTo("SandBox"));
        Assert.That(modules[2], Is.EqualTo("StoryMode"));
    }

    [Test]
    public void GetModules_WithEmptyModulesKey_ReturnsSingleEmptyString()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["Modules"] = ""
        };
        
        var modules = metadata.GetModules();
        
        Assert.That(modules.Length, Is.EqualTo(1));
        Assert.That(modules[0], Is.EqualTo(""));
    }

    [Test]
    public void GetModules_WithoutModulesKey_ReturnsEmptyArray()
    {
        var metadata = new SaveMetadata("TestSave");
        
        var modules = metadata.GetModules();
        
        Assert.That(modules, Is.Empty);
    }

    [Test]
    public void GetModuleVersion_WithValidModuleVersion_ReturnsVersion()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["Module_Native"] = "v1.2.3.456"
        };
        
        var version = metadata.GetModuleVersion("Native");
        
        Assert.That(version.Major, Is.EqualTo(1));
        Assert.That(version.Minor, Is.EqualTo(2));
        Assert.That(version.Revision, Is.EqualTo(3));
        Assert.That(version.ChangeSet, Is.EqualTo(456));
    }

    [Test]
    public void GetModuleVersion_WithInvalidVersionString_ReturnsEmptyVersion()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["Module_Native"] = "invalid"
        };
        
        var version = metadata.GetModuleVersion("Native");
        
        Assert.That(version, Is.EqualTo(ApplicationVersion.Empty));
    }

    [Test]
    public void GetModuleVersion_WithoutModuleVersion_ReturnsEmptyVersion()
    {
        var metadata = new SaveMetadata("TestSave");
        
        var version = metadata.GetModuleVersion("NonExistentModule");
        
        Assert.That(version, Is.EqualTo(ApplicationVersion.Empty));
    }

    [Test]
    public void GetChangeSet_WithValidApplicationVersion_ReturnsChangeSet()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["ApplicationVersion"] = "v1.2.3.456"
        };
        
        var changeset = metadata.GetChangeSet();
        
        Assert.That(changeset, Is.EqualTo(456));
    }

    [Test]
    public void GetChangeSet_WithInvalidFormat_ReturnsMinusOne()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["ApplicationVersion"] = "v1.2.3"  // Missing 4th part
        };
        
        var changeset = metadata.GetChangeSet();
        
        Assert.That(changeset, Is.EqualTo(-1));
    }

    [Test]
    public void GetChangeSet_WithNonNumericChangeSet_ReturnsMinusOne()
    {
        var metadata = new SaveMetadata("TestSave")
        {
            ["ApplicationVersion"] = "v1.2.3.abc"
        };
        
        var changeset = metadata.GetChangeSet();
        
        Assert.That(changeset, Is.EqualTo(-1));
    }

    [Test]
    public void GetChangeSet_WithoutApplicationVersion_ReturnsMinusOne()
    {
        var metadata = new SaveMetadata("TestSave");
        
        var changeset = metadata.GetChangeSet();
        
        Assert.That(changeset, Is.EqualTo(-1));
    }

    [Test]
    public void GetChangeSet_WithNullApplicationVersion_ReturnsMinusOne()
    {
        var metadata = new SaveMetadata("TestSave");
        // Explicitly add null value to test null handling in GetChangeSet
        string? nullVersion = null;
        metadata["ApplicationVersion"] = nullVersion!;
        
        var changeset = metadata.GetChangeSet();
        
        Assert.That(changeset, Is.EqualTo(-1));
    }

    [Test]
    public void Name_AccessWithoutSetting_ThrowsKeyNotFoundException()
    {
        var metadata = new SaveMetadata();
        
        Assert.Throws<KeyNotFoundException>(() => _ = metadata.Name);
    }
}

public class TWSaveMetadataTests
{
    [Test]
    public void Constructor_Default_CreatesEmptyList()
    {
        var metadata = new TWSaveMetadata();
        
        Assert.That(metadata.List, Is.Not.Null);
        Assert.That(metadata.List.Count, Is.EqualTo(0));
    }

    [Test]
    public void List_CanSetAndGetValues()
    {
        var metadata = new TWSaveMetadata
        {
            List = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            }
        };
        
        Assert.That(metadata.List.Count, Is.EqualTo(2));
        Assert.That(metadata.List["Key1"], Is.EqualTo("Value1"));
        Assert.That(metadata.List["Key2"], Is.EqualTo("Value2"));
    }
}
