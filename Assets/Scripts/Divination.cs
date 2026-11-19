using System;

// 占卜: picks a primary element from a birth date (公历), no hour needed.
// Uses the day stem (日主 / 日干) of the 八字 day pillar:
// 甲乙 木, 丙丁 火, 戊己 土, 庚辛 金, 壬癸 水.
public static class Divination
{
    static readonly string Stems = "甲乙丙丁戊己庚辛壬癸";
    static readonly string Branches = "子丑寅卯辰巳午未申酉戌亥";

    // Parses a date like "1995-08-17", "1995/8/17" or "1995.8.17". Returns false if it isn't a date.
    public static bool TryParse(string text, out DateTime birth)
    {
        birth = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim().Replace('/', '-').Replace('.', '-');
        return DateTime.TryParseExact(text, new[] { "yyyy-M-d", "yyyy-MM-dd" },
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out birth);
    }

    // Year, month and day typed into separate boxes. Returns false if they don't make a real date.
    public static bool TryParse(string year, string month, string day, out DateTime birth)
    {
        birth = default;
        if (!int.TryParse(year, out int y) || !int.TryParse(month, out int m) || !int.TryParse(day, out int d)) return false;
        if (y < 1 || y > 9999 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return false;
        birth = new DateTime(y, m, d);
        return true;
    }

    // Birthday (month, day) plus age: the birth year is worked out from today's date
    // (one year earlier if this year's birthday hasn't come yet). Returns false if it isn't a real date.
    public static bool TryParseWithAge(int age, string month, string day, DateTime today, out DateTime birth)
    {
        birth = default;
        if (age < 0 || !int.TryParse(month, out int m) || !int.TryParse(day, out int d)) return false;
        if (m < 1 || m > 12 || d < 1 || d > 31) return false;
        int y = today.Year - age;
        if (m > today.Month || (m == today.Month && d > today.Day)) y--;
        if (y < 1 || y > 9999 || d > DateTime.DaysInMonth(y, m)) return false;
        birth = new DateTime(y, m, d);
        return true;
    }

    // Index 0..59 of the day pillar (0 = 甲子).
    public static int DayPillar(DateTime birth)
    {
        var day = birth.Date;
        // 1949-10-01 was a 甲子 day.
        long days = (long)(day - new DateTime(1949, 10, 1)).TotalDays;
        return (int)(((days % 60) + 60) % 60);
    }

    public static string DayPillarName(DateTime birth)
    {
        int i = DayPillar(birth);
        return Stems[i % 10].ToString() + Branches[i % 12];
    }

    // Element order in the enum is 木火土金水, two stems each.
    public static Element DayMaster(DateTime birth)
    {
        return (Element)(DayPillar(birth) % 10 / 2);
    }
}
