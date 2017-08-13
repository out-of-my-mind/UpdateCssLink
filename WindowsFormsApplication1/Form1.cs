using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SQLite;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public class filemodel {
            public string path { set;get;}
        }
        //设置窗口透明度
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.Opacity = Math.Round((Double)(12 - trackBar1.Value) / (Double)10, 1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.Red;
            this.TransparencyKey = BackColor;
            this.Opacity = 1;
            this.comboBox1.Text = "*.aspx";
            this.comboBox1.Items.AddRange(new object[] {"*.aspx","*.html"});
            //this.comboBox1.SelectionStart = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //选择文件夹,显示文件夹根目录所有同类别的文件
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string path = folderBrowserDialog1.SelectedPath;
                textBox1.Text = path;
                BindData("*.aspx");
            }
        }

        private void BindData(string searchtype)
        {
            if (!string.IsNullOrEmpty(textBox1.Text.Trim()))
            {
                IEnumerable<string> files = Directory.EnumerateFiles(textBox1.Text, searchtype, SearchOption.TopDirectoryOnly);
                List<filemodel> myfile = new List<filemodel>();
                filemodel myfilemodel = new filemodel();
                foreach (string model in files)
                {
                    myfilemodel = new filemodel();
                    myfilemodel.path = model;
                    myfile.Add(myfilemodel);
                }
                GridViewFileShow.DataSource = myfile;
            }
        }
        
        //搜索框内容改变时
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text != null) {
                BindData(comboBox1.Text);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //分析文件
            DataGridViewCheckBoxCell check = new DataGridViewCheckBoxCell();
            List<string> pathlist = new List<string>();
            foreach (DataGridViewRow row in GridViewFileShow.Rows) {
                check = (DataGridViewCheckBoxCell)row.Cells[0];
                if ((bool)check.EditedFormattedValue || (bool)check.FormattedValue) {
                    pathlist.Add(row.Cells[1].Value.ToString());
                }
            }
            progressBar1.Maximum = pathlist.Count;
            progressBar1.Step = 1;
            System.Configuration.AppSettingsReader AppSettings = new System.Configuration.AppSettingsReader();
            string sqlconn = string.Empty;
            try
            {
                sqlconn = AppSettings.GetValue("SQLiteConnection", typeof(String)).ToString();
            }
            catch (InvalidOperationException ex) {
                //配置文件不正确
            }
            string sqlstr = @"create table if not exists filepath(
                            id integer primary key autoincrement,
                            path text,
                            grouplabel text
                            );";
            SQLiteCommand SQLiteCmd = new SQLiteCommand();
            SQLiteCmd.CommandText = sqlstr;
            SQLiteCmd.CommandType = CommandType.Text;
            if (SQLiteCommand.Execute(sqlstr, SQLiteExecuteType.Default, sqlconn) != null)
            {
                sqlstr = "";
                List<string> linkes = new List<string>();
                string lable = hidgroup.Text = DateTime.Now.ToString("yyyyMMddhhmmss");
                foreach (string path in pathlist)
                {
                    sqlstr += "insert into filepath(path,grouplabel) values('"+ path + "','"+ lable + "');";
                    linkes.AddRange(GetFileCssJSLink(path, ""));
                    progressBar1.PerformStep();
                }
                int count = (int)SQLiteCommand.Execute(sqlstr, SQLiteExecuteType.NonQuery, sqlconn);
                List<filemodel> myfile = new List<filemodel>();
                filemodel myfilemodel = new filemodel();
                linkes = linkes.Distinct().ToList();
                foreach (string model in linkes)
                {
                    myfilemodel = new filemodel();
                    myfilemodel.path = model;
                    myfile.Add(myfilemodel);
                }
                GridViewFileShow.DataSource = myfile;

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //处理文件    需要的参数   文件路径、正则类型
            string sqlstr = string.Empty;
            DataGridViewCheckBoxCell check = new DataGridViewCheckBoxCell();
            List<string> pathlist = new List<string>();
            System.Configuration.AppSettingsReader AppSettings = new System.Configuration.AppSettingsReader();
            string sqlconn = string.Empty;bool myselffile = false;
            try
            {
                sqlconn = AppSettings.GetValue("SQLiteConnection", typeof(String)).ToString();
                if (string.Equals(AppSettings.GetValue("UpdateFile", typeof(String)).ToString(), "myself", StringComparison.OrdinalIgnoreCase))
                {
                    //替换源文件
                    myselffile = true;
                }
                else {
                    if (!Directory.Exists(Environment.CurrentDirectory + "\\UpdateFiled")) {
                        Directory.CreateDirectory(Environment.CurrentDirectory + "\\UpdateFiled");
                    }
                }
                
            }
            catch (InvalidOperationException ex)
            {
                return;
                //配置文件不正确
            }
            sqlstr = "create table if not exists updatelinks(id integer primary key autoincrement, link text,grouplable text);";
            foreach (DataGridViewRow row in GridViewFileShow.Rows)
            {
                check = (DataGridViewCheckBoxCell)row.Cells[0];
                if ((bool)check.EditedFormattedValue || (bool)check.FormattedValue)
                {
                    sqlstr += "insert into updatelinks(link,grouplable) values('" + row.Cells[1].Value.ToString() + "','" + hidgroup.Text + "');";
                    pathlist.Add(row.Cells[1].Value.ToString());
                }
            }
            SQLiteCommand.Execute(sqlstr, SQLiteExecuteType.NonQuery, sqlconn);
            progressBar1.Maximum = pathlist.Count;
            progressBar1.Step = 1;
            
            sqlstr = "select path from filepath where grouplabel =( select grouplabel from filepath order by id desc LIMIT 1);";
            SQLiteDataReader dr  = (SQLiteDataReader)SQLiteCommand.Execute(sqlstr, SQLiteExecuteType.Reader, sqlconn);
            while (dr.Read()) {
                StringBuilder strhtml = ReadHtmlFile(dr["path"].ToString());
                foreach (string model in pathlist)
                {
                    if (model.IndexOf('?') > 0)
                    {
                        string old = "";
                        if (model.IndexOf("random") > 0)
                        {
                            //如果之前更改过
                            old = model.Substring(model.IndexOf("?random")+26);
                        }
                        else {
                            old = model.Substring(model.IndexOf('?'));
                        }
                        strhtml.Replace(model, model.Split('?')[0] + "?random=" + DateTime.Now.Ticks + (model.IndexOf('&') > 0 ? "&" : "") + old);
                    }
                    else
                    {
                        strhtml.Replace(model, model + "?random=" + DateTime.Now.Ticks);
                    }
                }
                string path = (myselffile ? dr["path"].ToString() : Environment.CurrentDirectory + "\\UpdateFiled\\" + dr["path"].ToString().Split('\\').Last());
                WriteHtmlFile(strhtml, path);
            }
        }
        
        /// <summary>
        /// 获取文件的链接  文件中所有的链接
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="type">链接类型</param>
        /// <returns></returns>
        private List<string> GetFileCssJSLink(string path,string type)
        {
            List<string> result = new List<string>();
            StringBuilder strhtml = ReadHtmlFile(path);
            string regx = string.Empty;
            if (type == "js"){
                regx = "(src.*[=].*\")(.*\\.js[^\\s]*)(\")";
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[2].Value);
                }
            }
            else if (type == "css"){
                regx = "(href.*[=].*\")(.*\\.css[^\\s]*)(\")";
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[2].Value);
                }
            }
            else {
                regx = "((href|src).*[=].*\")(.*\\.(css|js)[^\\s]*)(\")";//((href=\")| (src=\"))((.*\\.css[^\\s]*)|(.*\\.js[^\\s]*))(\")
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[3].Value);
                }
            }
            return result;
        }

        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //菜单历史记录  需要的参数   文件路径、正则类型、替换的链接
            MessageBox.Show("暂未完成该功能！");
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="temp"></param>Server.MapPath(".") + "/location_new.html")
        /// <returns></returns>
        public StringBuilder ReadHtmlFile(string temppath)
        {
            StringBuilder temp = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(temppath))
                {
                    temp.Append(sr.ReadToEnd());
                    sr.Dispose();
                    sr.Close();
                }
            }
            catch (Exception exp)
            {

            }
            return temp;
        }

        /// <summary>
        /// 写入HTML文件
        /// </summary>"test.htm"
        /// <param name="str">HTML代码</param>
        /// <param name="htmlfilename">完整带路径的文件名</param>
        /// <returns></returns>
        public bool WriteHtmlFile(StringBuilder str, string htmlfilename)
        {
            // 写文件 
            try
            {
                using (StreamWriter sw = new StreamWriter(htmlfilename, false, System.Text.Encoding.GetEncoding("UTF-8"))) //保存地址
                {
                    sw.WriteLine(str);
                    sw.Flush();
                    sw.Dispose();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {

            }
            return true;
        }
    }
}
