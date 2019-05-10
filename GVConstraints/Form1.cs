using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GVConstraints
{
    public partial class Form1 : Form
    {
        byte[,] ContactPairs;

        public Form1()
        {
            InitializeComponent();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedItem = "9";
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedItem = "8";
            button4.Enabled = false;
        }
        //Распределение соединений по магистралям
        private void btn2_Click(object sender, EventArgs e)
        {
            Dictionary<byte, List<Line>> routes = new Dictionary<byte, List<Line>>();
            byte contactPairMaxQuantity = (byte)(comboBox1.SelectedIndex + 2);
            byte connectionsQuantity = (byte)(comboBox2.SelectedIndex + 2);
            if (check(contactPairMaxQuantity, connectionsQuantity))
            {
                ChannelRouting obj = new ChannelRouting(ContactPairs);
                string[,] outputTable = obj.FindSolution(ContactPairs, out routes);
                if (outputTable == null)
                {
                    MessageBox.Show("Невозможно разрешить конфликт в графе вертикальных ограничений. Попробуйте изменить условие задачи.");
                }
                else
                {
                    createOutputTable(outputTable);
                }
            }
        }
        //Преобразование введенных данных в пары контактов
        private void getContactPairs(string text)
        {
            string[] contacts = text.Split(',');
            ContactPairs = new byte[2, contacts.Count()];
            for (byte i = 0; i < contacts.Count(); i++)
            {
                string[] parts = contacts[i].Trim().Split('-');
                ContactPairs[0, i] = byte.Parse(parts[0]);
                ContactPairs[1, i] = byte.Parse(parts[1]);
            }
        }
        //Очистка поля для ввода
        private void withoutRandom(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }
        // Генерация соединений
        private void autogen_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            button2.Enabled = true;
            byte contactPairQuantity = (byte)(comboBox1.SelectedIndex + 2);
            byte jointsQuantity = (byte)(comboBox2.SelectedIndex + 2);

            byte[] temp = new byte[2 * contactPairQuantity];
            for (byte i = 1; i <= jointsQuantity; i++)
            {
                Random rnd = new Random();
                byte left, right;
                bool done = false;
                do
                {
                    left = (byte)rnd.Next(0, 2 * contactPairQuantity - 1);
                    if (temp[left] != 0)
                    {
                        left = (byte)rnd.Next(0, 2 * contactPairQuantity - 1);
                    }
                    else
                    {
                        temp[left] = i;
                        done = true;
                    }
                }
                while (!done);

                done = false;
                do
                {
                    right = (byte)rnd.Next(0, 2 * contactPairQuantity - 1);
                    if (temp[right] != 0)
                    {
                        right = (byte)rnd.Next(0, 2 * contactPairQuantity - 1);
                    }
                    else
                    {
                        temp[right] = i;
                        done = true;
                    }
                }
                while (!done);
            }

            ContactPairs = new byte[2, contactPairQuantity];
            textBox1.Text = "";
            for (int i = 0, k = 0; i < contactPairQuantity; k += 2, i++)
            {
                ContactPairs[0, i] = temp[k];
                ContactPairs[1, i] = temp[k + 1];
                if (k == 2 * contactPairQuantity - 2)
                {
                    textBox1.Text += temp[k] + "-" + temp[k + 1];
                }
                else
                {
                    textBox1.Text += temp[k] + "-" + temp[k + 1] + ",";
                }
            }
        }
        //Вывод результатов в таблицу
        private void createOutputTable(string[,] outputTable)
        {
            outputMatrixTable.Rows.Clear();


            DataGridViewCellStyle style = new DataGridViewCellStyle();
            style.ForeColor = Color.White;
            style.BackColor = Color.Gray;

            outputMatrixTable.Width = outputTable.GetLength(1) * 30 + 43;
            outputMatrixTable.Height = (outputTable.GetLength(0) + 1) * 30 + 23;
            outputMatrixTable.ColumnCount = outputTable.GetLength(1);
            outputMatrixTable.RowCount = outputTable.GetLength(0) + 1;

            outputMatrixTable.EnableHeadersVisualStyles = false;

            for (byte i = 0; i < outputMatrixTable.RowCount; i++)
            {
                outputMatrixTable.Rows[i].Height = 30;
            }

            for (byte i = 0; i < outputMatrixTable.ColumnCount; i++)
            {
                outputMatrixTable.Columns[i].Width = 30;
            }

            for (int i = 1, k = 0; i < this.outputMatrixTable.RowCount - 1; i = 2 * k + 1, k++)
            {
                outputMatrixTable.Rows[i].HeaderCell.Value = (k).ToString();
            }

            for (byte j = 0; j < outputMatrixTable.Columns.Count; j++)
            {
                outputMatrixTable.Columns[j].HeaderCell.Value = ContactPairs[0, j].ToString();
                outputMatrixTable.Columns[j].HeaderCell.Style = style;
            }

            for (byte i = 0; i < outputTable.GetLength(0); i++)
            {
                for (byte j = 0; j < outputTable.GetLength(1); j++)
                {
                    outputMatrixTable.Rows[i].Cells[j].Value = outputTable[i, j];
                }
            }

            for (byte j = 0; j < outputTable.GetLength(1); j++)
            {
                outputMatrixTable.Rows[outputMatrixTable.RowCount - 1].Cells[j].Value = ContactPairs[1, j].ToString();
                outputMatrixTable.Rows[outputMatrixTable.RowCount - 1].Cells[j].Style = style;
            }
        }
        //Проверка корректности введенных данных
        private bool check(byte contactPairMaxQuantity, byte connectionsQuantity)
        {
            try
            {
                if (connectionsQuantity > contactPairMaxQuantity)
                {
                    MessageBox.Show("Ошибка. Проверьте правильность условия задачи.", "Число соединений меньше числа контактов");
                    return false;
                }
                getContactPairs(textBox1.Text);
                if (ContactPairs.GetLength(1) != contactPairMaxQuantity)
                {
                    MessageBox.Show("Ошибка. Проверьте правильность условия задачи.", "Число контактов не совпадает с числом заданных.");
                    return false;
                }
                Dictionary<byte, byte> contacts = new Dictionary<byte, byte>();
                for (byte i = 0; i < ContactPairs.GetLength(0); i++)
                {
                    for (byte j = 0; j < ContactPairs.GetLength(1); j++)
                    {
                        if (ContactPairs[i, j] != 0)
                        {
                            if (contacts.ContainsKey(ContactPairs[i, j]))
                            {
                                contacts[ContactPairs[i, j]]++;
                            }
                            else
                            {
                                contacts.Add(ContactPairs[i, j], 1);
                            }
                        }
                    }
                }

                if (contacts.Count() != connectionsQuantity)
                {
                    MessageBox.Show("Ошибка. Проверьте правильность условия задачи.", "Число соединений не совпадает с числом заданных.");
                    return false;
                }

                foreach (var contact in contacts)
                {
                    if (contact.Value < 2)
                    {
                        MessageBox.Show("Ошибка. Проверьте правильность условия задачи.", "Недопустимое число контактов");
                        return false;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Ошибка. Проверьте правильность условия задачи.", "Неверный формат ввода данных");
                return false;
            }
            return true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "3-0,18-12,16-0,3-0,16-0,0-18,3-0,1-12,16-2,17-17,0-0,12-1,10-10,0-13,14-0,0-2,10-0,0-2,11-13,5-5,14-8,11-13,15-4,4-8,0-7,7-0,4-0,6-8,4-15,9-0,0-8,6-0,0-9";
            button4.Enabled = true;
            button2.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            getContactPairs(textBox1.Text);
            ChannelRouting obj = new ChannelRouting(ContactPairs);
            Dictionary<byte, List<Line>> routes;
            string[,] outputTable = obj.FindSolution(ContactPairs, out routes);
            if (outputTable == null)
            {
                MessageBox.Show("Невозможно разрешить конфликт в графе вертикальных ограничений. Попробуйте изменить условие задачи.");
            }
            else
            {
                StringBuilder sb1 = new StringBuilder();
                var sortedRoutes = routes.OrderBy(x => x.Key);
                foreach (var route in sortedRoutes)
                {
                    sb1.AppendLine(route.Key.ToString() + ":");
                    foreach (Line line in route.Value)
                    {
                        sb1.Append(line.ConnectionId.ToString() + "(" + line.LeftEnd.Coordinate.Value.ToString() + "," + line.RightEnd.Coordinate.Value.ToString() + "); ");
                    }
                    sb1.AppendLine();
                }
                string path = AppDomain.CurrentDomain.BaseDirectory + "TestResults.txt";
                using (StreamWriter sw1 = new StreamWriter(path))
                {
                    sw1.WriteLine(sb1);
                }
                System.Diagnostics.Process.Start("notepad.exe", path);
            }
        }
    }
}
