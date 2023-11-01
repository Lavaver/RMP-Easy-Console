using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class JsonParser
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-easyhelp")
        {
            ShowHelpInfo();
            return;
        }

        else if (args.Length > 0 && args[0] == "-newjson")
        {
            AddNewJson();
            return;
        }

        while (true)
        {
            Console.WriteLine("请输入包含要解析的JSON内容的文件路径（输入 Q 回车退出程序，程序启动前输入 -easyhelp 或 -newjson 参数可打开简易使用说明或添加新 JSON 文件）：");
            string filePath = Console.ReadLine();

            if (filePath.ToLower() == "q")
            {
                break;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonContent);

                Console.WriteLine("\n解析结果：");

                Console.WriteLine("编号\t| 内容或值\t| 存在于");
                Console.WriteLine("----------------------------------------");

                int index = 1;

                foreach (KeyValuePair<string, JToken> property in jsonObject)
                {
                    string value = GetJsonValue(property.Value);

                    // 如果值中包含 Unicode 编码，则进行转换
                    if (value.Contains("\\u"))
                    {
                        value = DecodeUnicode(value);
                    }

                    Console.WriteLine("{0}\t| {1}\t| {2}", index, value, property.Key);
                    index++;
                }

                Console.WriteLine("----------------------------------------");
                Console.WriteLine("文件内共计 {0} 个变量、布尔值或字符串，其中出现最多值的前两个内容为：", jsonObject.Count);

                Dictionary<string, int> valueCounts = GetTopValueCounts(jsonObject);

                if (valueCounts.Count > 0)
                {
                    var topValues = valueCounts.OrderByDescending(kv => kv.Value).Take(2);

                    foreach (var kv in topValues)
                    {
                        Console.WriteLine("- {0}: {1}", kv.Key, kv.Value);
                    }
                }
                else
                {
                    Console.WriteLine("[无可识别的值]");
                }

                Console.WriteLine("----------------------------------------");

                // 生成日志文件
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string logFileName = $"json_{DateTime.Now.ToString("yyyyMMdd")}_{GetRandomString(8)}.log";
                string logFilePath = Path.Combine(desktopPath, logFileName);

                using (StreamWriter writer = File.CreateText(logFilePath))
                {
                    writer.WriteLine($"解析时间：{DateTime.Now.ToString()}");
                    writer.WriteLine($"解析文件路径：{filePath}");

                    // 写入解析结果
                    writer.WriteLine("\n解析结果：");

                    writer.WriteLine("编号\t| 内容或值\t| 存在于");
                    writer.WriteLine("----------------------------------------");

                    index = 1;

                    foreach (KeyValuePair<string, JToken> property in jsonObject)
                    {
                        string value = GetJsonValue(property.Value);

                        // 如果值中包含 Unicode 编码，则进行转换
                        if (value.Contains("\\u"))
                        {
                            value = DecodeUnicode(value);
                        }

                        writer.WriteLine("{0}\t| {1}\t| {2}", index, value, property.Key);
                        index++;
                    }

                    writer.WriteLine("----------------------------------------");
                    writer.WriteLine($"文件内共计 {jsonObject.Count} 个变量、布尔值或字符串，其中出现最多值的前两个内容为：");

                    valueCounts = GetTopValueCounts(jsonObject);

                    if (valueCounts.Count > 0)
                    {
                        var topValues = valueCounts.OrderByDescending(kv => kv.Value).Take(2);

                        foreach (var kv in topValues)
                        {
                            writer.WriteLine("- {0}: {1}", kv.Key, kv.Value);

                        }
                    }
                    else
                    {
                        writer.WriteLine("[无可识别的值]");

                    }

                    writer.WriteLine("----------------------------------------");
                    writer.WriteLine("注：生成的日志文件对于之后纠错、开发等有重要用途，请妥善保存。");
                    writer.WriteLine("日志生成于 RMP Easy Console 。XLBC Network © 2023，保留所有权利。");
                }

                Console.WriteLine($"\n日志文件已生成：{logFilePath}");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("指定的文件路径不存在，请重新输入。");
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine("无法解析提供的JSON内容。错误消息：\n" + ex.Message);
            }

            Console.WriteLine();
        }


        return;
    }

    static void ShowHelpInfo()
    {
        Console.WriteLine("简易使用");
        Console.WriteLine("------------------------");
        Console.WriteLine("RMP Easy Console 是一款极为方便查询 JSON 文件内信息的软件。通过它可以敏捷开发、高效调试与除错任何 JSON 文件中的潜在问题。");
        Console.WriteLine("接下来将协助你如何快速上手，三步学会该软件：");
        Console.WriteLine("1. 将要解析的 JSON 文件直接拖入到窗口中");
        Console.WriteLine("2. 按下回车");
        Console.WriteLine("3. 解析完成后会在桌面生成一个日志文件。它很重要，包含了本次解析此处显示的所有内容，请妥善保存");
    }

    static string GetJsonValue(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                return "[Object]";
            case JTokenType.Array:
                return "[Array]";
            case JTokenType.Integer:
                return token.Value<int>().ToString();
            case JTokenType.Float:
                return token.Value<float>().ToString();
            case JTokenType.String:
                return token.Value<string>();
            case JTokenType.Boolean:
                return token.Value<bool>().ToString();
            case JTokenType.Null:
                return "null";
            default:
                return "[Unknown]";
        }
    }

    static string DecodeUnicode(string input)
    {
        StringBuilder output = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '\\' && i + 5 < input.Length && input[i + 1] == 'u')
            {
                string hexValue = input.Substring(i + 2, 4);
                int intValue = Convert.ToInt32(hexValue, 16);

                // 如果编码使用 UTF-16，则需要将代码点拆分成两个 16 位值
                if (intValue >= 0xD800 && intValue <= 0xDBFF && i + 11 < input.Length && input.Substring(i + 6, 2) == "\\u" && input.Substring(i + 8, 4).Length == 4)
                {
                    int highSurrogate = intValue;
                    int lowSurrogate = Convert.ToInt32(input.Substring(i + 8, 4), 16);

                    if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
                    {
                        intValue = ((highSurrogate - 0xD800) << 10) + (lowSurrogate - 0xDC00) + 0x10000;
                        i += 5;
                    }
                }
                else
                {
                    i += 5;
                }

                output.Append(char.ConvertFromUtf32(intValue));
            }
            else
            {
                output.Append(input[i]);
            }
        }

        return output.ToString();
    }

    static Dictionary<string, int> GetTopValueCounts(JObject jsonObject)
    {
        Dictionary<string, int> valueCounts = new Dictionary<string, int>();

        foreach (KeyValuePair<string, JToken> property in jsonObject)
        {
            string value = GetJsonValue(property.Value);

            if (valueCounts.ContainsKey(value))
            {
                valueCounts[value]++;
            }
            else
            {
                valueCounts[value] = 1;
            }
        }

        return valueCounts;
    }

    static void AddNewJson()
    {
        Console.WriteLine("快速添加 JSON");
        Console.WriteLine("----------------------------");
        Console.WriteLine("[1] 添加新的 JSON 文件至桌面并按引导写入值");
        Console.WriteLine("[2] 添加新的 JSON 文件至其他位置并按引导写入值");
        Console.WriteLine("[3] 仅在桌面添加空 JSON 文件");
        Console.WriteLine("[4] 仅在其他位置添加空 JSON 文件");
        Console.WriteLine("[5] 离开程序");

        Console.Write("请选择选项（1-5）：");
        string option = Console.ReadLine();

        if (option == "1")
        {
            AddNewJsonToDesktop();
        }
        else if (option == "2")
        {
            AddNewJsonToCustomPath();
        }
        else if (option == "3")
        {
            AddEmptyJsonToDesktop();
        }
        else if (option == "4")
        {
            AddEmptyJsonToCustomPath();
        }
        else if (option == "5")
        {
            Console.WriteLine("程序已退出。");
        }
        else
        {
            Console.WriteLine("无效的选项。");
        }
    }

    static void AddNewJsonToDesktop()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        Console.Write("请输入新 JSON 的名称（不含 .json 后缀）：");
        string fileName = Console.ReadLine();

        string filePath = Path.Combine(desktopPath, $"{fileName}.json");

        AddValuesToJsonFile(filePath);
    }

    static void AddNewJsonToCustomPath()
    {
        Console.Write("请输入要保存的文件夹路径：");
        string folderPath = Console.ReadLine();

        Console.Write("请输入新 JSON 的名称（不含 .json 后缀）：");
        string fileName = Console.ReadLine();

        string filePath = Path.Combine(folderPath, $"{fileName}.json");

        AddValuesToJsonFile(filePath);
    }

    static void AddEmptyJsonToDesktop()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        Console.Write("请输入新 JSON 的名称（不含 .json 后缀）：");
        string fileName = Console.ReadLine();

        string filePath = Path.Combine(desktopPath, $"{fileName}.json");

        CreateEmptyJsonFile(filePath);
    }

    static void AddEmptyJsonToCustomPath()
    {
        Console.Write("请输入要保存的文件夹路径：");
        string folderPath = Console.ReadLine();

        Console.Write("请输入新 JSON 的名称（不含 .json 后缀）：");
        string fileName = Console.ReadLine();

        string filePath = Path.Combine(folderPath, $"{fileName}.json");

        CreateEmptyJsonFile(filePath);
    }

    static void CreateEmptyJsonFile(string filePath)
    {
        JObject jsonObject = new JObject();
        try
        {
            File.WriteAllText(filePath, jsonObject.ToString());

            Console.WriteLine($"JSON 文件已创建并保存至路径：{filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("创建文件时出现错误：" + ex.Message);
        }
    }

    static void AddValuesToJsonFile(string filePath)
    {
        JObject jsonObject = new JObject();

        while (true)
        {
            Console.WriteLine("请选择要添加的值类别");
            Console.WriteLine("-------------------------------");
            Console.WriteLine("[1] 字符串");
            Console.WriteLine("[2] 布尔值");
            Console.WriteLine("[3] 纯数字");
            Console.WriteLine("[4] 保存并退出");

            Console.Write("请输入选项（1-4）：");
            string option = Console.ReadLine();

            if (option == "1")
            {
                Console.Write("输入值名称：");
                string valueName = Console.ReadLine();

                Console.Write("输入字符：");
                string value = Console.ReadLine();

                jsonObject[valueName] = value;

                Console.WriteLine("值已添加成功！");
            }
            else if (option == "2")
            {
                Console.Write("输入值名称：");
                string valueName = Console.ReadLine();

                Console.Write("这个值是否开启（true/false）：");
                bool value = Convert.ToBoolean(Console.ReadLine());

                jsonObject[valueName] = value;

                Console.WriteLine("值已添加成功！");
            }
            else if (option == "3")
            {
                Console.Write("输入值名称：");
                string valueName = Console.ReadLine();

                Console.Write("输入数字：");
                int value = Convert.ToInt32(Console.ReadLine());

                jsonObject[valueName] = value;

                Console.WriteLine("值已添加成功！");
            }
            else if (option == "4")
            {
                try
                {
                    File.WriteAllText(filePath, jsonObject.ToString());

                    Console.WriteLine($"JSON 文件已保存至路径：{filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("保存文件时出现错误：" + ex.Message);
                }

                return;
            }
            else
            {
                Console.WriteLine("无效的选项，请重新输入。");
            }
        }
    }

    // 生成随机字符串的方法
    static string GetRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}