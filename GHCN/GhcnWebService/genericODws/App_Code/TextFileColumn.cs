using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for TextFileColumn
/// </summary>
public class TextFileColumn
{
    private int _start;
    private int _end;

    public TextFileColumn(int startIndex, int endIndex)
    {
        _start = startIndex;
        _end = endIndex;
    }

    public int Start
    {
        get { return _start - 1; }
    }

    public int Length
    {
        get { return (_end - _start + 1); }
    }
}