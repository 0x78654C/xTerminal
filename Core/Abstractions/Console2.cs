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

using System;

namespace Core.Abstractions
{
    public class Console2 : IConsole
    {
        public int CursorLeft => Console.CursorLeft;

        public int CursorTop => Console.CursorTop;

        public int BufferWidth => Console.BufferWidth;

        public int BufferHeight => Console.BufferHeight;

        public bool PasswordMode { get; set; }

        public void SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);

        public void SetCursorPosition(int left, int top)
        {
            if (!PasswordMode)
                Console.SetCursorPosition(left, top);
        }

        public void Write(string value)
        {
            if (PasswordMode)
                value = new String(default(char), value.Length);

            Console.Write(value);
        }

        public void WriteLine(string value) => Console.WriteLine(value);
    }
}