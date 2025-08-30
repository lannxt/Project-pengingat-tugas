using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Project_pengingat_tugas
{
    public partial class Form1 : Form
    {
        private class Reminder
        {
            public string Title { get; set; }
            public DateTime When { get; set; }
            public bool Notified { get; set; }
        }

        private readonly List<Reminder> _reminders = new List<Reminder>();
        private readonly string _dataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ReminderApp-data.json");


        private System.Windows.Forms.Timer _timer;
        private NotifyIcon _notify;

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {

            if (listView1.View != View.Details) listView1.View = View.Details;
            if (listView1.Columns.Count == 0)
            {
                listView1.Columns.Add("Waktu", 160);
                listView1.Columns.Add("Judul", 280);
                listView1.FullRowSelect = true;
                listView1.HideSelection = false;

                listView1.MultiSelect = false;
            }


            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd/MM/yyyy HH:mm";
            dateTimePicker1.ShowUpDown = true;


            _notify = new NotifyIcon
            {
                Visible = true,
                BalloonTipTitle = "Pengingat",
                Text = "Pengingat Jadwal"
            };
            try
            {
                _notify.Icon = SystemIcons.Information; 
            }
            catch {  }


            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 30000; 
            _timer.Tick += Timer_Tick;
            _timer.Start();

            listView1.KeyDown += ListView1_KeyDown;


            LoadData();
            RefreshList();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var title = (textBox1.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Judul kosong. Isi dulu dong.");
                textBox1.Focus();
                return;
            }

            var when = dateTimePicker1.Value;
            if (when < DateTime.Now.AddMinutes(-1))
            {
                var r = MessageBox.Show("Waktunya sudah lewat. Tetap tambahkan?",
                    "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.No) return;
            }

            _reminders.Add(new Reminder { Title = title, When = when, Notified = false });
            _reminders.Sort((a, b) => a.When.CompareTo(b.When));

            textBox1.Clear();
            RefreshList();
            SaveData(); 
        }


        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Pilih reminder yang mau dihapus dulu.");
                return;
            }

            var idx = listView1.SelectedItems[0].Index;
            var sorted = _reminders.OrderBy(x => x.When).ToList();
            if (idx < 0 || idx >= sorted.Count) return;

            var item = sorted[idx];

            var confirm = MessageBox.Show($"Hapus reminder \"{item.Title}\" pada {item.When:dd/MM/yyyy HH:mm} ?",
                "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _reminders.Remove(item);
                RefreshList();
                SaveData();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (listView1.SelectedItems.Count == 0) return;
            var idx = listView1.SelectedItems[0].Index;
            var sorted = _reminders.OrderBy(x => x.When).ToList();
            if (idx < 0 || idx >= sorted.Count) return;

            var r = sorted[idx];
            textBox1.Text = r.Title;
            dateTimePicker1.Value = r.When;
        }

        private void ListView1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Delete && listView1.SelectedItems.Count > 0)
            {
                BtnDelete_Click(sender, EventArgs.Empty);
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void textBox1_TextChanged_1(object sender, EventArgs e) { }
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }


        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var due = _reminders.Where(r => !r.Notified && r.When <= now).ToList();
            foreach (var r in due)
            {
                ShowReminder(r);
                r.Notified = true;
            }
            if (due.Count > 0)
            {
                SaveData();
                RefreshList();
            }
        }

        private void ShowReminder(Reminder r)
        {
            try
            {
                _notify.BalloonTipText = $"{r.Title}\n{r.When:dd/MM/yyyy HH:mm}";
                _notify.ShowBalloonTip(5000);
                System.Media.SystemSounds.Asterisk.Play();
                this.Activate();
            }
            catch
            {
        
            }
        }


        private void RefreshList()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            foreach (var r in _reminders.OrderBy(x => x.When))
            {
                var it = new ListViewItem(r.When.ToString("dd/MM/yyyy HH:mm"));
                it.SubItems.Add(r.Title + (r.Notified ? " (terkirim)" : ""));
                listView1.Items.Add(it);
            }

            listView1.EndUpdate();
        }

        private void SaveData()
        {
            try
            {
                var json = JsonSerializer.Serialize(_reminders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var data = JsonSerializer.Deserialize<List<Reminder>>(json);
                    if (data != null)
                    {
                        _reminders.Clear();
                        _reminders.AddRange(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data: " + ex.Message);
            }
        }

        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveData();
            try { _notify?.Dispose(); } catch { }
        }

        private void BtnDel_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Pilih dulu item yang mau dihapus.");
                return;
            }

            var idx = listView1.SelectedItems[0].Index;
            var sorted = _reminders.OrderBy(x => x.When).ToList();
            var item = sorted[idx];

            var confirm = MessageBox.Show($"Yakin hapus '{item.Title}'?",
                "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _reminders.Remove(item);
                RefreshList();
                SaveData();
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {

        }
    }
}
