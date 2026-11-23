//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using System.Reflection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using NUnit.Framework;
using RetroSchematic.Mcp;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The MCP tool layer: the same edit rules as the window (read-only by default, refusals
    /// with a reason), a new design built from nothing and saved, and every tool registered.
    /// </summary>
    [TestFixture]
    public class McpToolTests
    {
        private string _scratch = "";

        [SetUp]
        public void SetUp()
        {
            _scratch = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mcp-" + TestContext.CurrentContext.Test.Name + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_scratch))
                File.Delete(_scratch);
        }

        [Test]
        public void OpenedSheet_IsReadOnly_UntilEditModeIsOn()
        {
            SheetTools t = Tools();
            Assert.That(t.OpenSheet(DemoSheetLayoutTests.Demo), Does.Contain("read-only"));
            McpException ex = Assert.Throws<McpException>(() => t.Delete("part", "1A"))!;
            Assert.That(ex.Message, Does.Contain("read-only"));
            Assert.That(t.ListParts(), Does.Contain("1A 7400"));
            t.SetEditMode(true);
            Assert.That(t.Delete("part", "1A"), Is.EqualTo("Deleted part 1A."));
            Assert.That(t.ListParts(), Does.Not.Contain("1A 7400"));
            t.Undo();
            Assert.That(t.ListParts(), Does.Contain("1A 7400"));
        }

        [Test]
        public void NewDesign_FromNothing_PlaceConnectMoveSave()
        {
            SheetTools t = Tools();
            t.NewSheet("T1", "Tool test");
            Assert.That(t.PlacePart("7400", 10, 10), Is.EqualTo("Placed 7400 as 1A."));
            Assert.That(t.PlacePart("7474", 80, 10, "2B"), Is.EqualTo("Placed 7474 as 2B."));
            Assert.That(t.ConnectPins("1A.3", "2B.2", "D"), Is.EqualTo("Connected 1A.3 and 2B.2 on D."));
            Assert.That(t.ConnectPins("1A.6", "2B.3"), Does.EndWith("on N1."));
            t.MovePart("2B", 100, 20);
            Assert.That(t.ListParts(), Does.Contain("2B 7474 at (100, 20) mm"));
            Assert.That(t.ListNets(), Does.Contain("D: 1A.3 2B.2"));
            Assert.Throws<McpException>(() => t.SaveSheet(), "a new sheet needs a path the first time");
            Assert.That(t.SaveSheet(_scratch), Does.StartWith("Saved"));
            Netlist back = NetlistReader.ReadFile(_scratch);
            Assert.That(back.Parts.Count, Is.EqualTo(2));
            Assert.That(back.Nets.Count, Is.EqualTo(2));
        }

        [Test]
        public void Refusals_SayWhy_AndChangeNothing()
        {
            SheetTools t = Tools();
            Assert.Throws<McpException>(() => t.ListParts(), "nothing open yet");
            t.OpenSheet(DemoSheetLayoutTests.Demo, edit: true);
            McpException ex = Assert.Throws<McpException>(() => t.ConnectPins("1A.1", "1A.2"))!;
            Assert.That(ex.Message, Does.StartWith("Refused: ").And.Contain("one signal only"));
            Assert.Throws<McpException>(() => t.Delete("gate", "1A/1"));
            Assert.Throws<McpException>(() => t.PlacePart("NOSUCHCHIP", 0, 0));
            Assert.That(t.Editor!.CanUndo, Is.False);
            Assert.Throws<McpException>(() => t.OpenSheet(Path.Combine(TestContext.CurrentContext.WorkDirectory, "no-such-sheet.json")));
        }

        [Test]
        public void EveryPublicToolMethod_IsAnMcpTool_WithADescription()
        {
            MethodInfo[] methods = typeof(SheetTools).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int tools = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsSpecialName)
                    continue; // property getters
                McpServerToolAttribute? a = methods[i].GetCustomAttribute<McpServerToolAttribute>();
                Assert.That(a, Is.Not.Null, methods[i].Name + " is public but not a tool");
                Assert.That(methods[i].GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(), Is.Not.Null, methods[i].Name);
                tools++;
            }
            Assert.That(tools, Is.EqualTo(13));
            Assert.That(typeof(SheetTools).GetCustomAttribute<McpServerToolTypeAttribute>(), Is.Not.Null);
        }

        [Test]
        public void TheDemoSheet_IsFoundFromTheTestFolder()
        {
            Assert.That(SheetFiles.FindDemo(TestContext.CurrentContext.TestDirectory), Is.EqualTo(DemoSheetLayoutTests.Demo));
        }

        private static SheetTools Tools() => new(TestContext.CurrentContext.TestDirectory);
    }
}
