using CoffeeApp;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace CoffeeApp.Tests
{
    [TestClass]
    public class SerializationTests
    {
        private const int MaxCount = 10;
        private List<Coffee> coffees;
        private Coffee latte;
        private Coffee mocha;
        private Coffee espresso;
        private string tempDir;
        private CultureInfo originalCulture;
        private TextWriter originalOut;
        private StringWriter consoleOutput;

        [TestInitialize]
        public void Setup()
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            originalOut = Console.Out;
            consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            latte = new Coffee("AB1234", "Ranok Blend", CoffeeType.Latte, 85.50,
                               new DateTime(2024, 3, 15), false, 250);
            mocha = new Coffee("CD5678", "Milk Dream", CoffeeType.Mocha, 60.00,
                               new DateTime(2024, 5, 1), true, 300);
            espresso = new Coffee("EF9012", "Dark Night", CoffeeType.Espresso, 40.00,
                                  new DateTime(2023, 11, 20), false, 30);
            coffees = new List<Coffee> { latte, mocha, espresso };

            tempDir = Path.Combine(Path.GetTempPath(), "CoffeeTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Console.SetOut(originalOut);
            consoleOutput.Dispose();
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }

        private string TempFile(string name)
        {
            return Path.Combine(tempDir, name);
        }

        private string WriteTempFile(string name, string content)
        {
            string path = TempFile(name);
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        private static void AssertSameData(Coffee expected, Coffee actual)
        {
            Assert.AreEqual(expected.OrderCode, actual.OrderCode);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Price, actual.Price, 1e-9);
            Assert.AreEqual(expected.RoastDate, actual.RoastDate);
            Assert.AreEqual(expected.IsDecaf, actual.IsDecaf);
            Assert.AreEqual(expected.VolumeMl, actual.VolumeMl);
        }

        [TestMethod]
        [DataRow("test", ".csv", true, "test.csv")]
        [DataRow("test.csv", ".csv", true, "test.csv")]
        [DataRow("  data.CSV ", ".csv", true, "data.CSV")]
        [DataRow("test", "json", true, "test.json")]
        [DataRow("test.json", ".json", true, "test.json")]
        [DataRow("test.txt", ".csv", false, null)]
        [DataRow("test.json", ".csv", false, null)]
        [DataRow(".csv", ".csv", false, null)]
        [DataRow("te?st", ".csv", false, null)]
        [DataRow("", ".csv", false, null)]
        [DataRow("   ", ".csv", false, null)]
        [DataRow(null, ".csv", false, null)]
        public void TryGetFilePath_ReturnsExpected(string input, string ext, bool expectedResult, string expectedPath)
        {
            bool result = Program.TryGetFilePath(input, ext, out string path);

            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedPath, path);
        }

        [TestMethod]
        public void SaveToFileCsv_ThreeObjects_WritesHeaderAndThreeLines()
        {
            string path = TempFile("test.csv");

            int saved = Program.SaveToFileCsv(coffees, path);

            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(3, saved);
            Assert.AreEqual(4, lines.Length);
            Assert.AreEqual(Coffee.CsvHeader, lines[0]);
            Assert.AreEqual(latte.ToString(), lines[1]);
            Assert.AreEqual(espresso.ToString(), lines[3]);
        }

        [TestMethod]
        public void SaveToFileCsv_EmptyList_WritesOnlyHeader()
        {
            string path = TempFile("empty.csv");

            int saved = Program.SaveToFileCsv(new List<Coffee>(), path);

            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(0, saved);
            Assert.AreEqual(1, lines.Length);
            Assert.AreEqual(Coffee.CsvHeader, lines[0]);
        }

        [TestMethod]
        public void SaveToFileCsv_ExistingFile_IsOverwritten()
        {
            string path = WriteTempFile("old.csv", "old content\nline2\nline3\nline4\nline5\nline6");

            Program.SaveToFileCsv(new List<Coffee> { mocha }, path);

            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual(mocha.ToString(), lines[1]);
        }

        [TestMethod]
        public void SaveToFileCsv_NullList_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.SaveToFileCsv(null, TempFile("a.csv")));
        }

        [TestMethod]
        [DataRow((string)null)]
        [DataRow("")]
        [DataRow("   ")]
        public void SaveToFileCsv_EmptyPath_ThrowsArgumentNullException(string path)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.SaveToFileCsv(coffees, path));
        }

        [TestMethod]
        public void SaveToFileCsv_DirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
        {
            string path = Path.Combine(tempDir, "no_such_dir", "a.csv");

            Assert.ThrowsExactly<DirectoryNotFoundException>(() => Program.SaveToFileCsv(coffees, path));
        }

        [TestMethod]
        public void ReadFromFileCsv_SavedFile_ReturnsSameData()
        {
            string path = TempFile("roundtrip.csv");
            Program.SaveToFileCsv(coffees, path);
            List<string> errors = new List<string>();

            List<Coffee> actual = Program.ReadFromFileCsv(path, errors);

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(0, errors.Count);
            for (int i = 0; i < coffees.Count; i++)
                AssertSameData(coffees[i], actual[i]);
        }

        [TestMethod]
        public void ReadFromFileCsv_ReturnsNewObjects_AndIncreasesCounter()
        {
            string path = TempFile("counter.csv");
            Program.SaveToFileCsv(coffees, path);
            int counterBefore = Coffee.Counter;

            List<Coffee> actual = Program.ReadFromFileCsv(path, null);

            Assert.AreNotSame(latte, actual[0]);
            Assert.AreEqual(counterBefore + 3, Coffee.Counter);
        }

        [TestMethod]
        public void ReadFromFileCsv_BrokenLines_SkipsThemAndReportsErrors()
        {
            string content =
                Coffee.CsvHeader + "\n" +
                "AB1234,Ranok Blend,Latte,85.50,15.03.2024,Ні,250\n" +
                "CD5678,Milk Dream,Mocha,60.00,01.05.2024,Так\n" +
                "EF9012,Dark Night,Espresso,40.00,20.11.2023,Ні,30\n" +
                "GH3456,Velvet,Latte,abc,01.01.2024,Ні,200\n" +
                "IJ7890,Velvet,Frappe,50,01.01.2024,Ні,200\n" +
                "KL1234,Velvet,Latte,50,2024-01-01,Ні,200\n" +
                "MN5678,Velvet,Latte,50,01.01.2024,Може,200\n" +
                "OP9012,Velvet,Latte,50,01.01.2024,Ні,2.5\n" +
                "QR3456,Velvet,Latte,900,01.01.2024,Ні,200\n" +
                "BAD,Velvet,Latte,50,01.01.2024,Ні,200\n" +
                "\n" +
                "ST7890,Caramel Sky,Cappuccino,70.00,10.10.2024,Так,350\n";
            string path = WriteTempFile("broken.csv", content);
            List<string> errors = new List<string>();

            List<Coffee> actual = Program.ReadFromFileCsv(path, errors);

            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(8, errors.Count);
            CollectionAssert.AreEqual(new[] { "AB1234", "EF9012", "ST7890" },
                                      actual.ConvertAll(c => c.OrderCode));
            StringAssert.StartsWith(errors[0], "Рядок 3");
        }

        [TestMethod]
        public void ReadFromFileCsv_BrokenLines_DoNotIncreaseCounter()
        {
            string content = Coffee.CsvHeader + "\n" +
                             "CD5678,Milk Dream,Mocha,60.00,01.05.2024,Так\n" +
                             "QR3456,Velvet,Latte,900,01.01.2024,Ні,200\n";
            string path = WriteTempFile("broken2.csv", content);
            int counterBefore = Coffee.Counter;

            List<Coffee> actual = Program.ReadFromFileCsv(path, null);

            Assert.AreEqual(0, actual.Count);
            Assert.AreEqual(counterBefore, Coffee.Counter);
        }

        [TestMethod]
        public void ReadFromFileCsv_OnlyHeader_ReturnsEmptyList()
        {
            string path = WriteTempFile("header.csv", Coffee.CsvHeader + "\n");

            List<Coffee> actual = Program.ReadFromFileCsv(path, null);

            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void ReadFromFileCsv_WithoutHeader_ReadsAllLines()
        {
            string content = "AB1234,Ranok Blend,Latte,85.50,15.03.2024,Ні,250\n" +
                             "CD5678,Milk Dream,Mocha,60.00,01.05.2024,Так,300\n";
            string path = WriteTempFile("noheader.csv", content);

            List<Coffee> actual = Program.ReadFromFileCsv(path, null);

            Assert.AreEqual(2, actual.Count);
        }

        [TestMethod]
        public void ReadFromFileCsv_FileNotFound_ThrowsFileNotFoundException()
        {
            Assert.ThrowsExactly<FileNotFoundException>(() => Program.ReadFromFileCsv(TempFile("missing.csv"), null));
        }

        [TestMethod]
        [DataRow((string)null)]
        [DataRow("")]
        public void ReadFromFileCsv_EmptyPath_ThrowsArgumentNullException(string path)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.ReadFromFileCsv(path, null));
        }

        [TestMethod]
        [DataRow("Code,Name,Type,Price,RoastDate,IsDecaf,VolumeMl", true)]
        [DataRow("code,name,type,price,roastdate,isdecaf,volumeml", true)]
        [DataRow("Code, Name, Type, Price, RoastDate, IsDecaf, VolumeMl", true)]
        [DataRow("AB1234,Ranok Blend,Latte,85.50,15.03.2024,Ні,250", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void IsCsvHeader_ReturnsExpected(string line, bool expected)
        {
            Assert.AreEqual(expected, Program.IsCsvHeader(line));
        }

        [TestMethod]
        public void SaveToFileJson_ThreeObjects_WritesArrayOfThree()
        {
            string path = TempFile("test.json");

            int saved = Program.SaveToFileJson(coffees, path);

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.AreEqual(3, saved);
            Assert.AreEqual(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.AreEqual(3, doc.RootElement.GetArrayLength());
        }

        [TestMethod]
        public void SaveToFileJson_UsesRenamedProperties()
        {
            string path = TempFile("names.json");

            Program.SaveToFileJson(new List<Coffee> { latte }, path);

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement item = doc.RootElement[0];
            Assert.AreEqual("AB1234", item.GetProperty("code").GetString());
            Assert.AreEqual("Ranok Blend", item.GetProperty("title").GetString());
            Assert.AreEqual("Latte", item.GetProperty("kind").GetString());
            Assert.AreEqual(85.5, item.GetProperty("price_uah").GetDouble(), 1e-9);
            Assert.AreEqual("2024-03-15", item.GetProperty("roasted").GetString());
            Assert.IsFalse(item.GetProperty("decaf").GetBoolean());
            Assert.AreEqual(250, item.GetProperty("volume_ml").GetInt32());
        }

        [TestMethod]
        [DataRow("OrderCode")]
        [DataRow("Name")]
        [DataRow("Currency")]
        [DataRow("TemperatureC")]
        [DataRow("PricePer100Ml")]
        public void SaveToFileJson_IgnoredOrOldNames_AreNotWritten(string propertyName)
        {
            string path = TempFile("ignored.json");

            Program.SaveToFileJson(new List<Coffee> { latte }, path);

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.IsFalse(doc.RootElement[0].TryGetProperty(propertyName, out _));
            Assert.AreEqual(7, CountProperties(doc.RootElement[0]));
        }

        private static int CountProperties(JsonElement element)
        {
            int n = 0;
            foreach (JsonProperty _ in element.EnumerateObject()) n++;
            return n;
        }

        [TestMethod]
        public void SaveToFileJson_EmptyList_WritesEmptyArray()
        {
            string path = TempFile("empty.json");

            int saved = Program.SaveToFileJson(new List<Coffee>(), path);

            Assert.AreEqual(0, saved);
            Assert.AreEqual("[]", File.ReadAllText(path).Trim('\uFEFF').Trim());
        }

        [TestMethod]
        public void SaveToFileJson_NullList_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.SaveToFileJson(null, TempFile("a.json")));
        }

        [TestMethod]
        [DataRow((string)null)]
        [DataRow("")]
        public void SaveToFileJson_EmptyPath_ThrowsArgumentNullException(string path)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.SaveToFileJson(coffees, path));
        }

        [TestMethod]
        public void ReadFromFileJson_SavedFile_ReturnsSameData()
        {
            string path = TempFile("roundtrip.json");
            Program.SaveToFileJson(coffees, path);

            List<Coffee> actual = Program.ReadFromFileJson(path);

            Assert.AreEqual(3, actual.Count);
            for (int i = 0; i < coffees.Count; i++)
                AssertSameData(coffees[i], actual[i]);
        }

        [TestMethod]
        public void ReadFromFileJson_IncreasesCounterByCount()
        {
            string path = TempFile("counter.json");
            Program.SaveToFileJson(coffees, path);
            int counterBefore = Coffee.Counter;

            Program.ReadFromFileJson(path);

            Assert.AreEqual(counterBefore + 3, Coffee.Counter);
        }

        [TestMethod]
        public void ReadFromFileJson_NotSavedProperties_GetDefaultValues()
        {
            string path = TempFile("defaults.json");
            Coffee.DefaultTemperatureC = 75;
            latte.Reheat(10);
            Program.SaveToFileJson(new List<Coffee> { latte }, path);

            Coffee actual = Program.ReadFromFileJson(path)[0];

            Assert.AreEqual(85, latte.TemperatureC);
            Assert.AreEqual(75, actual.TemperatureC);
            Assert.AreEqual("грн", actual.Currency);
        }

        [TestMethod]
        public void ReadFromFileJson_EmptyArray_ReturnsEmptyList()
        {
            string path = WriteTempFile("empty.json", "[]");

            List<Coffee> actual = Program.ReadFromFileJson(path);

            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        public void ReadFromFileJson_NullLiteral_ReturnsEmptyList()
        {
            string path = WriteTempFile("null.json", "null");

            List<Coffee> actual = Program.ReadFromFileJson(path);

            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("not a json")]
        [DataRow("[{\"code\":\"AB1234\"")]
        [DataRow("{\"code\":\"AB1234\"}")]
        [DataRow("[null]")]
        [DataRow("[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":\"abc\",\"roasted\":\"2024-03-15\",\"decaf\":false,\"volume_ml\":250}]")]
        [DataRow("[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Frappe\",\"price_uah\":85.5,\"roasted\":\"2024-03-15\",\"decaf\":false,\"volume_ml\":250}]")]
        [DataRow("[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":85.5,\"roasted\":\"15.03.2024\",\"decaf\":false,\"volume_ml\":250}]")]
        public void ReadFromFileJson_CorruptedFile_ThrowsFormatException(string content)
        {
            string path = WriteTempFile("bad.json", content);

            Assert.ThrowsExactly<FormatException>(() => Program.ReadFromFileJson(path));
        }

        [TestMethod]
        [DataRow("[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":900,\"roasted\":\"2024-03-15\",\"decaf\":false,\"volume_ml\":250}]")]
        [DataRow("[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":85.5,\"roasted\":\"2024-03-15\",\"decaf\":false}]")]
        [DataRow("[{\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":85.5,\"roasted\":\"2024-03-15\",\"decaf\":false,\"volume_ml\":250}]")]
        public void ReadFromFileJson_InvalidOrMissingValue_ThrowsFormatException(string content)
        {
            string path = WriteTempFile("invalid.json", content);

            Assert.ThrowsExactly<FormatException>(() => Program.ReadFromFileJson(path));
        }

        [TestMethod]
        public void ReadFromFileJson_SecondObjectInvalid_CounterRestored()
        {
            string content =
                "[{\"code\":\"AB1234\",\"title\":\"Ranok Blend\",\"kind\":\"Latte\",\"price_uah\":85.5,\"roasted\":\"2024-03-15\",\"decaf\":false,\"volume_ml\":250}," +
                " {\"code\":\"CD5678\",\"title\":\"Milk Dream\",\"kind\":\"Mocha\",\"price_uah\":-1,\"roasted\":\"2024-05-01\",\"decaf\":true,\"volume_ml\":300}]";
            string path = WriteTempFile("half.json", content);
            int counterBefore = Coffee.Counter;

            Assert.ThrowsExactly<FormatException>(() => Program.ReadFromFileJson(path));

            Assert.AreEqual(counterBefore, Coffee.Counter);
        }

        [TestMethod]
        public void ReadFromFileJson_FileNotFound_ThrowsFileNotFoundException()
        {
            Assert.ThrowsExactly<FileNotFoundException>(() => Program.ReadFromFileJson(TempFile("missing.json")));
        }

        [TestMethod]
        public void AddLoadedCoffees_NonEmptyList_AppendsNewObjects()
        {
            List<Coffee> loaded = new List<Coffee>
            {
                new Coffee("ZZ0001", "Velvet", CoffeeType.Americano),
                new Coffee("ZZ0002", "Velvet", CoffeeType.Americano)
            };
            List<string> messages = new List<string>();

            int added = Program.AddLoadedCoffees(coffees, loaded, MaxCount, messages);

            Assert.AreEqual(2, added);
            Assert.AreEqual(5, coffees.Count);
            Assert.AreSame(latte, coffees[0]);
            Assert.AreEqual("ZZ0002", coffees[4].OrderCode);
            Assert.AreEqual(0, messages.Count);
        }

        [TestMethod]
        public void AddLoadedCoffees_DuplicateCodes_AddedWithNewUniqueCodes()
        {
            string path = TempFile("dup.csv");
            Program.SaveToFileCsv(coffees, path);
            List<Coffee> loaded = Program.ReadFromFileCsv(path, null);
            int counterAfterRead = Coffee.Counter;
            List<string> messages = new List<string>();

            int added = Program.AddLoadedCoffees(coffees, loaded, MaxCount, messages);

            Assert.AreEqual(3, added);
            Assert.AreEqual(6, coffees.Count);
            Assert.AreEqual(3, messages.Count);
            StringAssert.Contains(messages[0], "присвоєно новий код");
            Assert.AreEqual("AB1234", latte.OrderCode);
            Assert.AreNotEqual("AB1234", coffees[3].OrderCode);
            Assert.AreEqual("Ranok Blend", coffees[3].Name);
            Assert.IsTrue(Coffee.IsValidOrderCode(coffees[3].OrderCode));
            Assert.AreEqual(6, new HashSet<string>(coffees.ConvertAll(c => c.OrderCode.ToUpperInvariant())).Count);
            Assert.AreEqual(counterAfterRead, Coffee.Counter);
        }

        [TestMethod]
        public void AddLoadedCoffees_DuplicatesInsideLoaded_AllCodesUnique()
        {
            List<Coffee> loaded = new List<Coffee>
            {
                new Coffee("ZZ0001", "Velvet"),
                new Coffee("zz0001", "Velvet")
            };

            int added = Program.AddLoadedCoffees(coffees, loaded, MaxCount, null);

            Assert.AreEqual(2, added);
            Assert.AreEqual(5, new HashSet<string>(coffees.ConvertAll(c => c.OrderCode.ToUpperInvariant())).Count);
        }

        [TestMethod]
        public void GenerateUniqueCode_ReturnsValidCodeNotInList()
        {
            string code = Program.GenerateUniqueCode(coffees, new Random(3));

            Assert.IsTrue(Coffee.IsValidOrderCode(code));
            Assert.IsFalse(Program.ContainsCode(coffees, code));
        }

        [TestMethod]
        public void AddLoadedCoffees_ListBecomesFull_RestSkipped()
        {
            List<Coffee> loaded = new List<Coffee>
            {
                new Coffee("ZZ0001", "Velvet"),
                new Coffee("ZZ0002", "Velvet"),
                new Coffee("ZZ0003", "Velvet")
            };
            List<string> messages = new List<string>();
            int counterBefore = Coffee.Counter;

            int added = Program.AddLoadedCoffees(coffees, loaded, 4, messages);

            Assert.AreEqual(1, added);
            Assert.AreEqual(4, coffees.Count);
            Assert.AreEqual(2, messages.Count);
            StringAssert.Contains(messages[1], "заповнений");
            CollectionAssert.DoesNotContain(coffees, loaded[2]);
            Assert.AreEqual(counterBefore - 2, Coffee.Counter);
        }

        [TestMethod]
        public void AddLoadedCoffees_EmptyLoaded_ReturnsZero()
        {
            int added = Program.AddLoadedCoffees(coffees, new List<Coffee>(), MaxCount, null);

            Assert.AreEqual(0, added);
            Assert.AreEqual(3, coffees.Count);
        }

        [TestMethod]
        public void AddLoadedCoffees_NullArguments_Throw()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.AddLoadedCoffees(null, new List<Coffee>(), MaxCount, null));
            Assert.ThrowsExactly<ArgumentNullException>(() => Program.AddLoadedCoffees(coffees, null, MaxCount, null));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Program.AddLoadedCoffees(coffees, new List<Coffee>(), 0, null));
        }

        [TestMethod]
        public void Scenario_TestCases1to5_FromMethodicalGuide()
        {
            Coffee.DecreaseCounter(Coffee.Counter);
            List<Coffee> list = new List<Coffee>();
            Random rnd = new Random(7);
            const int max = 20;

            for (int i = 0; i < 5; i++)
                Program.AddCoffee(list, Program.CreateRandomCoffee(list, rnd), max);
            string csv = TempFile("test.csv");
            Program.SaveToFileCsv(list, csv);
            Assert.AreEqual(6, File.ReadAllLines(csv).Length);

            string[] lines = File.ReadAllLines(csv);
            lines[2] = lines[2].Substring(0, lines[2].LastIndexOf(','));
            string[] parts = lines[4].Split(',');
            parts[3] = "abc";
            lines[4] = string.Join(",", parts);
            File.WriteAllLines(csv, lines);
            List<string> errors = new List<string>();
            List<Coffee> fromCsv = Program.ReadFromFileCsv(csv, errors);
            Assert.AreEqual(3, fromCsv.Count);
            Assert.AreEqual(2, errors.Count);
            Assert.AreEqual(3, Program.AddLoadedCoffees(list, fromCsv, max, null));
            Assert.AreEqual(8, list.Count);
            Assert.AreEqual(8, Coffee.Counter);

            string json = TempFile("test.json");
            Program.SaveToFileJson(list, json);
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(json)))
                Assert.AreEqual(8, doc.RootElement.GetArrayLength());

            List<Coffee> fromJson = Program.ReadFromFileJson(json);
            Assert.AreEqual(8, fromJson.Count);
            Assert.AreEqual(8, Program.AddLoadedCoffees(list, fromJson, max, null));
            Assert.AreEqual(16, list.Count);
            Assert.AreEqual(16, Coffee.Counter);

            Program.ClearAll(list);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, Coffee.Counter);
        }

        [TestMethod]
        public void SerializationMethods_DoNotWriteToConsole()
        {
            string csv = TempFile("quiet.csv");
            string json = TempFile("quiet.json");
            File.WriteAllText(TempFile("bad.csv"), "bad line\n");

            Program.SaveToFileCsv(coffees, csv);
            Program.ReadFromFileCsv(csv, null);
            Program.ReadFromFileCsv(TempFile("bad.csv"), null);
            Program.SaveToFileJson(coffees, json);
            Program.ReadFromFileJson(json);
            Program.AddLoadedCoffees(coffees, Program.ReadFromFileJson(json), MaxCount, null);

            Assert.AreEqual(string.Empty, consoleOutput.ToString());
        }
    }
}