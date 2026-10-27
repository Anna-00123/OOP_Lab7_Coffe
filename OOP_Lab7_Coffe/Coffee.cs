using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoffeeApp
{
    public enum CoffeeType
    {
        Espresso,
        Americano,
        Cappuccino,
        Latte,
        Mocha,
        FlatWhite
    }

    public class DateOnlyJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string text = reader.GetString();
            if (!DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                throw new JsonException($"Error: дата \"{text}\" має бути у форматі {Format}!");
            return date;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }

    public class Coffee
    {
        private string orderCode;
        private string name;
        private CoffeeType type;
        private double price;
        private DateTime roastDate;
        private bool isDecaf;
        private int volumeMl;
        private int temperatureC;

        private static int counter;
        private static int defaultTemperatureC;
        private static readonly DateTime MinRoastDate = new DateTime(2020, 1, 1);

        public const string CsvHeader = "Code,Name,Type,Price,RoastDate,IsDecaf,VolumeMl";

        static Coffee()
        {
            counter = 0;
            defaultTemperatureC = 75;
        }

        [JsonIgnore]
        public string Currency { get; private set; } = "грн";

        public static int Counter
        {
            get { return counter; }
        }

        public static int DefaultTemperatureC
        {
            get { return defaultTemperatureC; }
            set
            {
                if (value < 20 || value > 95)
                    defaultTemperatureC = 75;
                else
                    defaultTemperatureC = value;
            }
        }

        [JsonPropertyName("code")]
        public string OrderCode
        {
            get { return orderCode; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(null, "Error: код замовлення не може бути порожнім!");
                if (!IsValidOrderCode(value))
                    throw new ArgumentException("Error: код замовлення має складатися з 6 літер або цифр!");
                orderCode = value;
            }
        }

        [JsonPropertyName("title")]
        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(null, "Error: назва не може бути порожньою!");
                if (value.Length < 2 || value.Length > 30)
                    throw new ArgumentOutOfRangeException(null, "Error: довжина назви має бути від 2 до 30 символів!");
                if (value.Contains(','))
                    throw new ArgumentException("Error: назва не може містити кому!");
                name = value;
            }
        }

        [JsonPropertyName("kind")]
        [JsonConverter(typeof(JsonStringEnumConverter<CoffeeType>))]
        public CoffeeType Type
        {
            get { return type; }
            set
            {
                if (!Enum.IsDefined(typeof(CoffeeType), value))
                    throw new ArgumentException("Error: невідомий тип кави!");
                type = value;
            }
        }

        [JsonPropertyName("price_uah")]
        public double Price
        {
            get { return price; }
            set
            {
                if (value <= 0 || value > 500)
                    throw new ArgumentOutOfRangeException(null, "Error: ціна має бути в діапазоні (0; 500] грн!");
                price = value;
            }
        }

        [JsonPropertyName("roasted")]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime RoastDate
        {
            get { return roastDate; }
            set
            {
                if (value.Date < MinRoastDate || value.Date > DateTime.Today)
                    throw new ArgumentOutOfRangeException(null,
                        "Error: дата обсмаження має бути від 01.01.2020 до сьогодні!");
                roastDate = value.Date;
            }
        }

        [JsonPropertyName("decaf")]
        public bool IsDecaf
        {
            get { return isDecaf; }
            set { isDecaf = value; }
        }

        [JsonPropertyName("volume_ml")]
        public int VolumeMl
        {
            get { return volumeMl; }
            set
            {
                if (value < 30 || value > 1000)
                    throw new ArgumentOutOfRangeException(null, "Error: об'єм має бути від 30 до 1000 мл!");
                volumeMl = value;
            }
        }

        [JsonIgnore]
        public int TemperatureC
        {
            get { return temperatureC; }
            private set { temperatureC = value; }
        }

        [JsonIgnore]
        public double PricePer100Ml
        {
            get { return Price * 100 / VolumeMl; }
        }

        public Coffee()
        {
            TemperatureC = DefaultTemperatureC;
        }

        public Coffee(string orderCode, string name)
            : this(orderCode, name, CoffeeType.Espresso, 50.0, DateTime.Today, false, 200)
        {
        }

        public Coffee(string orderCode, string name, CoffeeType type)
            : this(orderCode, name, type, 50.0, DateTime.Today, false, 200)
        {
        }

        public Coffee(string orderCode, string name, CoffeeType type, double price)
            : this(orderCode, name, type, price, DateTime.Today, false, 200)
        {
        }

        [JsonConstructor]
        public Coffee(string orderCode, string name, CoffeeType type, double price,
                      DateTime roastDate, bool isDecaf, int volumeMl)
        {
            OrderCode = orderCode;
            Name = name;
            Type = type;
            Price = price;
            RoastDate = roastDate;
            IsDecaf = isDecaf;
            VolumeMl = volumeMl;
            TemperatureC = DefaultTemperatureC;
            counter++;
        }

        public static bool IsLettersOrDigits(string text)
        {
            foreach (char c in text)
                if (!char.IsLetterOrDigit(c))
                    return false;
            return true;
        }

        public static bool IsValidOrderCode(string code)
        {
            return code != null && code.Length == 6 && IsLettersOrDigits(code);
        }

        public static double CelsiusToFahrenheit(int celsius)
        {
            return celsius * 9.0 / 5.0 + 32;
        }

        public static double CalculatePrice(double pricePer100Ml, int volumeMl)
        {
            return pricePer100Ml * volumeMl / 100;
        }

        public static void DecreaseCounter(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(null, "Error: кількість не може бути від'ємною!");
            counter -= count;
            if (counter < 0) counter = 0;
        }

        public static Coffee Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(null, "Error: рядок не може бути порожнім!");

            string[] parts = s.Split(',');
            if (parts.Length != 7)
                throw new FormatException(
                    "Error: невірний формат рядка – очікується 7 значень через кому " +
                    "(Код,Назва,Тип,Ціна,Дата,БезКофеїну,Об'єм)!");

            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim();

            string typeText = parts[2];
            if (typeText.Length == 0 || !char.IsLetter(typeText[0]) ||
                !Enum.TryParse(typeText, true, out CoffeeType type) ||
                !Enum.IsDefined(typeof(CoffeeType), type))
                throw new FormatException("Error: невідомий тип кави!");

            if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double price))
                throw new FormatException("Error: ціна має бути числом (роздільник – крапка)!");

            if (!DateTime.TryParseExact(parts[4], "dd.MM.yyyy", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime date))
                throw new FormatException("Error: дата має бути у форматі dd.MM.yyyy!");

            bool isDecaf = ParseBool(parts[5]);

            if (!int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume))
                throw new FormatException("Error: об'єм має бути цілим числом!");

            return new Coffee(parts[0], parts[1], type, price, date, isDecaf, volume);
        }

        public static bool TryParse(string s, out Coffee coffee)
        {
            coffee = null;
            try
            {
                coffee = Parse(s);
                return true;
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return false;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static bool ParseBool(string s)
        {
            string v = s.Trim().ToLower();
            if (v == "так" || v == "yes" || v == "true") return true;
            if (v == "ні" || v == "no" || v == "false") return false;
            throw new FormatException("Error: ознака «без кофеїну» має бути Так/Ні!");
        }

        private static void CheckDiscountPercent(double percent)
        {
            if (percent <= 0 || percent >= 100)
                throw new ArgumentOutOfRangeException(null, "Error: знижка має бути в діапазоні (0; 100)%!");
        }

        public void Reheat()
        {
            Console.WriteLine("  [Виклик Reheat()]");
            Reheat(10);
        }

        public void Reheat(int degrees)
        {
            int old = TemperatureC;
            TemperatureC = Clamp(TemperatureC + degrees, 20, 95);
            Console.WriteLine($"  -> Температура: {old} °C -> {TemperatureC} °C");
        }

        public void Cool(int degrees)
        {
            int old = TemperatureC;
            TemperatureC = Clamp(TemperatureC - degrees, 20, 95);
            Console.WriteLine($"  -> Охолодження: {old} °C -> {TemperatureC} °C");
        }

        public void ApplyDiscount(double percent)
        {
            Console.WriteLine("  [Виклик ApplyDiscount(double)]");
            CheckDiscountPercent(percent);
            double oldPrice = Price;
            Price = Price - Price * percent / 100;
            Console.WriteLine($"  -> Знижка {percent}% застосована. Ціна: {oldPrice:F2} -> {Price:F2} {Currency}");
        }

        public void ApplyDiscount(double percent, double minPrice)
        {
            Console.WriteLine("  [Виклик ApplyDiscount(double, double)]");
            CheckDiscountPercent(percent);
            double oldPrice = Price;
            double newPrice = Price - Price * percent / 100;
            double floor = Math.Min(minPrice, oldPrice);
            if (newPrice < floor) newPrice = floor;
            Price = newPrice;
            Console.WriteLine($"  -> Знижка {percent}% (мін. ціна {minPrice:F2}). Ціна: {oldPrice:F2} -> {Price:F2} {Currency}");
        }

        public void AddVolume(int ml)
        {
            AddVolume(ml, false);
        }

        public void AddVolume(int ml, bool increasePrice)
        {
            int oldVolume = VolumeMl;
            VolumeMl = Clamp(VolumeMl + ml, 30, 1000);
            if (increasePrice)
            {
                double newPrice = Price * VolumeMl / oldVolume;
                Price = Math.Min(newPrice, 500.0);
            }
            Console.WriteLine($"  -> Об'єм: {oldVolume} мл -> {VolumeMl} мл, ціна: {Price:F2} {Currency}");
        }

        public string ToTableRow()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0,-8}{1,-20}{2,-12}{3,-10:F2}{4,-14}{5,-7}{6,-8}{7}",
                OrderCode, Name, Type, Price, RoastDate.ToString("dd.MM.yyyy"),
                IsDecaf ? "Так" : "Ні", VolumeMl, TemperatureC);
        }

        public override string ToString()
        {
            return string.Join(",",
                OrderCode,
                Name,
                Type.ToString(),
                Price.ToString("F2", CultureInfo.InvariantCulture),
                RoastDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                IsDecaf ? "Так" : "Ні",
                VolumeMl.ToString(CultureInfo.InvariantCulture));
        }
    }
}