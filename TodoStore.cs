using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TodoApp;

public class TodoStore
{
    private static string GetPath(string fileName)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "bin");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return Path.Combine(folder, fileName);
    }

    private static readonly string TodoPath = GetPath("todos.json");
    private static readonly string DailyPath = GetPath("daily.json");
    private static readonly string BoardsPath = GetPath("boards.json");
    private static readonly string CounterPath = GetPath("counters.json");
    public static List<(string text, bool done)> Load()
    {
        if (!File.Exists(TodoPath)) return new();
        try
        {
            var raw = JsonSerializer.Deserialize<List<TodoItem>>(File.ReadAllText(TodoPath));
            return raw?.Select(x => (x.Text, x.Done)).ToList() ?? new();
        }
        catch { return new(); }
    }

    public static void Save(List<(string text, bool done)> todos)
    {
        var raw = todos.Select(x => new TodoItem { Text = x.text, Done = x.done }).ToList();
        File.WriteAllText(TodoPath, JsonSerializer.Serialize(raw, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static List<(string text, bool done)> LoadDaily()
    {
        if (!File.Exists(DailyPath)) return new();
        try
        {
            var file = JsonSerializer.Deserialize<DailyFile>(File.ReadAllText(DailyPath));
            if (file == null) return new();

            var items = file.Items.Select(x => (x.Text, x.Done)).ToList();

            if (file.LastChecked.Date < DateTime.Today)
            {
                items = items.Select(x => (x.Text, false)).ToList();
                SaveDaily(items);
            }

            return items;
        }
        catch { return new(); }
    }
    public static void SaveDaily(List<(string text, bool done)> todos)
    {
        var file = new DailyFile
        {
            LastChecked = DateTime.Today,
            Items = todos.Select(x => new TodoItem { Text = x.text, Done = x.done }).ToList()
        };
        File.WriteAllText(DailyPath, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static List<BoardState> LoadBoards()
    {
        if (!File.Exists(BoardsPath)) return new();
        try
        {
            var raw = JsonSerializer.Deserialize<List<BoardModel>>(File.ReadAllText(BoardsPath));
            if (raw == null) return new();

            return raw.Select(b => new BoardState
            {
                Name = b.Name,
                Items = b.Items.Select(i => (i.Text, i.Done)).ToList()
            }).ToList();
        }
        catch { return new(); }
    }

    public static void SaveBoards(List<BoardState> boards)
    {
        var raw = boards.Select(b => new BoardModel
        {
            Name = b.Name,
            Items = b.Items.Select(i => new TodoItem { Text = i.text, Done = i.done }).ToList()
        }).ToList();
        File.WriteAllText(BoardsPath, JsonSerializer.Serialize(raw, new JsonSerializerOptions { WriteIndented = true }));
    }
    public static List<CounterState> LoadCounters()
    {
        if (!File.Exists(CounterPath)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<CounterState>>(File.ReadAllText(CounterPath)) ?? new();
        }
        catch { return new(); }
    }

    public static void SaveCounters(List<CounterState> counters)
    {
        File.WriteAllText(CounterPath, JsonSerializer.Serialize(counters, new JsonSerializerOptions { WriteIndented = true }));
    }
    public class TodoItem
    {
        public string Text { get; set; } = "";
        public bool Done { get; set; }
    }
    private class DailyFile
    {
        public DateTime LastChecked { get; set; }
        public List<TodoItem> Items { get; set; } = new();
    }
    private class BoardModel
    {
        public string Name { get; set; } = "";
        public List<TodoItem> Items { get; set; } = new();
    }
}

public class BoardState
{
    public string Name { get; set; } = "";
    public List<(string text, bool done)> Items { get; set; } = new();
    public string InputText = "";
    public bool ShowAdd = false;
    public int EditIdx = -1;
    public string EditStr = "";
}
public class CounterState
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}