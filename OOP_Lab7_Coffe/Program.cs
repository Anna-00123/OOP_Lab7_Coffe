using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CoffeeApp
{
    public static class Program
    {
        private static List<Coffee> coffees = new List<Coffee>();
        private static int maxCount;
        private static Random random = new Random();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static bool AddCoffee(List<Coffee> list, Coffee coffee, int maxCount)
        {
            CheckList(list);
            if (coffee == null)
                throw new ArgumentNullException(nameof(coffee), "Error: об'єкт не може бути null!");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Error: максимальна кількість має бути > 0!");
            if (ContainsCode(list, coffee.OrderCode))
                throw new ArgumentException($"Error: напій з кодом {coffee.OrderCode} вже є у списку!");
            if (list.Count >= maxCount)
                return false;

            list.Add(coffee);
            return true;
        }

        public static int AddCoffeeRange(List<Coffee> list, List<Coffee> items, int maxCount)
        {
            CheckList(list);
            if (items == null)
                throw new ArgumentNullException(nameof(items), "Error: список для додавання не може бути null!");
            if (items.Exists(c => c == null))
                throw new ArgumentNullException(nameof(items), "Error: список містить null!");
            for (int i = 0; i < items.Count; i++)
            {
                string code = items[i].OrderCode;
                if (ContainsCode(list, code) ||
                    items.FindIndex(i + 1, c => string.Equals(c.OrderCode, code, StringComparison.OrdinalIgnoreCase)) >= 0)
                    throw new ArgumentException($"Error: код {code} повторюється!");
            }
            if (list.Count + items.Count > maxCount)
                return 0;

            list.AddRange(items);
            return items.Count;
        }

        public static bool InsertCoffee(List<Coffee> list, int index, Coffee coffee, int maxCount)
        {
            CheckList(list);
            if (coffee == null)
                throw new ArgumentNullException(nameof(coffee), "Error: об'єкт не може бути null!");
            if (index < 0 || index > list.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Error: невірна позиція!");
            if (ContainsCode(list, coffee.OrderCode))
                throw new ArgumentException($"Error: напій з кодом {coffee.OrderCode} вже є у списку!");
            if (list.Count >= maxCount)
                return false;

            list.Insert(index, coffee);
            return true;
        }

        public static bool ContainsCode(List<Coffee> list, string code)
        {
            CheckList(list);
            if (string.IsNullOrWhiteSpace(code)) return false;
            string c = code.Trim();
            return list.Exists(x => string.Equals(x.OrderCode, c, StringComparison.OrdinalIgnoreCase));
        }

        public static Coffee FindByCode(List<Coffee> list, string code)
        {
            CheckList(list);
            if (string.IsNullOrWhiteSpace(code)) return null;
            string c = code.Trim();
            return list.Find(x => string.Equals(x.OrderCode, c, StringComparison.OrdinalIgnoreCase));
        }

        public static int FindIndexByCode(List<Coffee> list, string code)
        {
            CheckList(list);
            if (string.IsNullOrWhiteSpace(code)) return -1;
            string c = code.Trim();
            return list.FindIndex(x => string.Equals(x.OrderCode, c, StringComparison.OrdinalIgnoreCase));
        }

        public static List<Coffee> FindByName(List<Coffee> list, string namePart)
        {
            CheckList(list);
            if (string.IsNullOrWhiteSpace(namePart))
                throw new ArgumentNullException(nameof(namePart), "Error: рядок для пошуку не може бути порожнім!");
            string p = namePart.Trim();
            return list.FindAll(x => x.Name.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        public static List<Coffee> FindByType(List<Coffee> list, CoffeeType type)
        {
            CheckList(list);
            if (!Enum.IsDefined(typeof(CoffeeType), type))
                throw new ArgumentException("Error: невідомий тип кави!");
            return list.FindAll(x => x.Type == type);
        }

        public static List<Coffee> FindByPriceRange(List<Coffee> list, double min, double max)
        {
            CheckList(list);
            if (min < 0 || max < 0)
                throw new ArgumentOutOfRangeException(null, "Error: ціна не може бути від'ємною!");
            if (min > max)
                throw new ArgumentException("Error: мінімальна ціна більша за максимальну!");
            return list.FindAll(x => x.Price >= min && x.Price <= max);
        }

        public static Coffee FindMostExpensive(List<Coffee> list)
        {
            CheckList(list);
            if (list.Count == 0) return null;
            Coffee max = list[0];
            foreach (Coffee c in list)
                if (c.Price > max.Price) max = c;
            return max;
        }

        public static bool RemoveAtNumber(List<Coffee> list, int number)
        {
            CheckList(list);
            int index = number - 1;
            if (index < 0 || index >= list.Count)
                return false;
            list.RemoveAt(index);
            Coffee.DecreaseCounter(1);
            return true;
        }

        public static bool RemoveByCode(List<Coffee> list, string code)
        {
            Coffee found = FindByCode(list, code);
            if (found == null)
                return false;
            list.Remove(found);
            Coffee.DecreaseCounter(1);
            return true;
        }

        public static int RemoveAllByType(List<Coffee> list, CoffeeType type)
        {
            CheckList(list);
            if (!Enum.IsDefined(typeof(CoffeeType), type))
                throw new ArgumentException("Error: невідомий тип кави!");
            int removed = list.RemoveAll(x => x.Type == type);
            Coffee.DecreaseCounter(removed);
            return removed;
        }

        public static int ClearAll(List<Coffee> list)
        {
            CheckList(list);
            int removed = list.Count;
            list.Clear();
            Coffee.DecreaseCounter(removed);
            return removed;
        }

        public static void SortByPrice(List<Coffee> list, bool descending)
        {
            CheckList(list);
            list.Sort((a, b) => a.Price.CompareTo(b.Price));
            if (descending)
                list.Reverse();
        }

        public static void SortByName(List<Coffee> list)
        {
            CheckList(list);
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        public static Coffee CreateRandomCoffee(List<Coffee> list, Random rnd)
        {
            CheckList(list);
            if (rnd == null)
                throw new ArgumentNullException(nameof(rnd), "Error: генератор не може бути null!");

            string[] names = { "Ranok Blend", "Milk Dream", "Dark Night", "Caramel Sky", "Lviv Classic", "Velvet" };
            string code = GenerateUniqueCode(list, rnd);

            CoffeeType[] types = (CoffeeType[])Enum.GetValues(typeof(CoffeeType));
            double price = Math.Round(30 + rnd.NextDouble() * 120, 2);
            DateTime date = DateTime.Today.AddDays(-rnd.Next(0, 365));
            int volume = rnd.Next(3, 51) * 10;

            return new Coffee(code, names[rnd.Next(names.Length)], types[rnd.Next(types.Length)],
                              price, date, rnd.Next(2) == 1, volume);
        }

        public static string GenerateUniqueCode(List<Coffee> list, Random rnd)
        {
            CheckList(list);
            if (rnd == null)
                throw new ArgumentNullException(nameof(rnd), "Error: генератор не може бути null!");
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string code;
            do
            {
                var sb = new StringBuilder();
                for (int i = 0; i < 6; i++)
                    sb.Append(chars[rnd.Next(chars.Length)]);
                code = sb.ToString();
            } while (ContainsCode(list, code));
            return code;
        }

        public static bool TryParseInt(string input, int min, int max, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (!int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return false;
            if (v < min || v > max) return false;
            value = v;
            return true;
        }

        public static bool TryParseDouble(string input, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return false;
            if (double.IsNaN(v) || double.IsInfinity(v)) return false;
            value = v;
            return true;
        }

        public static bool TryParseCoffeeType(string input, out CoffeeType type)
        {
            type = CoffeeType.Espresso;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim();
            int count = Enum.GetValues(typeof(CoffeeType)).Length;

            if (int.TryParse(s, out int number))
            {
                if (number < 1 || number > count) return false;
                type = (CoffeeType)(number - 1);
                return true;
            }
            return Enum.TryParse(s, true, out type) && Enum.IsDefined(typeof(CoffeeType), type);
        }

        private static void CheckList(List<Coffee> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "Error: список не може бути null!");
        }

        public static bool TryGetFilePath(string input, string extension, out string path)
        {
            path = null;
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(extension))
                return false;

            string s = input.Trim();
            string ext = extension.StartsWith(".") ? extension : "." + extension;
            string currentExt = Path.GetExtension(s);

            if (currentExt.Length == 0)
                s += ext;
            else if (!string.Equals(currentExt, ext, StringComparison.OrdinalIgnoreCase))
                return false;

            string fileName = Path.GetFileName(s);
            if (Path.GetFileNameWithoutExtension(fileName).Trim().Length == 0)
                return false;
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName.IndexOfAny(new[] { '<', '>', ':', '"', '|', '?', '*' }) >= 0)
                return false;

            path = s;
            return true;
        }

        public static int SaveToFileCsv(List<Coffee> list, string path)
        {
            CheckList(list);
            CheckPath(path);

            List<string> lines = new List<string>(list.Count + 1) { Coffee.CsvHeader };
            foreach (Coffee item in list)
                lines.Add(item.ToString());

            File.WriteAllLines(path, lines, Encoding.UTF8);
            return list.Count;
        }

        public static List<Coffee> ReadFromFileCsv(string path, List<string> errors)
        {
            CheckPath(path);

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            List<Coffee> result = new List<Coffee>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                if (i == 0 && IsCsvHeader(line))
                    continue;

                try
                {
                    result.Add(Coffee.Parse(line));
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
                {
                    errors?.Add($"Рядок {i + 1} пропущено: {ex.Message}");
                }
            }
            return result;
        }

        public static bool IsCsvHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string normalized = line.Replace(" ", "").Trim('\uFEFF');
            return string.Equals(normalized, Coffee.CsvHeader, StringComparison.OrdinalIgnoreCase);
        }

        public static int SaveToFileJson(List<Coffee> list, string path)
        {
            CheckList(list);
            CheckPath(path);

            string json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
            return list.Count;
        }

        public static List<Coffee> ReadFromFileJson(string path)
        {
            CheckPath(path);

            string json = File.ReadAllText(path, Encoding.UTF8);
            int counterBefore = Coffee.Counter;
            try
            {
                List<Coffee> result = JsonSerializer.Deserialize<List<Coffee>>(json, JsonOptions);
                if (result == null)
                    return new List<Coffee>();
                if (result.Exists(c => c == null))
                    throw new JsonException("масив містить null");
                return result;
            }
            catch (Exception ex)
            {
                int created = Coffee.Counter - counterBefore;
                if (created > 0)
                    Coffee.DecreaseCounter(created);
                throw new FormatException("Error: JSON-файл пошкоджений або містить некоректні дані! " + ex.Message, ex);
            }
        }

        public static int AddLoadedCoffees(List<Coffee> list, List<Coffee> loaded, int maxCount, List<string> messages)
        {
            CheckList(list);
            if (loaded == null)
                throw new ArgumentNullException(nameof(loaded), "Error: список для додавання не може бути null!");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Error: максимальна кількість має бути > 0!");

            int added = 0;
            foreach (Coffee c in loaded)
            {
                if (c == null)
                    continue;
                if (list.Count >= maxCount)
                {
                    messages?.Add($"{c.OrderCode}: список заповнений – пропущено");
                    Coffee.DecreaseCounter(1);
                    continue;
                }
                if (ContainsCode(list, c.OrderCode))
                {
                    string oldCode = c.OrderCode;
                    c.OrderCode = GenerateUniqueCode(list, random);
                    messages?.Add($"{oldCode}: код уже є у списку – присвоєно новий код {c.OrderCode}");
                }
                AddCoffee(list, c, maxCount);
                added++;
            }
            return added;
        }

        private static void CheckPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), "Error: шлях до файлу не може бути порожнім!");
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("=== Облік кавових напоїв (List<Coffee>) ===");
            maxCount = ReadInt("Введіть максимальну кількість напоїв (1-100): ", 1, 100);

            while (true)
            {
                PrintMenu();
                int choice = ReadInt("Ваш вибір: ", 0, 9);
                Console.WriteLine();
                switch (choice)
                {
                    case 1: AddCoffeeMenu(); break;
                    case 2: ShowAllMenu(); break;
                    case 3: FindCoffeeMenu(); break;
                    case 4: DemonstrateBehavior(); break;
                    case 5: DeleteCoffeeMenu(); break;
                    case 6: DemonstrateStatic(); break;
                    case 7: SaveMenu(); break;
                    case 8: LoadMenu(); break;
                    case 9: ClearMenu(); break;
                    case 0:
                        Console.WriteLine("До побачення!");
                        return;
                }
                Console.WriteLine();
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"Напоїв у списку: {coffees.Count}/{maxCount}");
            Console.WriteLine("1 – Додати об'єкт");
            Console.WriteLine("2 – Переглянути всі об'єкти");
            Console.WriteLine("3 – Знайти об'єкт");
            Console.WriteLine("4 – Продемонструвати поведінку");
            Console.WriteLine("5 – Видалити об'єкт");
            Console.WriteLine("6 – Продемонструвати static-методи");
            Console.WriteLine("7 – Зберегти колекцію об'єктів у файлі");
            Console.WriteLine("8 – Зчитати колекцію об'єктів з файлу");
            Console.WriteLine("9 – Очистити колекцію об'єктів");
            Console.WriteLine("0 – Вийти з програми");
        }

        private static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (TryParseInt(Console.ReadLine(), min, max, out int value))
                    return value;
                Console.WriteLine($"Error: введіть ціле число від {min} до {max}!");
            }
        }

        private static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (TryParseDouble(Console.ReadLine(), out double value))
                    return value;
                Console.WriteLine("Error: введіть число!");
            }
        }

        private static CoffeeType ReadCoffeeType()
        {
            CoffeeType[] types = (CoffeeType[])Enum.GetValues(typeof(CoffeeType));
            for (int i = 0; i < types.Length; i++)
                Console.WriteLine($"  {i + 1} – {types[i]}");
            while (true)
            {
                Console.Write("Тип (номер або назва): ");
                if (TryParseCoffeeType(Console.ReadLine(), out CoffeeType type))
                    return type;
                Console.WriteLine("Error: невідомий тип кави!");
            }
        }

        private static void AddCoffeeMenu()
        {
            if (coffees.Count >= maxCount)
            {
                Console.WriteLine("Error: список заповнений!");
                return;
            }
            Console.WriteLine("1 – Ввести поля вручну");
            Console.WriteLine("2 – Ввести рядком (Код,Назва,Тип,Ціна,dd.MM.yyyy,Так/Ні,Об'єм)");
            Console.WriteLine("3 – Створити випадковий напій");
            int mode = ReadInt("Спосіб: ", 1, 3);

            Coffee coffee = null;
            try
            {
                switch (mode)
                {
                    case 1: coffee = ReadCoffeeFields(); break;
                    case 2:
                        Console.Write("Рядок: ");
                        if (!Coffee.TryParse(Console.ReadLine(), out coffee)) return;
                        break;
                    case 3: coffee = CreateRandomCoffee(coffees, random); break;
                }

                if (AddCoffee(coffees, coffee, maxCount))
                    Console.WriteLine("Напій додано: " + coffee);
                else
                {
                    Console.WriteLine("Error: список заповнений!");
                    Coffee.DecreaseCounter(1);
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                if (coffee != null) Coffee.DecreaseCounter(1);
            }
        }

        private static Coffee ReadCoffeeFields()
        {
            var c = new Coffee();
            SetWithRetry("Код (6 літер/цифр): ", s => c.OrderCode = s.Trim());
            SetWithRetry("Назва (2-30 символів): ", s => c.Name = s.Trim());
            c.Type = ReadCoffeeType();
            while (true)
            {
                try { c.Price = ReadDouble("Ціна (0; 500]: "); break; }
                catch (ArgumentException ex) { Console.WriteLine(ex.Message); }
            }
            while (true)
            {
                Console.Write("Дата обсмаження (dd.MM.yyyy): ");
                if (DateTime.TryParseExact(Console.ReadLine()?.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime d))
                {
                    try { c.RoastDate = d; break; }
                    catch (ArgumentException ex) { Console.WriteLine(ex.Message); }
                }
                else Console.WriteLine("Error: невірний формат дати!");
            }
            c.IsDecaf = ReadInt("Без кофеїну? (1 – так, 0 – ні): ", 0, 1) == 1;
            while (true)
            {
                try { c.VolumeMl = ReadInt("Об'єм (30-1000 мл): ", int.MinValue, int.MaxValue); break; }
                catch (ArgumentException ex) { Console.WriteLine(ex.Message); }
            }
            return new Coffee(c.OrderCode, c.Name, c.Type, c.Price, c.RoastDate, c.IsDecaf, c.VolumeMl);
        }

        private static void SetWithRetry(string prompt, Action<string> setter)
        {
            while (true)
            {
                Console.Write(prompt);
                try { setter(Console.ReadLine() ?? ""); return; }
                catch (ArgumentException ex) { Console.WriteLine(ex.Message); }
            }
        }

        private static void PrintHeader()
        {
            Console.WriteLine($"{"№",-4}{"Код",-8}{"Назва",-20}{"Тип",-12}{"Ціна",-10}{"Обсмажено",-14}{"Декаф",-7}{"Мл",-8}{"°C"}");
        }

        private static void PrintList(List<Coffee> list)
        {
            if (list.Count == 0)
            {
                Console.WriteLine("Нічого не знайдено.");
                return;
            }
            PrintHeader();
            for (int i = 0; i < list.Count; i++)
                Console.WriteLine($"{i + 1,-4}{list[i].ToTableRow()}");
        }

        private static void ShowAll()
        {
            if (coffees.Count == 0)
            {
                Console.WriteLine("Список порожній.");
                return;
            }
            PrintList(coffees);
            Console.WriteLine($"Усього створено об'єктів (Coffee.Counter): {Coffee.Counter}");
        }

        private static void FindCoffeeMenu()
        {
            if (coffees.Count == 0) { Console.WriteLine("Список порожній."); return; }
            Console.WriteLine("1 – за кодом");
            Console.WriteLine("2 – за назвою (фрагмент)");
            Console.WriteLine("3 – за типом");
            Console.WriteLine("4 – за діапазоном ціни");
            Console.WriteLine("5 – найдорожчий напій");
            int mode = ReadInt("Критерій: ", 1, 5);
            try
            {
                switch (mode)
                {
                    case 1:
                        Console.Write("Код: ");
                        string code = Console.ReadLine();
                        int idx = FindIndexByCode(coffees, code);
                        if (idx < 0) Console.WriteLine("Нічого не знайдено.");
                        else
                        {
                            PrintHeader();
                            Console.WriteLine($"{idx + 1,-4}{coffees[idx].ToTableRow()}");
                        }
                        break;
                    case 2:
                        Console.Write("Фрагмент назви: ");
                        PrintList(FindByName(coffees, Console.ReadLine()));
                        break;
                    case 3:
                        PrintList(FindByType(coffees, ReadCoffeeType()));
                        break;
                    case 4:
                        double min = ReadDouble("Мінімальна ціна: ");
                        double max = ReadDouble("Максимальна ціна: ");
                        PrintList(FindByPriceRange(coffees, min, max));
                        break;
                    case 5:
                        PrintList(new List<Coffee> { FindMostExpensive(coffees) });
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void DemonstrateBehavior()
        {
            if (coffees.Count == 0) { Console.WriteLine("Список порожній."); return; }
            Console.Write("Код напою: ");
            Coffee c = FindByCode(coffees, Console.ReadLine());
            if (c == null) { Console.WriteLine("Напій не знайдено."); return; }

            Console.WriteLine("1 – Reheat()            2 – Reheat(градуси)");
            Console.WriteLine("3 – Cool(градуси)       4 – ApplyDiscount(%)");
            Console.WriteLine("5 – ApplyDiscount(%, мін. ціна)");
            Console.WriteLine("6 – AddVolume(мл)       7 – AddVolume(мл, зі зміною ціни)");
            int action = ReadInt("Дія: ", 1, 7);
            try
            {
                switch (action)
                {
                    case 1: c.Reheat(); break;
                    case 2: c.Reheat(ReadInt("Градуси: ", -100, 100)); break;
                    case 3: c.Cool(ReadInt("Градуси: ", -100, 100)); break;
                    case 4: c.ApplyDiscount(ReadDouble("Знижка, %: ")); break;
                    case 5: c.ApplyDiscount(ReadDouble("Знижка, %: "), ReadDouble("Мін. ціна: ")); break;
                    case 6: c.AddVolume(ReadInt("Мл: ", -1000, 1000)); break;
                    case 7: c.AddVolume(ReadInt("Мл: ", -1000, 1000), true); break;
                }
                Console.WriteLine("Результат: " + c);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void DeleteCoffeeMenu()
        {
            if (coffees.Count == 0) { Console.WriteLine("Список порожній."); return; }
            Console.WriteLine("1 – за порядковим номером");
            Console.WriteLine("2 – за кодом");
            Console.WriteLine("3 – усі напої певного типу");
            Console.WriteLine("4 – очистити весь список");
            int mode = ReadInt("Спосіб: ", 1, 4);
            switch (mode)
            {
                case 1:
                    PrintList(coffees);
                    int number = ReadInt("Номер: ", int.MinValue, int.MaxValue);
                    Console.WriteLine(RemoveAtNumber(coffees, number) ? "Видалено." : "Error: немає напою з таким номером!");
                    break;
                case 2:
                    Console.Write("Код: ");
                    Console.WriteLine(RemoveByCode(coffees, Console.ReadLine()) ? "Видалено." : "Напій не знайдено.");
                    break;
                case 3:
                    int n = RemoveAllByType(coffees, ReadCoffeeType());
                    Console.WriteLine($"Видалено напоїв: {n}");
                    break;
                case 4:
                    Console.WriteLine($"Видалено напоїв: {ClearAll(coffees)}");
                    break;
            }
        }

        private static void ShowAllMenu()
        {
            if (coffees.Count == 0)
            {
                Console.WriteLine("Список порожній.");
                Console.WriteLine($"Coffee.Counter = {Coffee.Counter}");
                return;
            }
            Console.WriteLine("1 – у поточному порядку");
            Console.WriteLine("2 – відсортувати за ціною (зростання)");
            Console.WriteLine("3 – відсортувати за ціною (спадання)");
            Console.WriteLine("4 – відсортувати за назвою");
            int mode = ReadInt("Спосіб: ", 1, 4);
            switch (mode)
            {
                case 2: SortByPrice(coffees, false); break;
                case 3: SortByPrice(coffees, true); break;
                case 4: SortByName(coffees); break;
            }
            ShowAll();
        }

        private static string ReadFilePath(string extension)
        {
            Console.Write($"Введіть назву файлу (*{extension}): ");
            string input = Console.ReadLine();
            if (TryGetFilePath(input, extension, out string path))
                return path;
            Console.WriteLine($"Error: некоректна назва файлу! Очікується файл *{extension}.");
            return null;
        }

        private static void SaveMenu()
        {
            if (coffees.Count == 0)
            {
                Console.WriteLine("Колекція порожня – нічого зберігати.");
                return;
            }
            Console.WriteLine("1 – зберегти у файл *.csv");
            Console.WriteLine("2 – зберегти у файл *.json");
            int mode = ReadInt("Формат: ", 1, 2);
            string ext = mode == 1 ? ".csv" : ".json";

            string path = ReadFilePath(ext);
            if (path == null) return;

            if (File.Exists(path) &&
                ReadInt("Файл уже існує. Перезаписати? (1 – так, 0 – ні): ", 0, 1) == 0)
            {
                Console.WriteLine("Збереження скасовано.");
                return;
            }

            try
            {
                int saved = mode == 1 ? SaveToFileCsv(coffees, path) : SaveToFileJson(coffees, path);
                Console.WriteLine($"Збережено об'єктів: {saved}");
                Console.WriteLine($"Файл: {Path.GetFullPath(path)}");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                       ex is ArgumentException || ex is NotSupportedException)
            {
                Console.WriteLine("Error: не вдалося зберегти файл! " + ex.Message);
            }
        }

        private static void LoadMenu()
        {
            Console.WriteLine("1 – зчитати з файлу *.csv");
            Console.WriteLine("2 – зчитати з файлу *.json");
            int mode = ReadInt("Формат: ", 1, 2);
            string ext = mode == 1 ? ".csv" : ".json";

            string path = ReadFilePath(ext);
            if (path == null) return;
            if (!File.Exists(path))
            {
                Console.WriteLine($"Error: файл {Path.GetFullPath(path)} не знайдено!");
                return;
            }

            List<Coffee> loaded;
            try
            {
                if (mode == 1)
                {
                    List<string> errors = new List<string>();
                    loaded = ReadFromFileCsv(path, errors);
                    foreach (string e in errors)
                        Console.WriteLine("  " + e);
                    Console.WriteLine($"Десеріалізовано об'єктів: {loaded.Count} (пропущено некоректних рядків: {errors.Count})");
                }
                else
                {
                    loaded = ReadFromFileJson(path);
                    Console.WriteLine($"Десеріалізовано об'єктів: {loaded.Count}");
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Десеріалізовано об'єктів: 0");
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine("Error: не вдалося прочитати файл! " + ex.Message);
                return;
            }

            List<string> messages = new List<string>();
            int added = AddLoadedCoffees(coffees, loaded, maxCount, messages);
            foreach (string m in messages)
                Console.WriteLine("  " + m);
            Console.WriteLine($"Додано до колекції об'єктів: {added}. Усього в колекції: {coffees.Count}/{maxCount}");
        }

        private static void ClearMenu()
        {
            if (coffees.Count == 0)
            {
                Console.WriteLine("Колекція вже порожня.");
                return;
            }
            int removed = ClearAll(coffees);
            Console.WriteLine($"Колекцію очищено. Видалено об'єктів: {removed}");
            Console.WriteLine($"Coffee.Counter = {Coffee.Counter}");
        }

        private static void DemonstrateStatic()
        {
            Console.WriteLine($"Coffee.Counter = {Coffee.Counter}");
            Console.WriteLine($"Coffee.DefaultTemperatureC = {Coffee.DefaultTemperatureC}");
            int t = ReadInt("Нова температура за замовчуванням (20-95, інше – скидання до 75): ",
                            int.MinValue, int.MaxValue);
            Coffee.DefaultTemperatureC = t;
            Console.WriteLine($"DefaultTemperatureC = {Coffee.DefaultTemperatureC}");

            int c = ReadInt("Температура в °C для переведення: ", -273, 1000);
            Console.WriteLine($"{c} °C = {Coffee.CelsiusToFahrenheit(c):F1} °F");

            double p100 = ReadDouble("Ціна за 100 мл: ");
            int vol = ReadInt("Об'єм, мл: ", 1, 10000);
            Console.WriteLine($"Ціна порції: {Coffee.CalculatePrice(p100, vol):F2} грн");

            Console.Write("Код для перевірки IsValidOrderCode: ");
            string code = Console.ReadLine();
            Console.WriteLine($"IsValidOrderCode = {Coffee.IsValidOrderCode(code)}");
        }
    }
}