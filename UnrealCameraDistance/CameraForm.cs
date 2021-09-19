using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace UnrealCameraDistance
{
    public partial class CameraForm : Form
    {
        string SerialKey = "7369-1402-D398-4ABF-7849-08DB-8D29-B655";
        public CameraForm()
        {
            if (ProcessHelper.ID() == SerialKey)
            {
                InitializeComponent();
            }
            else
            {
                MessageBox.Show("Warning! Key not found. Your key:" + ProcessHelper.ID() + ", but current:" + SerialKey);
                this.Close();
            }
        }
        bool cameraloaded = false;
        Process war3process = null;
        ProcessMemory war3processmemory = null;
        int GameDll = 0;
        int GameOffset = 0xAB62A4;
        int _GlobalClassOffset = 0xAB4F80;

        int _pGameHashTable = 0xAB7788;

        int HashTableVal_12()
        {
            return war3processmemory.ReadInt(war3processmemory.ReadInt(GameDll + _pGameHashTable) + 12);
        }

        uint HashTableVal_28()
        {
            return war3processmemory.ReadUInt(war3processmemory.ReadInt(GameDll + _pGameHashTable) + 28);
        }

        int HashTableVal_44()
        {
            return war3processmemory.ReadInt(war3processmemory.ReadInt(GameDll + _pGameHashTable) + 44);
        }

        int HashTableVal_60()
        {
            return war3processmemory.ReadInt(war3processmemory.ReadInt(GameDll + _pGameHashTable) + 60);
        }



        int GetPointerFromHashTable(uint X, uint Y)
        {
            bool v2; // zf@3
            int result; // eax@7
            int v4; // ecx@9
            int v5; // ecx@10

            if (!((X >> 31) > 0))
            {
                if (X < HashTableVal_28())
                {
                    v2 = war3processmemory.ReadInt(HashTableVal_12() + (int)(8 * X)) == -2;
                    goto LABEL_6;
                }
                return 0;
            }
            if ((X & 0x7FFFFFFF) >= HashTableVal_60())
            {
                return 0;
            }
            v2 = war3processmemory.ReadInt(HashTableVal_44() + (int)(8 * X)) == -2;
            LABEL_6:
            if (!v2)
            {
                return 0;
            }
            if ((X >> 31) > 0)
            {
                v4 = war3processmemory.ReadInt(HashTableVal_44() + (int)(8 * X) + 4);
                result = war3processmemory.ReadUInt(v4 + 24) != Y ? 0 : v4;
            }
            else
            {
                v5 = war3processmemory.ReadInt(HashTableVal_12() + (int)(8 * X) + 4);
                result = war3processmemory.ReadUInt(v5 + 24) != Y ? 0 : v5;
            }
            return result;

        }

        float GetCameraHeight()
        {
            int offset1 = war3processmemory.ReadInt(GameDll + _GlobalClassOffset);
            if (offset1 > 0)
            {
                offset1 = war3processmemory.ReadInt(offset1 + 0x254);
                if (offset1 > 0)
                {
                    offset1 = (offset1 + 0xfc);
                    if (offset1 > 0)
                    {
                        uint CameraHashtableX = war3processmemory.ReadUInt(offset1 + 0x8);
                        uint CameraHashtableY = war3processmemory.ReadUInt(offset1 + 0xC);
                        offset1 = GetPointerFromHashTable(CameraHashtableX, CameraHashtableY);
                        if (offset1 > 0)
                        {
                            return war3processmemory.ReadFloat(offset1 + 0x78);
                        }
                    }
                }
            }
            return 0.0f;
        }


        int GetCameraHeightAddr()
        {
            int offset1 = war3processmemory.ReadInt(GameDll + _GlobalClassOffset);
            if (offset1 > 0)
            {
                offset1 = war3processmemory.ReadInt(offset1 + 0x254);
                if (offset1 > 0)
                {

                    offset1 = (offset1 + 0xfc);
                    if (offset1 > 0)
                    {
                        uint CameraHashtableX = war3processmemory.ReadUInt(offset1 + 0x8);
                        uint CameraHashtableY = war3processmemory.ReadUInt(offset1 + 0xC);
                        offset1 = GetPointerFromHashTable(CameraHashtableX, CameraHashtableY);
                        if (offset1 > 0)
                        {
                            return (offset1 + 0x78);
                        }
                    }
                }
            }
            return 0;
        }


        float SetCameraHeight(float newcamera)
        {
            int offset1 = war3processmemory.ReadInt(GameDll + _GlobalClassOffset);
            if (offset1 > 0)
            {
                offset1 = war3processmemory.ReadInt(offset1 + 0x254);
                if (offset1 > 0)
                {
                    offset1 = offset1 = (offset1 + 0xfc);
                    if (offset1 > 0)
                    {
                        uint CameraHashtableX = war3processmemory.ReadUInt(offset1 + 0x8);
                        uint CameraHashtableY = war3processmemory.ReadUInt(offset1 + 0xC);
                        offset1 = GetPointerFromHashTable(CameraHashtableX, CameraHashtableY);
                        if (offset1 > 0)
                        {
                            war3processmemory.WriteFloat(offset1 + 0x78, newcamera);
                            return newcamera;
                        }
                    }
                }
            }
            return 0.0f;
        }

        private int FindGameDll()
        {
            foreach (ProcessModule m in war3process.Modules)
            {
                if (m.FileVersionInfo.FileName.ToLower().IndexOf("game.dll") > -1)
                {
                    //MessageBox.Show(m.BaseAddress.ToInt32().ToString("x2"));
                    return m.BaseAddress.ToInt32();
                }
            }
            return 0;
        }
        bool works = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (works)
                return;
            works = true;

            if (war3process == null || war3processmemory == null)
            {
                try
                {
                    war3process = Process.GetProcessesByName("war3")[0];
                    war3processmemory = new ProcessMemory(war3process.Id, war3process.ProcessName);
                    // Thread.Sleep(1000);
                    war3processmemory.StartProcess();
                }
                catch
                {
                    war3process = null;
                    war3processmemory = null;
                    cameraloaded = false;
                }
            }
            else
            {
                try
                {
                    //  war3process.Refresh();
                    //  MessageBox.Show("Start search Game.dll");
                    GameDll = FindGameDll();
                    //  MessageBox.Show(GameDll.ToString("x2"));
                    // GameDll = war3processmemory.DllImageAddress("Game.dll");
                    if (war3processmemory.ReadInt(GameOffset + GameDll) > 0)
                    {
                        //    MessageBox.Show("ok1");
                        if (!cameraloaded)
                        {
                            cameraloaded = true;
                            textBox1.Text = GetCameraHeight().ToString();
                        }
                        try
                        {
                            SetCameraHeight(float.Parse(textBox1.Text) + 1.25f);
                        }
                        catch
                        {

                        }

                    }
                    else
                    {
                        war3process = null;
                        war3processmemory = null;
                        cameraloaded = false;
                    }
                }
                catch
                {
                    war3process = null;
                    war3processmemory = null;
                    cameraloaded = false;
                }
            }
            works = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void CameraForm_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
