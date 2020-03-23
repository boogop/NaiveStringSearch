using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using NaiveStringSearch.Utils;

/*
    * 
    * Project:
    * - open a binary file and find any english word in any position  
    * 
    * Naive (needle/haystack) string search when pattern to match is any word 
    * in the english language
    * - check for any words at position 0
    * - check for any words at position 0+1
    * - check for any words at position 0+2
    * - etc
    * 
    * 
    * Requirements:
    * - progressbar
    * - doevents so the form doesn't go braindead
    * - one try-catch block
    * - no object orientation, this is just futzing around with algorithms
    * - any method calls should be cast as static
    * 
    * Problem
    * - NetSpell is identifying too many things as words
*/

namespace NaiveStringSearch
{
    public partial class MainForm : Form
    {
    OpenFileDialog openFileDialog1;
    NetSpell.SpellChecker.Spelling nS;

        public MainForm()
        {
            InitializeComponent();


            NetSpell.SpellChecker.Dictionary.WordDictionary w = new NetSpell.SpellChecker.Dictionary.WordDictionary();
            w.DictionaryFile = "en-US.dic";
            w.DictionaryFolder = Application.StartupPath + @"\";
            w.Initialize();
            nS = new NetSpell.SpellChecker.Spelling();
            nS.Dictionary = w;
            nS.ShowDialog = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFile(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void openFile(string filename)
        {
            string name = chkNull.whenNull(filename);

            if (chkNull.isNull(filename))
            {
                openFileDialog1 = new OpenFileDialog();
                openFileDialog1.RestoreDirectory = false;
                if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
                name = openFileDialog1.FileName;
            }

            if (chkNull.isNull(name)) return;

            // move all var decs out of the loop
            string r, s, t, raw;
            r = s = t = raw = "";
            bool isWord, skip;  // bools are initialized to false per C# spec
            byte bi;            // probably should initialize these but eh
            byte[] bytes;
            List<string> strings;
            int chunksize = 20;
            int iIncrement = chunksize - 10;

            this.Text = "Word Locator [" + name + "]";

            FileInfo f = new FileInfo(name);

            // show size
            double len = 0;
            if (f.Length > 0)
                len = Math.Round((chkNull.numNull(f.Length) / 1024 / 1024), 2);

            // show file info
            lblSize.Text = len.ToString() + "MB";
            lblCreated.Text = f.CreationTime.ToShortDateString();
            lblAccessed.Text = f.LastAccessTime.ToShortDateString();

            txtText.Text = "";

            Files fs = new Files();
            fs.processFile(name);

            // pop text & raw bytes
            strings = new List<string>();
            raw = fs.theText;
            bytes = fs.theBytes;

            pb1.Maximum = raw.Length / iIncrement + chunksize;

            // my understanding is each time through a loop the end condition is evaluated,
            // so if you have any arithmetic ops in the end condition those are eval'd every time
            int end = raw.Length - chunksize;
            for (int i = 0; i < end; i += iIncrement)
            {
                // each time through, select a chunk of n size. Increment i
                // by 10 since any word crossing the chunksize is unlikely to
                // have more than 10 chars in the preceeding chunk (at least
                // I'm willing to make that assumption for this project)

                s = GeneralTools.Mid(raw, i, chunksize);

                for (int j = 0; j < s.Length; j++)
                {
                    // track the decimal value of the char in the bytes[] array we exported from fs
                    bi = bytes[i + j];
                    // in the ascii table alpha chars are between 65 & 122, space is at 32
                    skip = (bi < 65 || bi > 122) && bi != 32;
                    if (skip) continue;
                    /*
                     * We need to scan each chunk for words. To do that we have to decrease
                     * the size of the chunk until a word is recognizable
                     * 
                     * string t =
                     * [left] _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ [right]
                     *        x x x x x <--- word here
                     * 
                     * If the above is the chunk, we'll look for words beginning on the left end.
                     * Loop k decrements the chunk from the right by one, so shrinks the chunk until
                     * the word at x becomes recognizable. Loop k only moves the right end
                     * 
                     * Loop j decrements the chunk from the left by one and repeats loop k which should
                     * let us recognize words in the middle or end. Loop j only moves the left end
                     * 
                     * string t =
                     *                   [left] _ _ _ _ _ _ _ _ _ _ _ [right]
                     *                          x x x x x <--- word here
                     *                          
                     * So if the above represents string t after a few iterations of j, loop k 
                     * will shrink t until
                     * 
                     * string r =
                     *                   [left] _ _ _ _ _ [right]
                     *                          x x x x x <--- word here
                     * 
                     * and NetSpell picks up the word
                     */

                    // we skip non-alpha chars which increments j so this should still work
                    // select from the right which shortens the string from the left
                    t = GeneralTools.Right(s, s.Length - j);

                    for (int k = 0; k < t.Length; k++)
                    {
                        // select from the left which shortens the string from the right
                        r = GeneralTools.Left(t, t.Length - k);
                        // NetSpell picks up way too many rando 2 letter combinations as words
                        if (r.Length < 3) break;
                        // let NetSpell figure out if we've found a word
                        isWord = nS.TestWord(r.ToLower());

                        if (isWord)
                        {
                            //if (r.ToLower() == "resource")
                            //{
                            //    // I was watching the behavior of fileassassin-setup-1.06.exe which
                            //    // was somehow turning resource into resource + sour + res
                            //    // still haven't figured out how it's doing that
                            //    System.Diagnostics.Debug.Assert(false);
                            //}
                            strings.Add(r);
                            // bail from the loop since we've shrunk t to the leftmost word. We don't
                            // want it to identify 'model' as model + mode + mod although this
                            // doesn't seem to be working the way I think it should
                            break;
                        }
                    }
                }

                pb1.Value++;
                Application.DoEvents();
            }

            for (int i = 0; i < strings.Count; i++)
            {
                // comment this out to see all the dups
                if (!txtText.Text.ToUpper().Contains(strings[i].ToUpper()))
                    txtText.Text += strings[i] + " ";
            }

            lblCount.Text = strings.Count.ToString();
            pb1.Value = 0;

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}
