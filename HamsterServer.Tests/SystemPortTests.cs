using System.Net;
using System.Net.Sockets;
using HamsterServer.Core;
using HamsterServer.Models;

namespace HamsterServer.Tests;

[TestClass]
public sealed class SystemPortTests
{
    [TestMethod]
    public void NetworkPort_ReadsTheBigEndianLow16Bits()
    {
        // GetExtendedTcpTable stores port 80 (0x0050) as the bytes 00 50, i.e. 0x5000 little-endian.
        Assert.AreEqual(80, SystemPortScanner.NetworkPort(0x5000));
        Assert.AreEqual(8080, SystemPortScanner.NetworkPort(0x901F));
        Assert.AreEqual(65535, SystemPortScanner.NetworkPort(0xFFFF));
    }

    [TestMethod]
    public void GetTcpListeners_FindsASocketThisProcessOpened()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var found = SystemPortScanner.GetTcpListeners().SingleOrDefault(l => l.Port == port);

            Assert.IsNotNull(found, $"Port {port} was not reported.");
            Assert.AreEqual(Environment.ProcessId, found.ProcessId);
            Assert.IsTrue(found.LocalOnly, "Bound to 127.0.0.1 only.");
            CollectionAssert.Contains(found.Addresses.ToList(), "127.0.0.1");
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void Classify_RecognisesOurProcessesByPathAndFlagsStrangersOnOurPorts()
    {
        var root = @"D:\Apps\Hamster Server";
        var config = new AppConfig();
        config.Apache.Port = 80;
        config.Apache.ExtraPorts = [new PortEntry { Port = 8080, Enabled = true }, new PortEntry { Port = 8081, Enabled = false }];
        config.Php.FcgiPort = 9000;
        ListeningPort L(int port, string name, string? path) => new(port, ["0.0.0.0"], 100 + port, name, path);

        var usage = SystemPortScanner.Classify(
        [
            L(80, "httpd", @"d:\apps\hamster server\apache\apache24\bin\httpd.exe"),
            L(9000, "php-cgi", @"d:\apps\hamster server\php\8.5.12-1\php-cgi.exe"),
            L(8080, "Skype", @"c:\program files\skype\skype.exe"),
            L(8081, "node", @"c:\program files\nodejs\node.exe"),
            L(3306, "mysqld", @"c:\xampp\mysql\bin\mysqld.exe"),
            // Another installation's httpd on our port is a conflict, not "ours".
            L(80, "httpd", @"c:\xampp\apache\bin\httpd.exe")
        ], config, root).ToList();

        Assert.AreEqual(PortOwnerKind.HamsterApache, usage[0].Owner);
        Assert.AreEqual("Apache HTTP (primary)", usage[0].Role);
        Assert.AreEqual(PortOwnerKind.HamsterPhp, usage[1].Owner);
        Assert.AreEqual(PortOwnerKind.Conflict, usage[2].Owner);
        Assert.AreEqual("Apache HTTP (extra)", usage[2].Role);
        Assert.AreEqual(PortOwnerKind.Other, usage[3].Owner, "8081 is configured but disabled, so it blocks nothing.");
        Assert.AreEqual(PortOwnerKind.Other, usage[4].Owner);
        Assert.AreEqual("MySQL / MariaDB", usage[4].WellKnownService);
        Assert.AreEqual(PortOwnerKind.Conflict, usage[5].Owner);
    }

    [TestMethod]
    public void HostsFile_ReadsLoopbackNamesAndTrustsDotLocalhost()
    {
        using var root = new TestDirectory();
        var path = Path.Combine(root.Path, "hosts");
        File.WriteAllText(path, """
            # Copyright (c) Microsoft Corp.
            127.0.0.1       localhost
            127.0.0.1   shop.test   www.shop.test   # the shop
            ::1             api.test
            192.168.1.20    nas.test
            #127.0.0.1      old.test
            """);

        var names = HostsFile.ReadLoopbackNames(path);

        foreach (var name in new[] { "shop.test", "WWW.SHOP.TEST", "api.test" })
            Assert.IsTrue(HostsFile.PointsHere(name, names), name);
        Assert.IsFalse(HostsFile.PointsHere("nas.test", names), "Points at another machine.");
        Assert.IsFalse(HostsFile.PointsHere("old.test", names), "Commented out.");
        Assert.IsFalse(HostsFile.PointsHere("blog.test", names));
        Assert.IsTrue(HostsFile.PointsHere("blog.localhost", names), "Browsers resolve *.localhost themselves.");
        Assert.AreEqual(0, HostsFile.ReadLoopbackNames(Path.Combine(root.Path, "missing")).Count);
    }
}
