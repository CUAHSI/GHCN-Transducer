using System;

namespace GhcnHarvester
{
    /// <summary>
    /// Auxiliary class to hold information about column
    /// positions in the ghcnd-stations and ghcnd-inventory files
    /// </summary>
    class TextFileColumn
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
}
