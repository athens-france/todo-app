using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TodoApp;

GL? gl = null;
ImGuiController? imgui = null;

var todos = TodoStore.Load();
var daily = TodoStore.LoadDaily();
var boards = TodoStore.LoadBoards();
var counters = TodoStore.LoadCounters();

var inputText = "";
var dailyInputText = "";
bool showInput = false;
bool showDailyInput = false;

int todoEditIndex = -1;
string todoEditText = "";

int dailyEditIndex = -1;
string dailyEditText = "";

string newBoardName = "";
bool showAddBoard = false;

string newCounterName = "";
bool showAddCounter = false;

bool isDeleteMode = false;

uint motivationalImage = 0;
Vector2 motivationalImageSize = new Vector2(150, 150);


var window = Window.Create(WindowOptions.Default with
{
    Title = "Todo App",
    Size = new Silk.NET.Maths.Vector2D<int>(900, 600)
    WindowBorder = WindowBorder.Resizable
});

window.Load += () =>
{
    gl = window.CreateOpenGL();
    var input = window.CreateInput();
    imgui = new ImGuiController(gl, window, input);

    // load random image
    string dir = "motivationalimages";
    if (Directory.Exists(dir))
    {
        var files = Directory.GetFiles(dir, "*.png");
        if (files.Length > 0)
        {
            var random = new Random();
            motivationalImage = LoadTexture(files[random.Next(files.Length)]);
        }
    }
};

window.Resize += size =>
{
    gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
};

window.Render += dt =>
{
    gl!.ClearColor(0.1f, 0.1f, 0.13f, 1f);
    gl!.Clear(ClearBufferMask.ColorBufferBit);
    imgui!.Update((float)dt);

    // root ui
    var viewport = ImGui.GetMainViewport();
    ImGui.SetNextWindowPos(viewport.Pos);
    ImGui.SetNextWindowSize(viewport.Size);

    ImGui.Begin("##root", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

    // delete mode stuff
    float deleteBtnWidth = 120f;
    ImGui.SetCursorPosX(ImGui.GetWindowWidth() - deleteBtnWidth - 10f);

    bool wasDeleteMode = isDeleteMode;

    if (wasDeleteMode)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 0.8f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1f, 0.4f, 0.4f, 1f));
    }

    if (ImGui.Button(isDeleteMode ? "are ya done?" : "remove a task", new Vector2(deleteBtnWidth, 0)))
    {
        isDeleteMode = !isDeleteMode;
    }

    if (wasDeleteMode)
    {
        ImGui.PopStyleColor(3);
    }

    ImGui.Spacing();

    if (ImGui.BeginTabBar("##tabs"))
    {
        if (ImGui.BeginTabItem("todo"))
        {
            DrawList(todos, ref inputText, ref showInput, ref todoEditIndex, ref todoEditText, () => TodoStore.Save(todos), "todo", isDeleteMode);
            ImGui.EndTabItem();
        }

        // Daily Tab
        if (ImGui.BeginTabItem("daily"))
        {
            ImGui.TextDisabled("these reset every day, but never disappear! this is your daily schedule.");
            ImGui.Spacing();
            DrawList(daily, ref dailyInputText, ref showDailyInput, ref dailyEditIndex, ref dailyEditText, () => TodoStore.SaveDaily(daily), "daily", isDeleteMode);
            ImGui.EndTabItem();
        }

        // Boards Tab
        if (ImGui.BeginTabItem("boards"))
        {
            if (ImGui.Button(showAddBoard ? "cancel" : "+ add board"))
            {
                showAddBoard = !showAddBoard;
                newBoardName = "";
            }

            if (showAddBoard)
            {
                ImGui.SetNextItemWidth(300f);
                ImGui.SetKeyboardFocusHere();
                if (ImGui.InputTextWithHint("##newboard", "come up with something nice....", ref newBoardName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (!string.IsNullOrWhiteSpace(newBoardName.Trim()))
                    {
                        boards.Add(new BoardState { Name = newBoardName.Trim() });
                        newBoardName = "";
                        showAddBoard = false;
                        TodoStore.SaveBoards(boards);
                    }
                }
            }

            ImGui.Separator();
            ImGui.BeginChild("BoardsArea", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

            for (int i = 0; i < boards.Count; i++)
            {
                var board = boards[i];
                ImGui.BeginChild($"BoardPanel_{i}", new Vector2(350, 0), ImGuiChildFlags.Border);

                ImGui.TextDisabled(board.Name.ToUpper());

                if (isDeleteMode)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 30);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 0.6f));
                    bool deletedBrd = ImGui.Button($"X##delbrd{i}");
                    ImGui.PopStyleColor();
                    if (deletedBrd)
                    {
                        boards.RemoveAt(i);
                        TodoStore.SaveBoards(boards);
                        ImGui.EndChild();
                        break;
                    }
                }
                
                ImGui.Separator();
                DrawList(board.Items, ref board.InputText, ref board.ShowAdd, ref board.EditIdx, ref board.EditStr, () => TodoStore.SaveBoards(boards), $"brd{i}", isDeleteMode);
                ImGui.EndChild();
                ImGui.SameLine();
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("counters"))
        {
            if (ImGui.Button(showAddCounter ? "cancel" : "+ add counter"))
            {
                showAddCounter = !showAddCounter;
                newCounterName = "";
            }

            if (showAddCounter)
            {
                ImGui.SetNextItemWidth(300f);
                ImGui.SetKeyboardFocusHere();
                if (ImGui.InputTextWithHint("##newcounter", "what's something you wanna track..?", ref newCounterName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (!string.IsNullOrWhiteSpace(newCounterName.Trim()))
                    {
                        counters.Add(new CounterState { Name = newCounterName.Trim(), Value = 0 });
                        newCounterName = "";
                        showAddCounter = false;
                        TodoStore.SaveCounters(counters);
                    }
                }
            }

            ImGui.Separator();

            for (int i = 0; i < counters.Count; i++)
            {
                var counter = counters[i];

                if (isDeleteMode)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 0.6f));
                    bool deletedCtr = ImGui.Button($"X##delctr{i}");
                    ImGui.PopStyleColor();
                    if (deletedCtr)
                    {
                        counters.RemoveAt(i);
                        TodoStore.SaveCounters(counters);
                        continue;
                    }
                    ImGui.SameLine();
                }

                if (ImGui.Button($"+##up{i}")) { counter.Value++; TodoStore.SaveCounters(counters); }
                ImGui.SameLine();
                if (ImGui.Button($"-##down{i}")) { counter.Value--; TodoStore.SaveCounters(counters); }
                ImGui.SameLine();
                ImGui.Text($"{counter.Name} - {counter.Value}");
            }
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    var containerSize = new Vector2(170, 190);
    var windowSize = ImGui.GetWindowSize();

    ImGui.SetCursorPos(new Vector2(windowSize.X - containerSize.X - 10, windowSize.Y - containerSize.Y - 10));
    
    if (ImGui.BeginChild("MotivationContainer", containerSize, ImGuiChildFlags.Border) && motivationalImage != 0)
    {
        ImGui.Text("you can do it!");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.SetCursorPosX((containerSize.X - motivationalImageSize.X) / 2);
        ImGui.Image((IntPtr)motivationalImage, motivationalImageSize);
        ImGui.EndChild();
    }
    else if (motivationalImage == 0)
    {
        ImGui.EndChild();
    }

    ImGui.End();
    imgui!.Render();
};

window.Closing += () => imgui?.Dispose();
window.Run();

void DrawList(List<(string text, bool done)> list, ref string input, ref bool showAdd, ref int editIdx, ref string editStr, Action onSave, string idSuffix, bool isDeleteMode)
{
    if (ImGui.Button(showAdd ? $"cancel##{idSuffix}" : $"+ add##{idSuffix}"))
    {
        showAdd = !showAdd;
        input = "";
        editIdx = -1;
    }

    if (showAdd)
    {
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 30f);
        ImGui.SetKeyboardFocusHere();
        if (ImGui.InputTextWithHint($"##new{idSuffix}", "what will you do today?", ref input, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (!string.IsNullOrWhiteSpace(input.Trim()))
            {
                list.Add((input.Trim(), false));
                input = "";
                showAdd = false;
                onSave();
            }
        }
    }

    ImGui.Spacing();

    int toToggle = -1;
    int toRemove = -1;

    for (int i = 0; i < list.Count; i++)
    {
        var (text, done) = list[i];

        if (isDeleteMode)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 0.6f));
            if (ImGui.Button($"X##del{idSuffix}_{i}")) toRemove = i;
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }

        if (done) ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.2f, 0.5f, 1f, 1f));

        bool check = done;
        if (ImGui.Checkbox($"##chk{idSuffix}_{i}", ref check))
            toToggle = i;

        if (done) ImGui.PopStyleColor();

        ImGui.SameLine();

        if (editIdx == i)
        {
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 120f);
            ImGui.SetKeyboardFocusHere();
            if (ImGui.InputText($"##edit{idSuffix}_{i}", ref editStr, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (!string.IsNullOrWhiteSpace(editStr.Trim()))
                {
                    list[i] = (editStr.Trim(), done);
                    onSave();
                }
                editIdx = -1;
            }
            ImGui.SameLine();
            if (ImGui.Button($"Save##sv{idSuffix}_{i}"))
            {
                if (!string.IsNullOrWhiteSpace(editStr.Trim()))
                {
                    list[i] = (editStr.Trim(), done);
                    onSave();
                }
                editIdx = -1;
            }
        }
        else
        {
            if (done)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.TextWrapped(text);
                
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                float midY = (min.Y + max.Y) / 2f;
                ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, midY), new Vector2(max.X, midY), ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1f)), 1.5f);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.TextWrapped(text);
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                editIdx = i;
                editStr = text;
            }
        }
    }

    if (toToggle >= 0)
    {
        var (text, done) = list[toToggle];
        list[toToggle] = (text, !done);
        onSave();
    }

    if (toRemove >= 0)
    {
        list.RemoveAt(toRemove);
        if (editIdx == toRemove) editIdx = -1;
        onSave();
    }
}

uint LoadTexture(string path)
{
    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
    var pixels = new byte[image.Width * image.Height * 4];
    image.CopyPixelDataTo(pixels);

    uint texture = gl!.GenTexture();
    gl.BindTexture(TextureTarget.Texture2D, texture);

    unsafe
    {
        fixed (byte* ptr = pixels)
        {
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba,
                (uint)image.Width,
                (uint)image.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr
            );
        }
    }

    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

    return texture;
}
