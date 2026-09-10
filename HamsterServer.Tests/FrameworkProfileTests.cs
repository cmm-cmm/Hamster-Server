using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class FrameworkProfileTests
{
    [TestMethod]
    public void Catalog_CoversEveryFrameworkPreset()
    {
        var catalogPresets = FrameworkProfiles.All.Select(profile => profile.Preset).ToHashSet();
        CollectionAssert.AreEquivalent(Enum.GetValues<FrameworkPreset>(), catalogPresets.ToArray());
        Assert.AreEqual(FrameworkProfiles.All.Count,
            FrameworkProfiles.All.Select(profile => profile.Preset).Distinct().Count());
    }

    [TestMethod]
    public void ResolveDocumentRoot_UsesLaravelPublicDirectory()
    {
        using var root = new TestDirectory();
        var publicDir = Path.Combine(root.Path, "public");
        Directory.CreateDirectory(publicDir);
        File.WriteAllText(Path.Combine(publicDir, "index.php"), "<?php");

        var resolved = FrameworkProfiles.ResolveDocumentRoot(root.Path, FrameworkPreset.Laravel);

        Assert.AreEqual(publicDir, resolved);
    }

    [TestMethod]
    public void ResolveDocumentRoot_AppendsStrictWebRootForNewMagentoProject()
    {
        using var root = new TestDirectory();

        var resolved = FrameworkProfiles.ResolveDocumentRoot(root.Path, FrameworkPreset.Magento);

        Assert.AreEqual(Path.Combine(root.Path, "pub"), resolved);
    }

    [TestMethod]
    public void ResolveDocumentRoot_PreservesLegacyDrupalRoot()
    {
        using var root = new TestDirectory();

        var resolved = FrameworkProfiles.ResolveDocumentRoot(root.Path, FrameworkPreset.Drupal);

        Assert.AreEqual(root.Path, resolved);
    }

    [TestMethod]
    public void MissingExtensions_DoesNotReportModulesCompiledIntoPhp()
    {
        var profile = FrameworkProfiles.Get(FrameworkPreset.Magento);

        var missing = FrameworkProfiles.GetMissingManagedExtensions(profile, []);

        CollectionAssert.DoesNotContain(missing, "bcmath");
        CollectionAssert.Contains(missing, "intl");
    }
}
