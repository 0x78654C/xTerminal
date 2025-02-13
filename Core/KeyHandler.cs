/*
 
 The MIT License (MIT)

Copyright (c) 2017 Toni Solarin-Sodara

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
Credits to : https://github.com/tonerdo/readline

Modified by: x_coding

 */


using Core.Abstractions;
using Core.Commands;
using Core.SystemTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Json = Core.SystemTools.JsonManage;
using AliasC = Core.SystemTools.AliasC;

namespace Core
{
    [SupportedOSPlatform("windows")]
    public class KeyHandler
    {
        private int _cursorPos;
        private int _cursorLimit;
        private StringBuilder _text;
        private List<string> _history;
        private int _historyIndex;
        private ConsoleKeyInfo _keyInfo;
        private Dictionary<string, Action> _keyActions;
        private string[] _completions;
        private int _completionStart;
        private int _completionsIndex;
        private IConsole Console2;
        private string _multiParam = "";
        private bool IsStartOfLine() => _cursorPos == 0;

        private bool IsEndOfLine() => _cursorPos == _cursorLimit;

        private bool IsStartOfBuffer() => Console2.CursorLeft == 0;

        private bool IsEndOfBuffer() => Console2.CursorLeft == Console2.BufferWidth - 1;
        private bool IsInAutoCompleteMode() => _completions != null;

        private void MoveCursorLeft()
        {
            if (IsStartOfLine())
                return;

            if (IsStartOfBuffer())
                Console2.SetCursorPosition(Console2.BufferWidth - 1, Console2.CursorTop - 1);
            else
                Console2.SetCursorPosition(Console2.CursorLeft - 1, Console2.CursorTop);

            _cursorPos--;
        }

        private void MoveCursorHome()
        {
            while (!IsStartOfLine())
                MoveCursorLeft();
        }

        private string BuildKeyInput()
        {
            return (_keyInfo.Modifiers != ConsoleModifiers.Control && _keyInfo.Modifiers != ConsoleModifiers.Shift) ?
                _keyInfo.Key.ToString() : _keyInfo.Modifiers.ToString() + _keyInfo.Key.ToString();
        }

        private void MoveCursorRight()
        {
            if (IsEndOfLine())
                return;

            if (IsEndOfBuffer())
                Console2.SetCursorPosition(0, Console2.CursorTop + 1);
            else
                Console2.SetCursorPosition(Console2.CursorLeft + 1, Console2.CursorTop);

            _cursorPos++;
        }

        private void MoveCursorEnd()
        {
            while (!IsEndOfLine())
                MoveCursorRight();
        }

        private void NullVoid() { }
        private void ClearLine()
        {
            MoveCursorEnd();
            while (!IsStartOfLine())
                Backspace();
        }

        private void WriteNewString(string str)
        {
            ClearLine();
            foreach (char character in str)
                WriteChar(character);
        }

        private void WriteString(string str)
        {
            foreach (char character in str)
                WriteChar(character);
        }

        private void WriteChar() => WriteChar(_keyInfo.KeyChar);

        private void WriteChar(char c)
        {
            if (IsEndOfLine())
            {
                _text.Append(c);
                Console2.Write(c.ToString());
                _cursorPos++;
            }
            else
            {
                int left = Console2.CursorLeft;
                int top = Console2.CursorTop;
                string str = _text.ToString().Substring(_cursorPos);
                _text.Insert(_cursorPos, c);
                Console2.Write(c.ToString() + str);
                Console2.SetCursorPosition(left, top);
                MoveCursorRight();
            }

            _cursorLimit++;
        }

        private void Backspace()
        {
            if (IsStartOfLine())
                return;

            MoveCursorLeft();
            int index = _cursorPos;
            _text.Remove(index, 1);
            string replacement = _text.ToString().Substring(index);
            int left = Console2.CursorLeft;
            int top = Console2.CursorTop;
            Console2.Write(string.Format("{0} ", replacement));
            Console2.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        private void Delete()
        {
            if (IsEndOfLine())
                return;

            int index = _cursorPos;
            _text.Remove(index, 1);
            string replacement = _text.ToString().Substring(index);
            int left = Console2.CursorLeft;
            int top = Console2.CursorTop;
            Console2.Write(string.Format("{0} ", replacement));
            Console2.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        private void TransposeChars()
        {
            // local helper functions
            bool almostEndOfLine() => (_cursorLimit - _cursorPos) == 1;
            int incrementIf(Func<bool> expression, int index) => expression() ? index + 1 : index;
            int decrementIf(Func<bool> expression, int index) => expression() ? index - 1 : index;

            if (IsStartOfLine()) { return; }

            var firstIdx = decrementIf(IsEndOfLine, _cursorPos - 1);
            var secondIdx = decrementIf(IsEndOfLine, _cursorPos);

            var secondChar = _text[secondIdx];
            _text[secondIdx] = _text[firstIdx];
            _text[firstIdx] = secondChar;

            var left = incrementIf(almostEndOfLine, Console2.CursorLeft);
            var cursorPosition = incrementIf(almostEndOfLine, _cursorPos);

            WriteNewString(_text.ToString());

            Console2.SetCursorPosition(left, Console2.CursorTop);
            _cursorPos = cursorPosition;

            MoveCursorRight();
        }

        private void StartAutoComplete()
        {
            while (_cursorPos > _completionStart)
                Backspace();

            _completionsIndex = 0;

            WriteString(_completions[_completionsIndex]);
        }

        private void NextAutoComplete()
        {
            while (_cursorPos > _completionStart)
                Backspace();

            _completionsIndex++;

            if (_completionsIndex == _completions.Length)
                _completionsIndex = 0;

            WriteString(_completions[_completionsIndex]);
        }

        private void PreviousAutoComplete()
        {
            while (_cursorPos > _completionStart)
                Backspace();

            _completionsIndex--;

            if (_completionsIndex == -1)
                _completionsIndex = _completions.Length - 1;

            WriteString(_completions[_completionsIndex]);
        }

        private void PrevHistory()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                WriteNewString(_history[_historyIndex]);
            }
        }

        private void NextHistory()
        {
            if (_historyIndex < _history.Count)
            {
                _historyIndex++;
                if (_historyIndex == _history.Count)
                    ClearLine();
                else
                    WriteNewString(_history[_historyIndex]);
            }
        }

        private void ResetAutoComplete()
        {
            _completions = null;
            _completionsIndex = 0;
        }

        public string Text
        {
            get
            {
                return _text.ToString();
            }
        }

        /// <summary>
        /// Get the alias commands from alias file.
        /// </summary>
        /// <param name="aliasFile"></param>
        /// <returns></returns>
        private static List<string> AliasCommands(string aliasFile)
        {
            var listCommands = new List<string>();
            if (!File.Exists(aliasFile))
                return listCommands;

            var data = Json.ReadJsonFromFile<AliasC[]>(aliasFile);
            foreach (var line in data)
                listCommands.Add(line.CommandName.Trim());
            return listCommands;
        }


        public KeyHandler(IConsole console, List<string> history, IAutoCompleteHandler autoCompleteHandler)
        {
            Console2 = console;

            _history = history ?? new List<string>();
            _historyIndex = _history.Count;
            _text = new StringBuilder();
            _keyActions = new Dictionary<string, Action>();

            _keyActions["LeftArrow"] = MoveCursorLeft;
            _keyActions["Home"] = MoveCursorHome;
            _keyActions["End"] = MoveCursorEnd;
            _keyActions["RightArrow"] = MoveCursorRight;
            _keyActions["ControlX"] = MoveCursorEnd;
            _keyActions["Backspace"] = Backspace;
            _keyActions["Delete"] = Delete;
            _keyActions["UpArrow"] = PrevHistory;
            _keyActions["DownArrow"] = NextHistory;
            _keyActions["Escape"] = NullVoid;
            _keyActions["Insert"] = NullVoid;
            _keyActions["Tab"] = () =>
            {
                GlobalVariables.tabPressCount++;
                if (GlobalVariables.tabPressCount == 2)
                {
                    MoveCursorEnd();
                    var outCompletion = "";
                    var currentDirectory = File.ReadAllText(GlobalVariables.currentDirectory);
                    var candidate = _text.ToString();
                    candidate = candidate.Trim('\t');
                    candidate = candidate.Trim('\b');
                    candidate = candidate.Trim('\0');
                    candidate = candidate.Trim('\r');
                    candidate = candidate.Trim('\n');
                    candidate = candidate.Trim('\u0018');
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "cd", currentDirectory, GlobalVariables.TypeSuggestions.Directory, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "odir", currentDirectory, GlobalVariables.TypeSuggestions.Directory, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "ls", currentDirectory, GlobalVariables.TypeSuggestions.Directory, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "hex", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "./", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "ccs", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "fcopy", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "mv", currentDirectory, GlobalVariables.TypeSuggestions.All, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "fmove", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "del", currentDirectory, GlobalVariables.TypeSuggestions.All, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "edit", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "cp", currentDirectory, GlobalVariables.TypeSuggestions.All, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "md5", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "sort", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "cat", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "ln", currentDirectory, GlobalVariables.TypeSuggestions.All, ref outCompletion);
                    AutoSuggestionCommands.FileDirSuggestion(candidate, _multiParam, "exif", currentDirectory, GlobalVariables.TypeSuggestions.File, ref outCompletion);
                    var aliasCommands = AliasCommands(GlobalVariables.aliasFile);

                    // Passing alias comamnds in auto suggestion list.
                    foreach (var aliasCommand in aliasCommands)
                    {
                        if (aliasCommand.Length > 0 && candidate.StartsWith(aliasCommand))
                        {

                            var splitCanmdidate = candidate.Split(' ');
                            var cmd = splitCanmdidate[0];
                            var countParams = splitCanmdidate.Count();
                            var lastSuggestion = splitCanmdidate[countParams - 1];

                            var suggestionPass = "";
                            if (lastSuggestion != cmd)
                                suggestionPass = $"{cmd} {lastSuggestion}";
                            else
                                suggestionPass = cmd;
                            var count = 0;
                            _multiParam = "";
                            foreach (var param in splitCanmdidate)
                            {
                                if (countParams > 2)
                                    if (count > 0 && count < countParams - 1)
                                        _multiParam += $"{param} ";
                                count++;
                            }
                            AutoSuggestionCommands.FileDirSuggestion(suggestionPass, _multiParam, aliasCommand, currentDirectory, GlobalVariables.TypeSuggestions.All, ref outCompletion);
                        }
                    }

                    var countOut = outCompletion.ToList().Count;
                    if (countOut > 0)
                    {
                        var getCommandStr = candidate;
                        var getCommand = getCommandStr.Split(' ')[0];
                        var paramCommand = getCommandStr.SplitByText($"{getCommand} ", 1);
                        foreach (var paramChar in getCommandStr)
                            Backspace();
                        var command = "";
                        if (_multiParam.Length > 1)
                            command = $"{getCommand} {_multiParam.Trim()} {outCompletion}";
                        else
                            command = $"{getCommand} {outCompletion}";
                        _multiParam = "";
                        _cursorPos += command.Length - _text.Length;
                        _text.Clear();  
                        _text.Append(command);
                        _cursorLimit = _text.Length;
                        Console2.Write(_text.ToString());
                    }
                    GlobalVariables.tabPressCount = 0;
                }

                //_keyActions["ControlA"] = MoveCursorHome;
                //_keyActions["ControlB"] = MoveCursorLeft;
                //_keyActions["Escape"] = ClearLine;
                //_keyActions["ControlP"] = PrevHistory;
                //_keyActions["ControlD"] = Delete;
                //_keyActions["ControlH"] = Backspace;
                //_keyActions["ControlL"] = ClearLine;
                //_keyActions["ControlF"] = MoveCursorRight;
                //_keyActions["ControlE"] = MoveCursorEnd;
                //_keyActions["ControlN"] = NextHistory;
                //_keyActions["ControlU"] = () =>
                //{
                //    while (!IsStartOfLine())
                //        Backspace();
                //};
                //_keyActions["ControlK"] = () =>
                //{
                //    int pos = _cursorPos;
                //    MoveCursorEnd();
                //    while (_cursorPos > pos)
                //        Backspace();
                //};
                //_keyActions["ControlW"] = () =>
                //{
                //    while (!IsStartOfLine() && _text[_cursorPos - 1] != ' ')
                //        Backspace();
                //};
                //_keyActions["ControlT"] = TransposeChars;

                //if (IsInAutoCompleteMode())
                //{
                //    NextAutoComplete();
                //}
                //else
                //{
                //    if (autoCompleteHandler == null || !IsEndOfLine())
                //        return;

                //    string text = _text.ToString();

                //    _completionStart = text.LastIndexOfAny(autoCompleteHandler.Separators);
                //    _completionStart = _completionStart == -1 ? 0 : _completionStart + 1;

                //    _completions = autoCompleteHandler.GetSuggestions(text, _completionStart);
                //    _completions = _completions?.Length == 0 ? null : _completions;

                //    if (_completions == null)
                //        return;

                //    StartAutoComplete();
                //}
            };

            //_keyActions["ShiftTab"] = () =>
            //{
            //    if (IsInAutoCompleteMode())
            //    {
            //        PreviousAutoComplete();
            //    }
            //};
        }

        public void Handle(ConsoleKeyInfo keyInfo)
        {
            _keyInfo = keyInfo;

            // If in auto complete mode and Tab wasn't pressed
            if (IsInAutoCompleteMode() && _keyInfo.Key != ConsoleKey.Tab)
                ResetAutoComplete();

            Action action;
            _keyActions.TryGetValue(BuildKeyInput(), out action);
            action = action ?? WriteChar;
            action.Invoke();
        }
    }
}