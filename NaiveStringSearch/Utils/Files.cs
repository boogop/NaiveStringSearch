using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace NaiveStringSearch.Utils
{
    internal class Files
    {
        private string _hex;

        internal string theHex
        {
            get { return _hex; }
            set { _hex = value; }
        }

        private string _text;

        internal string theText
        {
            get { return _text; }
            set { _text = value; }
        }

        private byte[] _bytes;

        public byte[] theBytes
        {
            get { return _bytes; }
            set { _bytes = value; }
        }



        internal void processFile(string filename)
        {
            string foo = "";
            _bytes = File.ReadAllBytes(filename);
            string hex = BitConverter.ToString(_bytes);
            _hex = GeneralTools.ReplaceEx(hex, "-", " ");

            Task t = Task.Run(() =>
            {
                foo = Encoding.ASCII.GetString(_bytes).Replace((char)0, '.');
            });

            t.Wait();
            _text = GeneralTools.ReplaceEx(foo, "?", ".");
        }

    }
}
